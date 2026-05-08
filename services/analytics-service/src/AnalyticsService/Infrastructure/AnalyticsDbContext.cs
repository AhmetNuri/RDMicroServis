using AnalyticsService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Infrastructure;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MetricName);
            entity.HasIndex(e => e.RecordedAt);
            entity.HasIndex(e => e.ServiceName);
            entity.Property(e => e.MetricValue).HasPrecision(18, 4);
            entity.Property(e => e.Dimensions)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
        });

        modelBuilder.Entity<DailySummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date).IsUnique();
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
        });
    }
}
