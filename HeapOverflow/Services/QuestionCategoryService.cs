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
using Humanizer;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using X10D.Text;

namespace HeapOverflow.Services;

/// <summary>
///     Represents a service which manages question categories.
/// </summary>
internal sealed class QuestionCategoryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;

    private readonly Dictionary<DiscordGuild, List<QuestionCategory>> _questionCategories = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="QuestionCategoryService"/> class.
    /// </summary>
    public QuestionCategoryService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Creates a new category for questions.
    /// </summary>
    /// <param name="guild">The guild in which to create the category.</param>
    /// <param name="staffMember">The staff member who created the category.</param>
    /// <param name="name">The name of the category.</param>
    /// <param name="description">Optional. The description of the category. Defaults to <see langword="null" />.</param>
    /// <returns>The newly-created category.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="name" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty, or contains only whitespace.</exception>
    public async Task<QuestionCategory> CreateCategoryAsync(DiscordGuild guild, DiscordMember staffMember,
        string name, string? description = null)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(ExceptionMessages.NameCannotBeEmpty, nameof(name));

        var category = new QuestionCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Titleize(),
            Description = description?.AsNullIfWhiteSpace(),
            GuildId = guild.Id
        };

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        EntityEntry<QuestionCategory> entry = await context.QuestionCategories.AddAsync(category).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        category = entry.Entity;

        if (!_questionCategories.TryGetValue(guild, out List<QuestionCategory>? categories))
        {
            categories = new List<QuestionCategory>();
            _questionCategories.Add(guild, categories);
        }

        categories.Add(category);

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("Help Category Created");
        embed.WithDescription($"A category `{category.Name}` ({category.Id}) has been created by {staffMember.Mention}.");
        embed.WithColor(DiscordColor.Green);
        await _corePlugin.LogAsync(guild, embed).ConfigureAwait(false);

        return category;
    }

    /// <summary>
    ///     Deletes a category from its guild.
    /// </summary>
    /// <param name="category">The category to delete.</param>
    /// <param name="staffMember">The staff member who issued the deletion.</param>
    /// <exception cref="ArgumentNullException"><paramref name="category" /> is <see langword="null" />.</exception>
    public async Task DeleteCategoryAsync(QuestionCategory category, DiscordMember staffMember)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));

        DiscordGuild? guild = staffMember.Guild;
        if (_questionCategories.TryGetValue(guild, out List<QuestionCategory>? categories))
            categories.Remove(category);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        context.QuestionCategories.Remove(category);
        await context.SaveChangesAsync().ConfigureAwait(false);

        if (guild is not null)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("Help Category Deleted");
            embed.WithDescription($"The category `{category.Name}` ({category.Id}) has been deleted by {staffMember.Mention}.");
            embed.WithColor(DiscordColor.Red);
            await _corePlugin.LogAsync(guild, embed).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Gets all categories within the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose categories to retrieve.</param>
    /// <returns>A read-only view of the <see cref="QuestionCategory" /> instances defined within the specified guild.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyCollection<QuestionCategory> GetCategories(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        if (!_questionCategories.TryGetValue(guild, out List<QuestionCategory>? categories))
            return ArraySegment<QuestionCategory>.Empty;

        return categories.AsReadOnly();
    }

    /// <summary>
    ///     Gets the category with the specified ID.
    /// </summary>
    /// <param name="guild">The guild whose categories to search.</param>
    /// <param name="id">The ID of the category to find.</param>
    /// <returns>
    ///     The category whose <see cref="QuestionCategory.Id" /> is equal to <paramref name="id" />, or <see langword="null" />
    ///     if no such match was found.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public QuestionCategory? GetCategory(DiscordGuild guild, Guid id)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        if (!_questionCategories.TryGetValue(guild, out List<QuestionCategory>? categories))
            return null;

        return categories.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    ///     Gets the category with the specified name.
    /// </summary>
    /// <param name="guild">The guild whose categories to search.</param>
    /// <param name="name">The name of the category to find.</param>
    /// <returns>
    ///     The category whose <see cref="QuestionCategory.Name" /> is equal to <paramref name="name" />;
    ///     <see langword="null" /> if no such match was found, or if <paramref name="name" /> is <see langword="null" />, empty,
    ///     or consists of only whitespace.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public QuestionCategory? GetCategory(DiscordGuild guild, string name)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (!_questionCategories.TryGetValue(guild, out List<QuestionCategory>? categories))
            return null;

        return categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Modifies a category.
    /// </summary>
    /// <param name="category">The category to modify.</param>
    /// <param name="action">A delegate containing the modification.</param>
    /// <param name="staffMember">The staff member who performed the modification.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="category"/> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="action"/> is <see langword="null" />.</para>
    /// </exception>
    public async Task ModifyCategoryAsync(QuestionCategory category, Action<QuestionCategory> action, DiscordMember staffMember)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));
        if (action is null) throw new ArgumentNullException(nameof(action));

        string oldName = category.Name;
        string? oldDescription = category.Description;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        category = context.Entry(category).Entity;
        action(category);
        context.QuestionCategories.Update(category);
        await context.SaveChangesAsync().ConfigureAwait(false);

        if (_discordClient.Guilds.TryGetValue(category.GuildId, out DiscordGuild? guild))
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("Help Category Modified");
            embed.WithDescription($"The category `{category.Name}` ({category.Id}) has been modified by {staffMember.Mention}.");
            embed.WithColor(DiscordColor.Orange);
            embed.AddField("Old Name", oldName, true);
            embed.AddField("New Name", category.Name, true);
            embed.AddField("Old Description", oldDescription?.WithWhiteSpaceAlternative("<none>"));
            embed.AddField("New Description", category.Description?.WithWhiteSpaceAlternative("<none>"));
            await _corePlugin.LogAsync(guild, embed).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _discordClient.GuildAvailable += OnGuildAvailable;
    }

    private async Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();

        if (!_questionCategories.TryGetValue(e.Guild, out List<QuestionCategory>? categories))
            _questionCategories.Add(e.Guild, categories = new List<QuestionCategory>());

        categories.AddRange(context.QuestionCategories);
    }
}
