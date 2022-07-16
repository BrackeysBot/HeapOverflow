using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.Core.API;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HeapOverflow.Data;
using Microsoft.Extensions.Hosting;
using CoreGuildConfiguration = BrackeysBot.Core.API.Configuration.GuildConfiguration;

namespace HeapOverflow.Services;

/// <summary>
///     Represents a service which listens for incoming questions.
/// </summary>
internal sealed class QuestionSubmissionService : BackgroundService
{
    private readonly HeapOverflowPlugin _plugin;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly CachedMessageService _cachedMessageService;
    private readonly QuestionCategoryService _categoryService;
    private readonly QuestionService _questionService;
    private readonly Dictionary<DiscordUser, QuestionCategory> _chosenCategories = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="QuestionSubmissionService" /> class.
    /// </summary>
    public QuestionSubmissionService(HeapOverflowPlugin plugin, ICorePlugin corePlugin, DiscordClient discordClient,
        CachedMessageService cachedMessageService, QuestionCategoryService categoryService, QuestionService questionService)
    {
        _plugin = plugin;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _cachedMessageService = cachedMessageService;
        _categoryService = categoryService;
        _questionService = questionService;
    }

    /// <summary>
    ///     Posts the question submission embed into the "Ask Here" channel for the specified guild.
    /// </summary>
    /// <param name="channel">The guild in which to post the embed.</param>
    /// <param name="message">
    ///     The message to modify, if any. If <see langword="null" />, the existing message will be found. If no message exists,
    ///     a new one is sent.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="channel" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="channel" /> is not a guild channel.</exception>
    public async Task PostQuestionSubmissionEmbedAsync(DiscordChannel channel, DiscordMessage? message = null)
    {
        if (channel is null) throw new ArgumentNullException(nameof(channel));
        if (channel.Guild is null) throw new ArgumentException(ExceptionMessages.ChannelMustBeInGuild, nameof(channel));

        DiscordColor embedColor = DiscordColor.Purple;
        if (_corePlugin.TryGetGuildConfiguration(channel.Guild, out CoreGuildConfiguration? configuration))
            embedColor = configuration.PrimaryColor;

        string embedDescription = _plugin.Configuration.Get("messages.askHereEmbed", EmbedMessages.AskHere);

        var options = new List<DiscordSelectComponentOption>();
        var builder = new DiscordMessageBuilder();
        var embed = new DiscordEmbedBuilder
        {
            Title = "❓ Open a new question",
            Description = embedDescription,
            Color = embedColor
        };

        builder.AddEmbed(embed);

        foreach (QuestionCategory category in _categoryService.GetCategories(channel.Guild).OrderBy(c => c.Name))
        {
            string name = category.Name;
            string? description = category.Description;
            var value = category.Id.ToString("N");
            options.Add(new DiscordSelectComponentOption(name, value, description));
        }

        if (options.Count == 0)
        {
            embed.Title = "❌ No categories found";
            embed.Description = "There are no categories available for questions.";
            embed.Color = DiscordColor.Red;
            await channel.SendMessageAsync(embed).ConfigureAwait(false);
            return;
        }

        var select = new DiscordSelectComponent("askhere-category", "Select a category", options);
        builder.AddComponents(select);

        message ??= await _cachedMessageService.GetDiscordMessageAsync("askHereMessage", channel.Guild).ConfigureAwait(false);
        if (message is null)
        {
            message = await channel.SendMessageAsync(builder).ConfigureAwait(false);
            await _cachedMessageService.CacheMessageAsync("askHereMessage", message).ConfigureAwait(false);
        }
        else
        {
            await message.ModifyAsync(builder).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.ComponentInteractionCreated += OnComponentInteractionCreated;
        _discordClient.ModalSubmitted += OnModalSubmitted;
        return Task.CompletedTask;
    }

    private async Task OnModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
    {
        DiscordInteraction interaction = e.Interaction;
        if (interaction.Data.CustomId == $"ask-{interaction.User.Id}" &&
            _chosenCategories.TryGetValue(interaction.User, out QuestionCategory? category))
        {
            _chosenCategories.Remove(interaction.User);

            IEnumerable<DiscordComponent> components = interaction.Data.Components.SelectMany(c => c.Components);
            IEnumerable<TextInputComponent> inputs = components.OfType<TextInputComponent>();
            string? title = inputs.FirstOrDefault(c => c.CustomId == "title")?.Value;

            if (!string.IsNullOrWhiteSpace(title))
                await _questionService.CreateQuestionAsync((DiscordMember) interaction.User, category, title);
        }
    }

    private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id == "askhere-category")
            await SubmitQuestionAsync(e);
    }

    private async Task SubmitQuestionAsync(ComponentInteractionCreateEventArgs e)
    {
        if (!Guid.TryParse(e.Values[0], out Guid categoryId)) return;

        QuestionCategory? category = _categoryService.GetCategory(e.Guild, categoryId);
        if (category is not null)
        {
            _chosenCategories[e.User] = category;

            var builder = new DiscordInteractionResponseBuilder();
            builder.WithCustomId($"ask-{e.User.Id}");
            builder.WithTitle("Open a new question");

            var titleComponent = new TextInputComponent("Briefly describe the problem", "title", min_length: 15, max_length: 100);
            builder.AddComponents(titleComponent);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, builder);
        }
    }
}
