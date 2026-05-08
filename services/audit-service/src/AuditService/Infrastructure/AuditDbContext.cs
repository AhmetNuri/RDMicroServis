using AuditService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Infrastructure;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditEventPg> AuditEvents => Set<AuditEventPg>()!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditEventPg>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ResourceType);

            entity.HasGeneratedTsVectorColumn(
                p => p.SearchVector!,
                "english",
                p => new { p.EventType, p.Action, p.ResourceType })
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN");
        });
    }
}
