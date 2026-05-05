using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Billings.Requests;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.SalesTargets.Repositories;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Billings.Services;

public class BillingService(
    IBillingRepository billingRepository,
    IStockRepository stockRepository,
    IUserGeoAssignmentRepository geoAssignmentRepository,
    IUserReportingLineRepository reportingLineRepository,
    ISalesTargetRepository salesTargetRepository,
    IProductRepository productRepository,
    IDistributedLockService lockService,
    ICacheService cache,
    AppDbContext db) : IBillingService
{
    private readonly IBillingRepository _billingRepository = billingRepository;
    private readonly IStockRepository _stockRepository = stockRepository;
    private readonly IUserGeoAssignmentRepository _geoAssignmentRepository = geoAssignmentRepository;
    private readonly IUserReportingLineRepository _reportingLineRepository = reportingLineRepository;
    private readonly ISalesTargetRepository _salesTargetRepository = salesTargetRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly ICacheService _cache = cache;
    private readonly AppDbContext _db = db;

    private static readonly TimeSpan SalesCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<BillingDto> CreateAsync(CreateBillingRequest request, int salesRepId, CancellationToken ct = default)
    {
        // ① Validate outlet
        var outlet = await _billingRepository.GetOutletAsync(request.OutletId, ct)
            ?? throw new NotFoundException("Outlet", request.OutletId);

        // ② Validate all products exist and are active
        var requestedProductIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productNames        = await _billingRepository.GetActiveProductNamesAsync(requestedProductIds, ct);
        var missingIds          = requestedProductIds.Except(productNames.Keys).ToList();
        if (missingIds.Count > 0)
            throw new NotFoundException("Product", string.Join(", ", missingIds));

        // ③ Resolve geo from UserGeoAssignment
        var geo = await _geoAssignmentRepository.GetActiveByUserIdAsync(salesRepId, ct)
            ?? throw new BusinessRuleException(
                "GEO_ASSIGNMENT_NOT_FOUND",
                $"Sales rep {salesRepId} has no active geographic assignment.");

        // ④ Resolve distributor by territory
        var territoryId  = geo.TerritoryId
            ?? throw new BusinessRuleException(
                "GEO_TERRITORY_NOT_SET",
                $"Sales rep {salesRepId} has no territory assigned.");

        var distributor = await _billingRepository.GetDistributorByTerritoryAsync(territoryId, ct)
            ?? throw new BusinessRuleException(
                "DISTRIBUTOR_NOT_FOUND",
                $"No active distributor found for territory {territoryId}.");

        // ⑤ Walk org chain — 4 hops up UserReportingLine
        var l1 = await _reportingLineRepository.GetActiveByUserIdAsync(salesRepId, ct);
        int? supervisorId = l1?.ReportsToUserId;

        var l2 = supervisorId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(supervisorId.Value, ct) : null;
        int? asmId = l2?.ReportsToUserId;

        var l3 = asmId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(asmId.Value, ct) : null;
        int? rsmId = l3?.ReportsToUserId;

        var l4 = rsmId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(rsmId.Value, ct) : null;
        int? nsmId = l4?.ReportsToUserId;

        // ⑥ Pre-check stock availability for Sale items only (fast fail before acquiring lock).
        // Collect ALL shortages across sale line items so the rep sees every missing product in one
        // response — avoids retry-discover-retry-discover loops from mobile.
        var saleItems      = request.Items.Where(i => i.BillingItemType == BillingItemType.Sale).ToList();
        var saleProductIds = saleItems.Select(i => i.ProductId).Distinct().ToList();
        if (saleProductIds.Count > 0)
        {
            var snapshot = await _billingRepository.GetStockSnapshotAsync(distributor.Id, saleProductIds, ct);
            var shortages = new List<StockShortage>();
            foreach (var item in saleItems)
            {
                var stock     = snapshot.FirstOrDefault(s => s.ProductId == item.ProductId);
                var available = stock?.QuantityOnHand ?? 0m;
                if (available < item.Quantity)
                {
                    var name = productNames.TryGetValue(item.ProductId, out var n) ? n : $"Product #{item.ProductId}";
                    shortages.Add(new StockShortage(item.ProductId, name, item.Quantity, available));
                }
            }
            if (shortages.Count > 0)
                throw new InsufficientStockException(shortages);
        }

        // ⑦ Compute amounts
        var billDiscountRate = request.BillDiscountRate;
        decimal subTotal = 0m;
        var lineItems = request.Items.Select((item, idx) =>
        {
            var discountAmount = item.IsFreeIssue
                ? 0m
                : Math.Round(item.Quantity * item.UnitPrice * item.DiscountRate / 100m, 2);
            var totalPrice = item.IsFreeIssue
                ? 0m
                : Math.Round(item.Quantity * item.UnitPrice - discountAmount, 2);

            if (!item.IsFreeIssue) subTotal += totalPrice;

            return new BillingItem
            {
                ProductId      = item.ProductId,
                Quantity       = item.Quantity,
                UnitPrice      = item.IsFreeIssue ? 0m : item.UnitPrice,
                DiscountRate   = item.IsFreeIssue ? 0m : item.DiscountRate,
                DiscountAmount = discountAmount,
                TotalPrice     = totalPrice,
                IsFreeIssue    = item.IsFreeIssue,
                BillingItemType = item.BillingItemType,
                ReturnType     = item.ReturnType,
                ExpireDate     = item.ExpireDate,
                LineNumber     = idx + 1,
                CreatedAt      = DateTime.UtcNow
            };
        }).ToList();

        var billDiscountAmount = Math.Round(subTotal * billDiscountRate / 100m, 2);
        var totalAmount        = subTotal - billDiscountAmount;

        // ⑧ Acquire advisory lock scoped to sales rep (BillingId not yet known)
        await using var advisoryLock = await _lockService.AcquireAsync($"billing:create:{salesRepId}", ct)
            ?? throw new ConcurrencyConflictException(
                new { salesRepId, message = "Another billing creation is already in progress for this sales rep." });

        // ⑨ Generate billing number
        var seqNo         = await _billingRepository.GetNextBillingNumberAsync(ct);
        var billingNumber = $"BIL-{DateTime.UtcNow.Year}-{seqNo:D5}";

        // ⑩ Build entity
        var billing = new Billing
        {
            BillingNumber = billingNumber,
            BillingDate   = request.BillingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            OutletId          = request.OutletId,
            SalesRepId        = salesRepId,
            DistributorId     = distributor.Id,
            SupervisorUserId  = supervisorId,
            AsmUserId         = asmId,
            RsmUserId         = rsmId,
            NsmUserId         = nsmId,
            RouteId           = outlet.RouteId,
            DivisionId        = outlet.DivisionId,
            TerritoryId       = geo.TerritoryId,
            AreaId            = geo.AreaId,
            RegionId          = geo.RegionId,
            SubTotalAmount    = subTotal,
            BillDiscountRate  = billDiscountRate,
            BillDiscountAmount = billDiscountAmount,
            TotalAmount       = totalAmount,
            Status            = BillingStatus.Submitted,
            Notes             = request.Notes,
            Latitude          = request.Latitude,
            Longitude         = request.Longitude,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow,
            CreatedBy         = salesRepId,
            Items             = lineItems
        };

        // ⑪ ExecutionStrategy + transaction + stock movement (atomic)
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _billingRepository.BeginTransactionAsync(ct);
            try
            {
                await _billingRepository.AddAsync(billing, ct);
                await _billingRepository.SaveChangesAsync(ct);  // billingId assigned here

                foreach (var item in billing.Items)
                {
                    if (item.BillingItemType == BillingItemType.Sale)
                    {
                        await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, ct);
                        await _stockRepository.DeductStockAsync(
                            distributor.Id, item.ProductId, item.Quantity,
                            item.IsFreeIssue ? StockTransactionType.FreeIssue : StockTransactionType.Sale,
                            "Billing", billing.Id, salesRepId, ct: ct);
                    }
                    else if (item.ReturnType == Enums.ReturnType.MarketResell)
                    {
                        await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, ct);
                        await _stockRepository.CreditStockAsync(
                            distributor.Id, item.ProductId, item.Quantity,
                            StockTransactionType.Return,
                            "Billing", billing.Id, salesRepId, ct: ct);
                    }
                    // Damage / Expire: billing record only — no stock movement
                }

                await _billingRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });

        // ⑫ Re-fetch read-only for DTO projection
        var created = await _billingRepository.GetByIdAsync(billing.Id, ct)
            ?? throw new DatabaseUnavailableException();

        return ProjectToDto(created);
    }

    public async Task<BillingDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetByIdAsync(id, ct);
        return billing is null ? null : ProjectToDto(billing);
    }

    public Task<(List<BillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        BillingStatus? status,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default)
        => _billingRepository.GetListAsync(
            page, pageSize, status,
            outletId, distributorId, salesRepId,
            dateFrom, dateTo, ct);

    // ── Projection ────────────────────────────────────────────────────────

    private static BillingDto ProjectToDto(Billing b) => new(
        b.Id,
        b.BillingNumber,
        b.BillingDate,
        b.OutletId,
        b.Outlet?.Name ?? string.Empty,
        b.SalesRepId,
        b.SalesRep?.Name ?? string.Empty,
        b.DistributorId,
        b.Distributor?.Name ?? string.Empty,
        b.SupervisorUserId,
        b.Supervisor?.Name,
        b.AsmUserId,
        b.Asm?.Name,
        b.RsmUserId,
        b.Rsm?.Name,
        b.NsmUserId,
        b.Nsm?.Name,
        b.RouteId,
        b.DivisionId,
        b.TerritoryId,
        b.AreaId,
        b.RegionId,
        b.SubTotalAmount,
        b.BillDiscountRate,
        b.BillDiscountAmount,
        b.TotalAmount,
        b.Status,
        b.Notes,
        b.Latitude,
        b.Longitude,
        b.CreatedAt,
        b.Items.OrderBy(i => i.LineNumber).Select(i => new BillingItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Code ?? string.Empty,
            i.Product?.ItemDescription ?? string.Empty,
            i.Quantity,
            i.UnitPrice,
            i.DiscountRate,
            i.DiscountAmount,
            i.TotalPrice,
            i.IsFreeIssue,
            i.BillingItemType,
            i.ReturnType,
            i.ExpireDate,
            i.LineNumber)).ToList()
    );

    public async Task<OutletBillingSummaryResponseDto> GetOutletSummaryAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default)
    {
        var rows = await _billingRepository.GetOutletSummaryRawAsync(
            salesRepId, routeId, dateFrom, dateTo, ct);

        var outletSummaries = rows
            .GroupBy(r => new { r.OutletId, r.OutletName })
            .Select(g => new OutletBillingSummaryDto(
                g.Key.OutletId,
                g.Key.OutletName,
                g.Count(),
                g.Sum(r => r.TotalAmount),
                g.OrderByDescending(r => r.BillingDate)
                 .Select(r => new BillLineDto(r.Id, r.BillingNumber, r.BillingDate, r.TotalAmount, r.Status.ToString()))
                 .ToList()))
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return new OutletBillingSummaryResponseDto(
            GrandTotal: outletSummaries.Sum(x => x.TotalAmount),
            TotalBillingCount: outletSummaries.Sum(x => x.BillingCount),
            OutletSummaries: outletSummaries);
    }

    public async Task<RepMonthlySalesDto> GetRepMonthlySalesAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
    {
        var cacheKey = $"rep-sales:{salesRepId}:{year}:{month}";
        var cached = await _cache.GetAsync<RepMonthlySalesDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var total = await _billingRepository.GetRepMonthlySalesTotalAsync(salesRepId, year, month, ct);
        var result = new RepMonthlySalesDto(year, month, total);
        await _cache.SetAsync(cacheKey, result, SalesCacheTtl, ct);
        return result;
    }

    public async Task<RepMonthlySalesItemwiseDto> GetRepMonthlySalesItemwiseAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
    {
        var cacheKey = $"rep-sales-itemwise:{salesRepId}:{year}:{month}";
        var cached = await _cache.GetAsync<RepMonthlySalesItemwiseDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // Two narrow grouped queries — sequential because they share the scoped DbContext.
        // Each is index-friendly and runs in single-digit ms.
        var sales   = await _billingRepository.GetRepMonthlySalesByProductAsync(salesRepId, year, month, ct);
        var targets = await _salesTargetRepository.GetByRepAndMonthAsync(salesRepId, year, month, ct);

        var salesByProduct   = sales.ToDictionary(r => r.ProductId);
        var targetsByProduct = targets
            .GroupBy(t => t.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TargetQuantity));

        var productIds = salesByProduct.Keys.Union(targetsByProduct.Keys).ToList();
        var nameMap    = await _productRepository.GetCodeAndNameByIdsAsync(productIds, ct);

        var items = productIds
            .Select(pid =>
            {
                var soldPacks  = salesByProduct.TryGetValue(pid, out var s)  ? s.Qty    : 0m;
                var soldAmount = salesByProduct.TryGetValue(pid, out var s2) ? s2.Amount : 0m;
                var targetQty  = targetsByProduct.TryGetValue(pid, out var t) ? t : 0m;

                var (code, name, packsPerCase) = nameMap.TryGetValue(pid, out var meta)
                    ? meta
                    : ($"#{pid}", $"Product {pid}", 1);

                // Billing quantity is stored in packs; targets are recorded in cases.
                // Send both — mobile renders "2 CS · 120 PKT" so reps see breakdown and total.
                // Cases are reported to 1 decimal — half-case breaks matter, quarter-cases don't.
                var divisor = packsPerCase > 0 ? packsPerCase : 1;
                var soldQtyCases = Math.Round(soldPacks / divisor, 1);

                var pct = targetQty > 0
                    ? Math.Round(soldQtyCases / targetQty * 100m, 1)
                    : 0m;

                return new RepMonthlySalesItemDto(
                    pid, code, name, targetQty, soldQtyCases, soldPacks, soldAmount, pct);
            })
            .OrderBy(i => i.AchievementPercent)   // laggards first
            .ThenBy(i => i.ItemName)
            .ToList();

        var result = new RepMonthlySalesItemwiseDto(
            year, month,
            TotalTargetQuantity:    items.Sum(i => i.TargetQuantity),
            TotalSoldQuantity:      items.Sum(i => i.SoldQuantity),
            TotalSoldQuantityPacks: items.Sum(i => i.SoldQuantityPacks),
            TotalSoldAmount:        items.Sum(i => i.SoldAmount),
            Items: items);

        await _cache.SetAsync(cacheKey, result, SalesCacheTtl, ct);
        return result;
    }
}
