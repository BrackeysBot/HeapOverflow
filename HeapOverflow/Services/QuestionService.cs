using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HeapOverflow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace HeapOverflow.Services;

/// <summary>
///     Represents a service which manages questions in the help category.
/// </summary>
internal sealed class QuestionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly HeapOverflowPlugin _plugin;
    private readonly ICorePlugin _corePlugin;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;

    private readonly List<Question> _activeQuestions = new();
    private readonly Dictionary<DiscordGuild, DiscordChannel> _activeQuestionsChannel = new();
    private readonly Dictionary<DiscordGuild, DiscordChannel> _askHereChannel = new();
    private readonly Dictionary<DiscordGuild, DiscordChannel> _forumsChannel = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="QuestionService"/> class.
    /// </summary>
    public QuestionService(HeapOverflowPlugin plugin, ICorePlugin corePlugin, IServiceScopeFactory scopeFactory,
        DiscordClient discordClient)
    {
        _plugin = plugin;
        _corePlugin = corePlugin;
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Closes a question, archiving the thread to which it corresponds.
    /// </summary>
    /// <param name="question">The question to close.</param>
    /// <param name="closeReason">The reason for the closure.</param>
    /// <param name="closer">The member who closed the question.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="question" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="closer" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="closeReason" /> is not a valid <see cref="CloseReason" />.
    /// </exception>
    public async Task CloseAsync(Question question, CloseReason closeReason, DiscordMember closer)
    {
        if (question is null) throw new ArgumentNullException(nameof(question));
        if (closer is null) throw new ArgumentNullException(nameof(closer));
        if (!Enum.IsDefined(closeReason)) throw new ArgumentOutOfRangeException(nameof(closeReason));

        if (question.IsClosed)
            return;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        question = context.Entry(question).Entity;

        question.IsClosed = true;
        question.CloseReason = closeReason;
        question.CloserId = closer.Id;
        question.ClosedAt = DateTimeOffset.Now;

        if (closer.Guild.Threads.TryGetValue(question.ThreadId, out DiscordThreadChannel? thread))
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("Question closed");
            embed.WithThumbnail(closer.Guild.IconUrl);
            embed.WithDescription(closer.Id == question.AuthorId
                ? "This question was closed by the asker."
                : $"This question was closed by {closer.Mention}.");
            embed.AddField("Reason", closeReason);
            embed.WithColor(closeReason == CloseReason.Resolved ? DiscordColor.Green : DiscordColor.Red);

            await thread.SendMessageAsync(embed).ConfigureAwait(false);
            await thread.ModifyAsync(model => model.IsArchived = true).ConfigureAwait(false);
        }

        context.Update(question);
        await context.SaveChangesAsync();

        Logger.Info("Question {Id} (channel #{ChannelId}) closed by {Closer}", question.Id, question.ThreadId, closer);
    }

    /// <summary>
    ///     Creates a new question in the help category.
    /// </summary>
    /// <param name="member">The member who asked the question.</param>
    /// <param name="category">The category of the question.</param>
    /// <param name="title">The title of the question.</param>
    /// <returns>The newly-created <see cref="Question" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Question> CreateQuestionAsync(DiscordMember member, QuestionCategory category, string title)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (title is null) throw new ArgumentNullException(nameof(title));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException(ExceptionMessages.TitleCannotBeEmpty, nameof(title));
        if (title.Length < 5) throw new ArgumentException(ExceptionMessages.TitleTooShort, nameof(title));

        var categoryPrefix = $"[{category.Name}] ";
        int categoryPrefixLength = 100 - categoryPrefix.Length;
        string displayTitle = title;
        if (title.Length > categoryPrefixLength) displayTitle = $"{title[..categoryPrefixLength]}...";

        if (!_forumsChannel.TryGetValue(member.Guild, out DiscordChannel? forumsChannel))
            throw new InvalidOperationException(ExceptionMessages.ForumsChannelNotFound);

        DiscordThreadChannel thread = await forumsChannel
            .CreateThreadAsync($"[{category.Name}] {displayTitle}", AutoArchiveDuration.ThreeDays, ChannelType.PublicThread)
            .ConfigureAwait(false);

        var question = Question.Create(category, title, member, thread);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        EntityEntry<Question> entry = await context.Questions.AddAsync(question).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        question = entry.Entity;
        _activeQuestions.Add(question);

        await thread.AddThreadMemberAsync(member).ConfigureAwait(false);
        await thread.ModifyAsync(model => model.Locked = true).ConfigureAwait(false);

        DiscordColor primaryColor = DiscordColor.Purple;
        DiscordColor secondaryColor = DiscordColor.Yellow;

        if (_corePlugin.TryGetGuildConfiguration(member.Guild, out GuildConfiguration? guildConfiguration))
        {
            primaryColor = guildConfiguration.PrimaryColor;
            secondaryColor = guildConfiguration.SecondaryColor;
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle($"Question from {member.GetUsernameWithDiscriminator()}");
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithDescription(question.Title);
        embed.WithFooter(category.Name);
        embed.WithColor(primaryColor);
        await thread.SendMessageAsync(embed).ConfigureAwait(false);

        var builder = new DiscordMessageBuilder();
        builder.WithContent($"{MentionUtility.MentionUser(member.Id)}, to improve your chances of getting help, " +
                            "please keep these tips in mind.");

        embed = new DiscordEmbedBuilder();
        embed.WithTitle("Format code!");
        string codeBlock = Formatter.BlockCode("Console.WriteLine(\"Hello World\");", "cs");
        embed.WithDescription("Send short snippets of code in codeblocks, " +
                              "and use syntax highlighting to make it easier to read.\n\n" +
                              "For example:\n" +
                              Formatter.Sanitize(codeBlock) +
                              "\nwill produce:\n" +
                              codeBlock);
        embed.WithColor(secondaryColor);
        builder.AddEmbed(embed);

        embed = new DiscordEmbedBuilder();
        embed.WithTitle("Use a paste service!");
        embed.WithDescription("To send lengthy code, consider uploading it to " +
                              Formatter.MaskedUrl("PasteMyst", new Uri("https://paste.myst.rs/")) +
                              " and then sending the link to this thread.");
        embed.WithColor(secondaryColor);
        builder.AddEmbed(embed);

        embed = new DiscordEmbedBuilder();
        embed.WithTitle("Be patient!");
        embed.WithDescription("If you don't receive immediate help, it may mean that your question is poorly written, and so " +
                              "answerers may not feel confident in being able to help you. Use this time to provide as much " +
                              "detail as possible, so that you have the best chances of solving the problem.\n\n" +
                              "Keep in mind that the ratio of those that need help, to those that do help, is very small - " +
                              "and those that do help are volunteers, so please be respectful!");
        embed.WithColor(secondaryColor);
        builder.AddEmbed(embed);

        await thread.SendMessageAsync(builder).ConfigureAwait(false);

        Logger.Info("Question in category '{Category}' asked by {Member}: {Title}", category.ToString(), member, question.Title);
        return question;
    }

    /// <summary>
    ///     Gets the <see cref="Question" /> associated with a specified thread channel.
    /// </summary>
    /// <param name="threadChannel">The channel whose question to retrieve.</param>
    /// <returns>
    ///     The <see cref="Question" /> associated with <paramref name="threadChannel" />, or <see langword="null" /> if no active
    ///     question could be found.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="threadChannel" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="threadChannel" /> is not a thread.</exception>
    public async Task<Question?> GetQuestionFromThreadAsync(DiscordChannel threadChannel)
    {
        if (threadChannel is null) throw new ArgumentNullException(nameof(threadChannel));
        if (!threadChannel.IsThread)
            throw new ArgumentException(ExceptionMessages.ChannelMustBeThread, nameof(threadChannel));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        return await context.Questions.FirstOrDefaultAsync(q => q.ThreadId == threadChannel.Id).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a value indicating whether the specified channel is a question thread channel.
    /// </summary>
    /// <param name="channel">The channel whose status to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="channel" /> refers to a question thread; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     This method behaves similarly to <see cref="IsQuestionChannel" />, except it also searches archived questions from the
    ///     database.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="channel" /> is <see langword="null" />.</exception>
    /// <seealso cref="IsQuestionChannel" />
    public async Task<bool> IsArchivedQuestionChannelAsync(DiscordChannel channel)
    {
        if (channel is null) throw new ArgumentNullException(nameof(channel));
        if (IsQuestionChannel(channel)) return true;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        return await context.Questions.AnyAsync(q => q.ThreadId == channel.Id).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a value indicating whether the specified channel is a question thread channel.
    /// </summary>
    /// <param name="channel">The channel whose status to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="channel" /> refers to a question thread; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="channel" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     This method does not account for closed questions. To include closed questions, use
    ///     <see cref="IsArchivedQuestionChannelAsync" />.
    /// </remarks>
    /// <seealso cref="IsArchivedQuestionChannelAsync" />
    public bool IsQuestionChannel(DiscordChannel channel)
    {
        if (channel is null) throw new ArgumentNullException(nameof(channel));
        if (!channel.IsThread) return false;

        return _activeQuestions.Exists(q => q.ThreadId == channel.Id);
    }

    /*public async Task ResetActiveQuestionsChannelAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (!_activeQuestionsChannels.TryGetValue(guild, out DiscordChannel? channel)) return;
        if (channel is null) return;

        IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync().ConfigureAwait(false);
        foreach (DiscordMessage message in messages)
            _ = message.DeleteAsync();

        await UpdateActiveQuestionsMessageAsync();
    }*/

    /*public async Task UpdateActiveQuestionsMessageAsync()
    {
        if (_activeQuestionsChannel is null) return;

        if (_activeQuestionsMessage is null)
        {
            IReadOnlyList<DiscordMessage>? messages = await _activeQuestionsChannel.GetMessagesAsync().ConfigureAwait(false);
            _activeQuestionsMessage = messages.FirstOrDefault(m => m.Author == _discordClient.CurrentUser);
        }

        _activeQuestionsMessage ??= await _activeQuestionsChannel.SendMessageAsync("*Please wait...*").ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine($"__**{"Active Question".ToQuantity(_activeQuestions.Count)}**__");
        builder.AppendLine("*Only the 10 most recent questions are shown for each category.*\n");

        foreach (IGrouping<ulong, Question> category in _activeQuestions.GroupBy(q => q.Category))
        {
            Question[] questions = category.OrderByDescending(q => q.CreatedAt).Take(10).ToArray();
            if (questions.Length == 0) continue;

            DiscordChannel channel = await _discordClient.GetChannelAsync(category.Key).ConfigureAwait(false);
            builder.AppendLine(Formatter.Bold(channel.Name.Titleize()));

            foreach (Question question in questions)
            {
                DiscordUser author = await _discordClient.GetUserAsync(question.AuthorId).ConfigureAwait(false);
                builder.Append(MentionUtility.MentionChannel(question.ThreadId));
                builder.Append($" (asked {Formatter.Timestamp(question.CreatedAt)}");
                builder.AppendLine($" by {author.GetUsernameWithDiscriminator()})");
            }

            builder.AppendLine();
        }

        await _activeQuestionsMessage.ModifyAsync(builder.ToString()).ConfigureAwait(false);
    }*/

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _discordClient.GuildAvailable += OnGuildAvailable;
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        var activeQuestionsChannelId = _plugin.Configuration.Get<ulong>($"{e.Guild.Id}.activeQuestionsChannel");
        var askHereChannelId = _plugin.Configuration.Get<ulong>($"{e.Guild.Id}.askHereChannel");
        var forumsChannelId = _plugin.Configuration.Get<ulong>($"{e.Guild.Id}.forumsChannel");

        if (askHereChannelId != 0)
        {
            if (e.Guild.GetChannel(askHereChannelId) is { } askHereChannel)
                _askHereChannel[e.Guild] = askHereChannel;
        }

        if (activeQuestionsChannelId != 0)
        {
            if (e.Guild.GetChannel(activeQuestionsChannelId) is { } activeQuestionsChannel)
                _activeQuestionsChannel[e.Guild] = activeQuestionsChannel;
        }

        if (forumsChannelId != 0)
        {
            if (e.Guild.GetChannel(forumsChannelId) is { } forumsChannel)
                _forumsChannel[e.Guild] = forumsChannel;
        }

        return Task.CompletedTask;
        // return ResetActiveQuestionsChannelAsync(e.Guild);
    }
}
