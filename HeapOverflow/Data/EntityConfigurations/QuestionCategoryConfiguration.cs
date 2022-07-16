using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HeapOverflow.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines entity configuration for a <see cref="Question" />.
/// </summary>
internal sealed class QuestionCategoryConfiguration : IEntityTypeConfiguration<QuestionCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<QuestionCategory> builder)
    {
        builder.ToTable(nameof(QuestionCategory));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasConversion<GuidToBytesConverter>();
        builder.Property(e => e.GuildId);
        builder.Property(e => e.Name);
        builder.Property(e => e.Description);
    }
}
