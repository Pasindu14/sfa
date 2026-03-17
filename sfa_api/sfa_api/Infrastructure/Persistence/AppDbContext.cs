using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.SalesOrders.Entities;
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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PricingStructure> PricingStructures => Set<PricingStructure>();
    public DbSet<PricingStructureItem> PricingStructureItems => Set<PricingStructureItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<SalesOrderHistory> SalesOrderHistories => Set<SalesOrderHistory>();

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
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.RegionId);
            e.HasOne(x => x.Territory).WithMany().HasForeignKey(x => x.TerritoryId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
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
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.RouteId);
            e.HasIndex(x => x.DivisionId);
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).IsRequired();
        });

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.UpdatedAt);
            // NOTE: No HasQueryFilter - we display both active and inactive records
        });

        // PricingStructure
        modelBuilder.Entity<PricingStructure>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDefault);
        });

        // PricingStructureItem
        modelBuilder.Entity<PricingStructureItem>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.DealerPackPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.DealerCasePrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.PromotionalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.PricingStructure).WithMany(p => p.Items).HasForeignKey(x => x.PricingStructureId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PricingStructureId, x.ProductId }).IsUnique();
        });

        // SalesOrder sequence (used to generate order numbers)
        modelBuilder.HasSequence<long>("sales_order_number_seq").StartsAt(1).IncrementsBy(1);

        // SalesOrder
        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.CancelReason).HasMaxLength(500);
            e.HasIndex(x => x.DistributorId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items)
             .WithOne(i => i.SalesOrder)
             .HasForeignKey(i => i.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History)
             .WithOne(h => h.SalesOrder)
             .HasForeignKey(h => h.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SalesOrderItem
        modelBuilder.Entity<SalesOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // SalesOrderHistory
        modelBuilder.Entity<SalesOrderHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Action).IsRequired().HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.ItemsSnapshot).HasColumnType("text");
            e.Property(x => x.FromStatus).HasConversion<int?>();
            e.Property(x => x.ToStatus).HasConversion<int?>();
            e.HasIndex(x => x.SalesOrderId);
            e.HasIndex(x => x.PerformedAt);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
