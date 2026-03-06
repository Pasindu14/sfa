using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;

namespace sfa_api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Infrastructure tables
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdempotencyKey>(e => {
            e.HasKey(x => x.Key);
            e.HasIndex(x => x.ExpiresAt);
        });
        modelBuilder.Entity<RevokedToken>(e => {
            e.HasKey(x => x.Jti);
            e.HasIndex(x => x.ExpiresAt);
        });
        modelBuilder.Entity<AuditLog>(e => {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.ChangedAt);
            e.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
