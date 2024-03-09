using Microsoft.EntityFrameworkCore;
using Traditional.PerformanceTests.Infrastructure.DataModels;

namespace Traditional.PerformanceTests.Infrastructure;

/// <inheritdoc />
public class PerformanceDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets a db set for the test run.
    /// </summary>
    public DbSet<TestRun> TestRuns { get; init; } = null!;

    /// <summary>
    /// Gets the data points of the test runs.
    /// </summary>
    public DbSet<DataPoint> DataPoints { get; init; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTestRun(modelBuilder);
        ConfigureDataPoint(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    private static void ConfigureTestRun(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestRun>()
            .HasKey(testResult => testResult.Id);

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.TestName)
            .IsRequired();

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.SummaryContent)
            .IsRequired()
            .HasColumnType("jsonb");

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.HtmlReport)
            .IsRequired();

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.LogsStdout)
            .IsRequired(false);

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.LogsStderr)
            .IsRequired(false);

        modelBuilder.Entity<TestRun>()
            .Property(testRun => testRun.Metadata)
            .IsRequired(false);

        modelBuilder.Entity<TestRun>()
            .HasMany(testRun => testRun.DataPoints)
            .WithOne(dataPoint => dataPoint.TestRun)
            .HasForeignKey(dataPoint => dataPoint.TestRunId);
    }

    private static void ConfigureDataPoint(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataPoint>()
            .HasKey(dataPoint => dataPoint.Id);

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.Timestamp)
            .IsRequired();

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.MetricName)
            .IsRequired();

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.MetricType)
            .IsRequired();

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.DataType)
            .IsRequired();

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.Value)
            .IsRequired();

        modelBuilder.Entity<DataPoint>()
            .HasOne(dataPoint => dataPoint.TestRun)
            .WithMany(testRun => testRun.DataPoints)
            .HasForeignKey(dataPoint => dataPoint.TestRunId);

        modelBuilder.Entity<DataPoint>()
            .Property(dataPoint => dataPoint.Tags)
            .IsRequired(false);
    }
}
