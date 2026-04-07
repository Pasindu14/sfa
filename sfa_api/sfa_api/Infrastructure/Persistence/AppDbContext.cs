using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.ProductCategoryPricings.Entities;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.DailyRouteAssignments.Enums;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserReportingLines.Entities;
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
    public DbSet<ProductCategoryPrice> ProductCategoryPrices => Set<ProductCategoryPrice>();
    public DbSet<PricingStructure> PricingStructures => Set<PricingStructure>();
    public DbSet<PricingStructureItem> PricingStructureItems => Set<PricingStructureItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseOrderHistory> PurchaseOrderHistories => Set<PurchaseOrderHistory>();
    public DbSet<SalesInvoiceImportBatch> SalesInvoiceImportBatches => Set<SalesInvoiceImportBatch>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<GRN> GRNs => Set<GRN>();
    public DbSet<GRNItem> GRNItems => Set<GRNItem>();
    public DbSet<DistributorStock> DistributorStocks => Set<DistributorStock>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<UserReportingLine> UserReportingLines => Set<UserReportingLine>();
    public DbSet<UserGeoAssignment> UserGeoAssignments => Set<UserGeoAssignment>();
    public DbSet<DailyRouteAssignment> DailyRouteAssignments => Set<DailyRouteAssignment>();

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
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
        });

        // Area
        modelBuilder.Entity<Area>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => new { x.Name, x.RegionId }).IsUnique();
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
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
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
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
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
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

        // UserReportingLine
        modelBuilder.Entity<UserReportingLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.ReportsToUserId);
            e.HasIndex(x => new { x.UserId, x.IsActive });
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.EffectiveFrom);
            // Both FKs point to Users — restrict delete to protect audit trail
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReportsToUser)
             .WithMany()
             .HasForeignKey(x => x.ReportsToUserId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);
        });

        // UserGeoAssignment
        modelBuilder.Entity<UserGeoAssignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.DivisionId);
            e.HasIndex(x => new { x.UserId, x.IsActive });
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.EffectiveFrom);
            // All FKs use Restrict — no cascades, preserving full audit history
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Division)
             .WithMany()
             .HasForeignKey(x => x.DivisionId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Territory)
             .WithMany()
             .HasForeignKey(x => x.TerritoryId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Area)
             .WithMany()
             .HasForeignKey(x => x.AreaId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Region)
             .WithMany()
             .HasForeignKey(x => x.RegionId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Route
        modelBuilder.Entity<RouteEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.Name).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
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
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.Name).HasFilter("\"IsActive\" = true");
            e.HasQueryFilter(x => x.IsActive);
            e.HasIndex(x => x.RouteId);
            e.HasIndex(x => x.DivisionId);
            e.HasIndex(x => x.TerritoryId);
            e.HasIndex(x => x.AreaId);
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.UpdatedAt).HasFilter("\"IsActive\" = true");
            e.HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).IsRequired();
        });

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.UpdatedAt);
            // NOTE: No HasQueryFilter — repositories use IgnoreQueryFilters() throughout and
            // filter IsActive explicitly. A global filter here causes EF warnings because
            // GRNItem, PurchaseOrderItem, SalesInvoiceItem, DistributorStock, StockTransaction
            // all have required FKs to Product.
        });

        // ProductCategoryPrice
        modelBuilder.Entity<ProductCategoryPrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Price).HasColumnType("decimal(18,4)");
            e.HasIndex(x => new { x.ProductId, x.Category }).IsUnique();
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PricingStructure
        modelBuilder.Entity<PricingStructure>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.IsDefault);
            // NOTE: No HasQueryFilter — repositories use IgnoreQueryFilters() throughout and
            // filter IsActive explicitly. A global filter here causes EF warnings because
            // PricingStructureItem has a required FK to PricingStructure.
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

        // PurchaseOrder sequence (used to generate order numbers)
        modelBuilder.HasSequence<long>("purchase_order_number_seq").StartsAt(1).IncrementsBy(1);

        // PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
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
             .WithOne(i => i.PurchaseOrder)
             .HasForeignKey(i => i.PurchaseOrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History)
             .WithOne(h => h.PurchaseOrder)
             .HasForeignKey(h => h.PurchaseOrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseOrderItem
        modelBuilder.Entity<PurchaseOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseOrderHistory
        modelBuilder.Entity<PurchaseOrderHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Action).IsRequired().HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.ItemsSnapshot).HasColumnType("text");
            e.Property(x => x.FromStatus).HasConversion<string?>();
            e.Property(x => x.ToStatus).HasConversion<string?>();
            e.HasIndex(x => x.PurchaseOrderId);
            e.HasIndex(x => x.PerformedAt);
        });

        // SalesInvoiceImportBatch sequence (used to generate batch numbers)
        modelBuilder.HasSequence<long>("sales_invoice_import_batch_number_seq").StartsAt(1).IncrementsBy(1);

        // SalesInvoiceImportBatch
        modelBuilder.Entity<SalesInvoiceImportBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.BatchNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.BatchNumber).IsUnique();
            e.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.ErrorSummary).HasColumnType("text");
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.ImportedBy);
            e.HasIndex(x => x.ImportedAt);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Importer)
             .WithMany()
             .HasForeignKey(x => x.ImportedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // SalesInvoice
        modelBuilder.Entity<SalesInvoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.VchBillNo).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.VchBillNo).IsUnique();
            e.Property(x => x.BusyOrderRequestNo).HasMaxLength(50);
            e.Property(x => x.SfaPoNumber).HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.InvoiceType).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.DistributorId);
            e.HasIndex(x => x.ImportBatchId);
            e.HasIndex(x => x.PurchaseOrderId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.InvoiceDate);
            // Composite covering index for the common combined filter in GetListAsync
            e.HasIndex(x => new { x.DistributorId, x.Status, x.InvoiceDate });
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PurchaseOrder)
             .WithMany()
             .HasForeignKey(x => x.PurchaseOrderId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ImportBatch)
             .WithMany()
             .HasForeignKey(x => x.ImportBatchId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items)
             .WithOne(i => i.SalesInvoice)
             .HasForeignKey(i => i.SalesInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SalesInvoiceItem
        modelBuilder.Entity<SalesInvoiceItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ItemErpCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.Unit).IsRequired().HasMaxLength(20);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.SalesInvoiceId);
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GRN sequence ──────────────────────────────────────────────────────
        modelBuilder.HasSequence<long>("grn_number_seq").StartsAt(1).IncrementsBy(1);

        // ── GRN ───────────────────────────────────────────────────────────────
        modelBuilder.Entity<GRN>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.GrnNumber).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.GrnNumber).IsUnique();
            e.HasIndex(x => x.IsDeleted);
            // Unique FK — enforces 1:1 with SalesInvoice (no double-GRN at DB level)
            e.HasIndex(x => x.SalesInvoiceId).IsUnique();
            e.Property(x => x.Status)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.SalesInvoice)
             .WithMany()
             .HasForeignKey(x => x.SalesInvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ConfirmedByUser)
             .WithMany()
             .HasForeignKey(x => x.ConfirmedBy)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Items)
             .WithOne(i => i.GRN)
             .HasForeignKey(i => i.GrnId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GRNItem ───────────────────────────────────────────────────────────
        modelBuilder.Entity<GRNItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.Unit).IsRequired().HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => x.GrnId);
            e.HasIndex(x => x.ProductId);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DistributorStock ──────────────────────────────────────────────────
        modelBuilder.Entity<DistributorStock>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.QuantityOnHand).HasColumnType("decimal(18,4)");
            // Composite unique — one row per distributor+product
            e.HasIndex(x => new { x.DistributorId, x.ProductId }).IsUnique();
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── StockTransaction ──────────────────────────────────────────────────
        modelBuilder.Entity<StockTransaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TransactionType)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(x => x.Direction)
             .HasConversion<string>()
             .HasMaxLength(5);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.QuantityBefore).HasColumnType("decimal(18,4)");
            e.Property(x => x.QuantityAfter).HasColumnType("decimal(18,4)");
            e.Property(x => x.ReferenceType).IsRequired().HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(500);
            // Indexed for common queries: by distributor, by product, by reference
            e.HasIndex(x => x.DistributorId);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
            // Composite covering index for transaction history queries (DistributorId + ProductId, sorted by date desc)
            e.HasIndex(x => new { x.DistributorId, x.ProductId, x.TransactedAt })
             .IsDescending(false, false, true);
            e.HasOne(x => x.Distributor)
             .WithMany()
             .HasForeignKey(x => x.DistributorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TransactedByUser)
             .WithMany()
             .HasForeignKey(x => x.TransactedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // DailyRouteAssignment
        modelBuilder.Entity<DailyRouteAssignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.RouteId);
            e.HasIndex(x => x.AssignedDate);
            e.HasIndex(x => new { x.UserId, x.AssignedDate }).IsUnique()
             .HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => new { x.RouteId, x.AssignedDate })
             .HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.IsDeleted);
            // Matching query filter prevents EF warning about Route's global IsActive filter
            // being the required end of this relationship. Deleted assignments are excluded
            // globally; repos that need them call IgnoreQueryFilters().
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Route)
             .WithMany()
             .HasForeignKey(x => x.RouteId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.DeletionStatus)
             .HasConversion<int>()
             .HasDefaultValue(DailyRouteAssignmentDeletionStatus.None);
            e.Property(x => x.DeletionRequestReason).HasMaxLength(500);
            e.Property(x => x.DeletionRejectionReason).HasMaxLength(500);
            e.HasIndex(x => x.DeletionStatus);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
