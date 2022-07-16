using HeapOverflow.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HeapOverflow.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines entity configuration for a <see cref="Question" />.
/// </summary>
internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable(nameof(Question));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasConversion<GuidToBytesConverter>();
        builder.Property(e => e.CategoryId).HasConversion<GuidToBytesConverter>();
        builder.Property(e => e.GuildId);
        builder.Property(e => e.CreatedAt).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.Title);
        builder.Property(e => e.Tags).HasConversion<ListOfStringToBytesConverter>();
        builder.Property(e => e.IsClosed);
    }
}
