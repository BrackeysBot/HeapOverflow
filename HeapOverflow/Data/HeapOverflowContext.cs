using System.IO;
using HeapOverflow.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace HeapOverflow.Data;

/// <summary>
///     Represents a session with the HeapOverflow database.
/// </summary>
internal sealed class HeapOverflowContext : DbContext
{
    private const string DatabaseFileName = "heap-overflow.db";
    private readonly string _dataSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HeapOverflowContext" /> class.
    /// </summary>
    /// <param name="plugin">The owning plugin.</param>
    public HeapOverflowContext(HeapOverflowPlugin plugin)
    {
        _dataSource = Path.Combine(plugin.DataDirectory.FullName, DatabaseFileName);
    }

    /// <summary>
    ///     Gets the set of cached messages.
    /// </summary>
    /// <value>The set of cached messages.</value>
    public DbSet<CachedMessage> CachedMessages { get; internal set; } = null!; // initialized by EF

    /// <summary>
    ///     Gets the set of question categories.
    /// </summary>
    /// <value>The set of question categories.</value>
    public DbSet<QuestionCategory> QuestionCategories { get; internal set; } = null!; // initialized by EF

    /// <summary>
    ///     Gets the set of questions.
    /// </summary>
    /// <value>The set of questions.</value>
    public DbSet<Question> Questions { get; internal set; } = null!; // initialized by EF

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite($"Data Source={_dataSource}");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CachedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionCategoryConfiguration());
    }
}
