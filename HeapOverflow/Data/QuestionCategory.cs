using System;

namespace HeapOverflow.Data;

/// <summary>
///     Represents a category for a question to be in.
/// </summary>
internal sealed class QuestionCategory : IEquatable<QuestionCategory>
{
    /// <summary>
    ///     Gets or sets the description of this category.
    /// </summary>
    /// <value>The description of this category.</value>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets the guild in which this category was created.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the category.
    /// </summary>
    /// <value>The category ID.</value>
    public Guid Id { get; internal set; }

    /// <summary>
    ///     Gets or sets the name of this category.
    /// </summary>
    /// <value>The name of this category.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Determines whether two <see cref="QuestionCategory" /> instances are equal.
    /// </summary>
    /// <param name="left">The first category.</param>
    /// <param name="right">The second category.</param>
    /// <returns><see langword="true" /> if the two categories are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(QuestionCategory? left, QuestionCategory? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="QuestionCategory" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first category.</param>
    /// <param name="right">The second category.</param>
    /// <returns><see langword="true" /> if the two categories are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(QuestionCategory? left, QuestionCategory? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="QuestionCategory" /> is equal to the current
    ///     <see cref="QuestionCategory" />.
    /// </summary>
    /// <param name="other">The other <see cref="QuestionCategory" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified <see cref="QuestionCategory" /> is equal to the current
    ///     <see cref="QuestionCategory" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(QuestionCategory? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is QuestionCategory other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
