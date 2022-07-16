using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeapOverflow.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines entity configuration for a <see cref="Question" />.
/// </summary>
internal sealed class CachedMessageConfiguration : IEntityTypeConfiguration<CachedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CachedMessage> builder)
    {
        builder.ToTable(nameof(CachedMessage));
        builder.HasKey(e => new {e.GuildId, e.Key});

        builder.Property(e => e.Key);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.MessageId);
    }
}
