using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Infrastructure tables
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    // Feature tables
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Distributor> Distributors => Set<Distributor>();

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

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique();
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.FamilyId);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User)
             .WithMany(x => x.RefreshTokens)
             .HasForeignKey(x => x.UserId)
             .IsRequired(false);
            e.HasQueryFilter(x => x.User == null || !x.User.IsDeleted);
        });

        // Distributor
        modelBuilder.Entity<Distributor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.TradeDiscount).HasColumnType("decimal(5,2)");
            e.Property(x => x.Commission).HasColumnType("decimal(5,2)");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
