using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using HeapOverflow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HeapOverflow.Services;

/// <summary>
///     Represents a service which manages cached messages.
/// </summary>
internal sealed class CachedMessageService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly Dictionary<ulong, Dictionary<string, CachedMessage>> _cachedMessages = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="CachedMessageService" /> class.
    /// </summary>
    public CachedMessageService(IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Caches a message with the specified key. If the key already exists, the message is overwritten.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="key" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para><paramref name="key" /> is empty, or contains only whitespace.</para>
    ///     -or-
    ///     <para><paramref name="message" /> refers to a non-guild message.</para>
    /// </exception>
    public async Task CacheMessageAsync(string key, DiscordMessage message)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException(ExceptionMessages.KeyCannotBeEmpty, nameof(key));
        if (message is null) throw new ArgumentNullException(nameof(message));

        DiscordChannel channel = message.Channel;
        DiscordGuild? guild = channel.Guild;
        if (guild is null) throw new ArgumentException(ExceptionMessages.ChannelMustBeInGuild, nameof(message));

        if (!_cachedMessages.TryGetValue(guild.Id, out Dictionary<string, CachedMessage>? cachedMessages))
        {
            cachedMessages = new Dictionary<string, CachedMessage>();
            _cachedMessages.Add(guild.Id, cachedMessages);
        }

        var cachedMessage = new CachedMessage
        {
            Key = key,
            GuildId = guild.Id,
            ChannelId = channel.Id,
            MessageId = message.Id
        };
        cachedMessages[key] = cachedMessage;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();

        if (await context.CachedMessages.AnyAsync(m => m.Key == key && m.GuildId == guild.Id).ConfigureAwait(false))
            context.CachedMessages.Update(cachedMessage);
        else
            await context.CachedMessages.AddAsync(cachedMessage).ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the <see cref="DiscordMessage" /> associated with a specified cached message.
    /// </summary>
    /// <param name="key">The key of the message to retrieve.</param>
    /// <param name="guildId">The ID of the guild whose messages to search.</param>
    /// <returns>The associated <see cref="DiscordMessage" />, or <see langword="null" /> if no such message was found.</returns>
    public async Task<DiscordMessage?> GetDiscordMessageAsync(string key, ulong guildId)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (!TryGetCachedMessage(guildId, key, out CachedMessage? cachedMessage))
            return null;

        return await GetDiscordMessageAsync(cachedMessage).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the <see cref="DiscordMessage" /> associated with a specified cached message.
    /// </summary>
    /// <param name="key">The key of the message to retrieve.</param>
    /// <param name="guild">The guild whose messages to search.</param>
    /// <returns>The associated <see cref="DiscordMessage" />, or <see langword="null" /> if no such message was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public Task<DiscordMessage?> GetDiscordMessageAsync(string key, DiscordGuild guild)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (guild is null) return Task.FromException<DiscordMessage?>(new ArgumentNullException(nameof(guild)));
        return GetDiscordMessageAsync(key, guild.Id);
    }

    /// <summary>
    ///     Gets the <see cref="DiscordMessage" /> associated with a specified <see cref="CachedMessage" />.
    /// </summary>
    /// <param name="cachedMessage">The cached message.</param>
    /// <returns>The associated <see cref="DiscordMessage" />, or <see langword="null" /> if no such message was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cachedMessage" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> GetDiscordMessageAsync(CachedMessage cachedMessage)
    {
        if (cachedMessage is null) throw new ArgumentNullException(nameof(cachedMessage));
        if (!_discordClient.Guilds.TryGetValue(cachedMessage.GuildId, out DiscordGuild? guild))
            return null;

        if (!guild.Channels.TryGetValue(cachedMessage.ChannelId, out DiscordChannel? channel))
            return null;

        try
        {
            return await channel.GetMessageAsync(cachedMessage.MessageId).ConfigureAwait(false);
        }
        catch (DiscordException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Attempts to find a cached message with the specified key.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose cached messages to search.</param>
    /// <param name="key">The key of the message to retrieve.</param>
    /// <param name="message">
    ///     When this method returns, contains the cached message with the matching key; or <see langword="null" /> if no such
    ///     match was found.
    /// </param>
    /// <returns><see langword="true" /> if the cached message could be retrieved; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="key" /> is empty, or consists of only whitespace.</exception>
    public bool TryGetCachedMessage(ulong guildId, string key, [NotNullWhen(true)] out CachedMessage? message)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException(ExceptionMessages.KeyCannotBeEmpty, nameof(key));

        if (!_cachedMessages.TryGetValue(guildId, out Dictionary<string, CachedMessage>? cachedMessages))
        {
            message = null;
            return false;
        }

        if (!cachedMessages.TryGetValue(key, out CachedMessage? cachedMessage))
        {
            message = null;
            return false;
        }

        message = cachedMessage;
        return true;
    }

    /// <summary>
    ///     Attempts to find a cached message with the specified key.
    /// </summary>
    /// <param name="guild">The guild whose cached messages to search.</param>
    /// <param name="key">The key of the message to retrieve.</param>
    /// <param name="message">
    ///     When this method returns, contains the cached message with the matching key; or <see langword="null" /> if no such
    ///     match was found.
    /// </param>
    /// <returns><see langword="true" /> if the cached message could be retrieved; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="key" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="key" /> is empty, or consists of only whitespace.</exception>
    public bool TryGetCachedMessage(DiscordGuild guild, string key, [NotNullWhen(true)] out CachedMessage? message)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        return TryGetCachedMessage(guild.Id, key, out message);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HeapOverflowContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        await foreach (CachedMessage cachedMessage in context.CachedMessages)
        {
            if (!_cachedMessages.TryGetValue(cachedMessage.GuildId, out Dictionary<string, CachedMessage>? cachedMessages))
            {
                cachedMessages = new Dictionary<string, CachedMessage>();
                _cachedMessages.Add(cachedMessage.GuildId, cachedMessages);
            }

            cachedMessages[cachedMessage.Key] = cachedMessage;
        }
    }
}
