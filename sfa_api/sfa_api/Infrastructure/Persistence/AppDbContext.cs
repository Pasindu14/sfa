using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Users.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

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
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Territory> Territories => Set<Territory>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<Outlet> Outlets => Set<Outlet>();

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
            // NOTE: No HasQueryFilter - we display both active and inactive records
            // Soft delete is for audit purposes only, records are never physically removed
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .IsRequired(false);
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
            // NOTE: No query filter - refresh tokens are tied to users via FK relationship
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
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt);
            // NOTE: No HasQueryFilter - we display both active and inactive records
            // Soft delete is for audit purposes only, records are never physically removed
        });

        // Region
        modelBuilder.Entity<Region>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.UpdatedAt);
        });

        // Area
        modelBuilder.Entity<Area>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.Name, x.RegionId }).IsUnique();
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Region)
             .WithMany()
             .HasForeignKey(x => x.RegionId)
             .IsRequired();
        });

        // Territory
        modelBuilder.Entity<Territory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.Name, x.AreaId }).IsUnique();
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Area)
             .WithMany()
             .HasForeignKey(x => x.AreaId)
             .IsRequired();
            e.HasOne(x => x.Region)
             .WithMany()
             .HasForeignKey(x => x.RegionId)
             .IsRequired();
        });

        // Division
        modelBuilder.Entity<Division>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.Name, x.TerritoryId }).IsUnique();
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Territory)
             .WithMany()
             .HasForeignKey(x => x.TerritoryId)
             .IsRequired();
            e.HasOne(x => x.Area)
             .WithMany()
             .HasForeignKey(x => x.AreaId)
             .IsRequired();
            e.HasOne(x => x.Region)
             .WithMany()
             .HasForeignKey(x => x.RegionId)
             .IsRequired();
        });

        // Route
        modelBuilder.Entity<RouteEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.DivisionId);
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasOne(x => x.Division).WithMany().HasForeignKey(x => x.DivisionId).IsRequired();
            e.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).IsRequired();
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).IsRequired();
            e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).IsRequired();
        });

        // Outlet
        modelBuilder.Entity<Outlet>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.NicNo);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.RouteId);
            e.HasIndex(x => x.DivisionId);
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).IsRequired();
            // NOTE: No HasQueryFilter - we display both active and inactive records
            // Soft delete is for audit purposes only, records are never physically removed
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
