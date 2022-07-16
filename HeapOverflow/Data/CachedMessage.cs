using System;

namespace HeapOverflow.Data;

/// <summary>
///     Represents a known message.
/// </summary>
internal sealed class CachedMessage : IEquatable<CachedMessage>
{
    /// <summary>
    ///     Gets the key of this message.
    /// </summary>
    /// <value>The message key.</value>
    public string Key { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the ID of the guild in which this message belongs.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the channel in which this message belongs.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; internal set; }

    /// <summary>
    ///     Gets the message ID.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong MessageId { get; internal set; }

    /// <summary>
    ///     Determines whether two <see cref="CachedMessage" /> instances are equal.
    /// </summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <returns><see langword="true" /> if the two messages are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(CachedMessage? left, CachedMessage? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="CachedMessage" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <returns><see langword="true" /> if the two messages are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(CachedMessage? left, CachedMessage? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="CachedMessage" /> is equal to the current <see cref="CachedMessage" />.
    /// </summary>
    /// <param name="other">The other <see cref="CachedMessage" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified <see cref="CachedMessage" /> is equal to the current
    ///     <see cref="CachedMessage" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(CachedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && GuildId == other.GuildId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CachedMessage other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Key, GuildId);
    }
}
