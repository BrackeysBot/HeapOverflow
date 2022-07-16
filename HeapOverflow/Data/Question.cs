using System;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace HeapOverflow.Data;

/// <summary>
///     Represents a question.
/// </summary>
internal sealed class Question : IEquatable<Question>
{
    /// <summary>
    ///     Gets the ID of the user who authored the question.
    /// </summary>
    /// <value>The author of the question.</value>
    public ulong AuthorId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the category the question belongs to.
    /// </summary>
    /// <value>The category ID.</value>
    /// <remarks>This value is equivalent to the ID of the channel in which this question was asked.</remarks>
    public Guid CategoryId { get; internal set; }

    /// <summary>
    ///     Gets the date and time at which the question was closed.
    /// </summary>
    /// <value>
    ///     The date and time at which this question was asked, or <see langword="null" /> if <see cref="IsClosed" /> is
    ///     <see langword="false" />.
    /// </value>
    public DateTimeOffset? ClosedAt { get; internal set; }

    /// <summary>
    ///     Gets the reason for the question's closure.
    /// </summary>
    /// <value>The close reason, or <see langword="null" /> if <see cref="IsClosed" /> is <see langword="false" />.</value>
    public CloseReason? CloseReason { get; internal set; }

    /// <summary>
    ///     Gets the ID of the user who closed the question.
    /// </summary>
    /// <value>The closer of the question.</value>
    public ulong CloserId { get; internal set; }

    /// <summary>
    ///     Gets or sets the creation timestamp of the question.
    /// </summary>
    /// <value>The date and time at which this question was posted.</value>
    public DateTimeOffset CreatedAt { get; internal set; }

    /// <summary>
    ///     Gets the ID of the guild in which the question was asked.
    /// </summary>
    /// <value>The ID of the guild.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the question.
    /// </summary>
    /// <value>The ID of the question.</value>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    /// <summary>
    ///     Gets a value indicating whether this question is closed.
    /// </summary>
    /// <value><see langword="true" /> if the question is closed; otherwise, <see langword="false" />.</value>
    public bool IsClosed { get; internal set; }

    /// <summary>
    ///     Gets a mutable list of the tags in this question.
    /// </summary>
    /// <value>A <see cref="List{T}" /> of <see cref="string" /> values representing the tags of this question.</value>
    public List<string> Tags { get; internal set; } = new();

    /// <summary>
    ///     Gets the ID of the thread which this question created.
    /// </summary>
    /// <value>The thread ID.</value>
    public ulong ThreadId { get; internal set; }

    /// <summary>
    ///     Gets or sets the title of this question.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Constructs a <see cref="Question" /> instance.
    /// </summary>
    /// <param name="title">The question title.</param>
    /// <param name="category">The category of the question.</param>
    /// <param name="asker">The member who asked the question.</param>
    /// <param name="thread">The thread channel.</param>
    /// <returns>The newly-created <see cref="Question" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="category" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="title" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="asker" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="thread" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="title" /> is empty, or consists of only whitespace.</exception>
    public static Question Create(QuestionCategory category, string title, DiscordMember asker, DiscordThreadChannel thread)
    {
        if (category is null) throw new ArgumentNullException(nameof(category));
        if (title is null) throw new ArgumentNullException(nameof(title));
        if (asker is null) throw new ArgumentNullException(nameof(asker));
        if (thread is null) throw new ArgumentNullException(nameof(thread));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException(ExceptionMessages.TitleCannotBeEmpty, nameof(title));

        return new Question
        {
            Id = Guid.NewGuid(),
            AuthorId = asker.Id,
            CategoryId = category.Id,
            GuildId = thread.Guild.Id,
            Title = title,
            ThreadId = thread.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    ///     Determines whether two <see cref="Question" /> instances are equal.
    /// </summary>
    /// <param name="left">The first question.</param>
    /// <param name="right">The second question.</param>
    /// <returns><see langword="true" /> if the two questions are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(Question? left, Question? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="Question" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first question.</param>
    /// <param name="right">The second question.</param>
    /// <returns><see langword="true" /> if the two questions are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Question? left, Question? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="Question" /> is equal to the current <see cref="Question" />.
    /// </summary>
    /// <param name="other">The other <see cref="Question" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified <see cref="Question" /> is equal to the current <see cref="Question" />;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Question? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Question other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Id);
    }
}
