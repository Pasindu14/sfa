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
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Notifications;
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
    IUserRepository userRepository,
    INotificationService notificationService,
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
    private readonly IUserRepository _userRepository = userRepository;
    private readonly INotificationService _notificationService = notificationService;
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

        // ⑥ Pre-check stock availability before acquiring lock (fast fail).
        // Collects ALL shortages in one pass so the rep sees every missing product at once.
        // FOC lines split by funding source:
        //   - Company-funded FOC   → drawn from StockType.FreeIssue pool
        //   - Distributor-funded FOC → drawn from StockType.Normal pool, so it competes with Sale demand for the same balance
        var saleItems       = request.Items.Where(i => i.BillingItemType == BillingItemType.Sale).ToList();
        var companyFiItems  = request.Items.Where(i => i.BillingItemType == BillingItemType.FreeIssue
                                                    && i.FreeIssueSource == FreeIssueSource.Company).ToList();
        var distributorFiItems = request.Items.Where(i => i.BillingItemType == BillingItemType.FreeIssue
                                                       && i.FreeIssueSource == FreeIssueSource.Distributor).ToList();
        var preCheckIds = saleItems.Select(i => i.ProductId)
                                   .Concat(companyFiItems.Select(i => i.ProductId))
                                   .Concat(distributorFiItems.Select(i => i.ProductId))
                                   .Distinct().ToList();

        if (preCheckIds.Count > 0)
        {
            var snapshot  = await _billingRepository.GetStockSnapshotAsync(distributor.Id, preCheckIds, ct);
            var shortages = new List<StockShortage>();

            // Combine Sale + Distributor-funded FOC demand per product against the Normal pool
            var normalDemand = saleItems.Concat(distributorFiItems)
                                        .GroupBy(i => i.ProductId)
                                        .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) });

            foreach (var demand in normalDemand)
            {
                var stock     = snapshot.FirstOrDefault(s => s.ProductId == demand.ProductId && s.StockType == StockType.Normal);
                var available = stock?.QuantityOnHand ?? 0m;
                if (available < demand.Quantity)
                {
                    var name = productNames.TryGetValue(demand.ProductId, out var n) ? n : $"Product #{demand.ProductId}";
                    shortages.Add(new StockShortage(demand.ProductId, name, demand.Quantity, available));
                }
            }

            foreach (var item in companyFiItems)
            {
                var stock     = snapshot.FirstOrDefault(s => s.ProductId == item.ProductId && s.StockType == StockType.FreeIssue);
                var available = stock?.QuantityOnHand ?? 0m;
                if (available < item.Quantity)
                {
                    var name = productNames.TryGetValue(item.ProductId, out var n) ? n : $"Product #{item.ProductId}";
                    shortages.Add(new StockShortage(item.ProductId, $"{name} (FOC stock)", item.Quantity, available));
                }
            }

            if (shortages.Count > 0)
                throw new InsufficientStockException(shortages);
        }

        // ⑦ Compute amounts
        // Sale       → discountAmount = qty × price × rate/100; totalPrice = qty×price − discountAmount; counts toward SubTotal
        // FreeIssue  → discountAmount = 0; totalPrice = qty × price (informational FOC value); excluded from SubTotal
        // Return     → totalPrice = qty × price; tracked separately, no SubTotal contribution
        var billDiscountRate = request.BillDiscountRate;
        decimal subTotal                  = 0m;
        decimal freeIssueValueCompany     = 0m;
        decimal freeIssueValueDistributor = 0m;
        decimal returnValue               = 0m;
        var lineItems = request.Items.Select((item, idx) =>
        {
            decimal discountAmount;
            decimal totalPrice;

            switch (item.BillingItemType)
            {
                case BillingItemType.FreeIssue:
                    discountAmount = 0m;
                    totalPrice     = Math.Round(item.Quantity * item.UnitPrice, 2);
                    if (item.FreeIssueSource == FreeIssueSource.Distributor)
                        freeIssueValueDistributor += totalPrice;
                    else
                        freeIssueValueCompany += totalPrice;
                    break;
                case BillingItemType.Sale:
                    discountAmount = Math.Round(item.Quantity * item.UnitPrice * item.DiscountRate / 100m, 2);
                    totalPrice     = Math.Round(item.Quantity * item.UnitPrice - discountAmount, 2);
                    subTotal      += totalPrice;
                    break;
                default: // Return
                    discountAmount = Math.Round(item.Quantity * item.UnitPrice * item.DiscountRate / 100m, 2);
                    totalPrice     = Math.Round(item.Quantity * item.UnitPrice - discountAmount, 2);
                    if (item.ReturnType == ReturnType.MarketResell)
                        returnValue += totalPrice;
                    break;
            }

            return new BillingItem
            {
                ProductId        = item.ProductId,
                Quantity         = item.Quantity,
                UnitPrice        = item.UnitPrice,
                DiscountRate     = item.BillingItemType == BillingItemType.FreeIssue ? 0m : item.DiscountRate,
                DiscountAmount   = discountAmount,
                TotalPrice       = totalPrice,
                BillingItemType  = item.BillingItemType,
                ReturnType       = item.ReturnType,
                FreeIssueSource  = item.BillingItemType == BillingItemType.FreeIssue ? item.FreeIssueSource : null,
                ExpireDate       = item.ExpireDate,
                LineNumber       = idx + 1,
                CreatedAt        = DateTime.UtcNow
            };
        }).ToList();
        var freeIssueValue = freeIssueValueCompany + freeIssueValueDistributor;

        var billDiscountAmount = Math.Round(subTotal * billDiscountRate / 100m, 2);
        var totalAmount        = subTotal - billDiscountAmount - returnValue;

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
            ReturnValue               = returnValue,
            FreeIssueValue            = freeIssueValue,
            FreeIssueValueCompany     = freeIssueValueCompany,
            FreeIssueValueDistributor = freeIssueValueDistributor,
            RepStatus         = RepBillingStatus.Submitted,
            DistributorStatus = DistributorBillingStatus.Pending,
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
                    switch (item.BillingItemType)
                    {
                        case BillingItemType.Sale:
                            await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, StockType.Normal, ct);
                            await _stockRepository.DeductStockAsync(
                                distributor.Id, item.ProductId, item.Quantity,
                                StockType.Normal,
                                StockTransactionType.Sale,
                                "Billing", billing.Id, salesRepId, ct: ct);
                            break;

                        case BillingItemType.FreeIssue when item.FreeIssueSource == FreeIssueSource.Distributor:
                            // Distributor-funded FOC: distributor gives away their own saleable stock as a promotion.
                            // Deduct from Normal pool — same physical inventory the Sale lines compete for.
                            await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, StockType.Normal, ct);
                            await _stockRepository.DeductStockAsync(
                                distributor.Id, item.ProductId, item.Quantity,
                                StockType.Normal,
                                StockTransactionType.FreeIssue,
                                "Billing", billing.Id, salesRepId,
                                notes: "Distributor-funded FOC", ct: ct);
                            break;

                        case BillingItemType.FreeIssue:
                            // Company-funded FOC (default): drawn from the FOC pool the manufacturer ships to the distributor.
                            await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, StockType.FreeIssue, ct);
                            await _stockRepository.DeductStockAsync(
                                distributor.Id, item.ProductId, item.Quantity,
                                StockType.FreeIssue,
                                StockTransactionType.FreeIssue,
                                "Billing", billing.Id, salesRepId, ct: ct);
                            break;

                        case BillingItemType.Return when item.ReturnType == Enums.ReturnType.MarketResell:
                            await _stockRepository.GetStockForUpdateAsync(distributor.Id, item.ProductId, StockType.Normal, ct);
                            await _stockRepository.CreditStockAsync(
                                distributor.Id, item.ProductId, item.Quantity,
                                StockType.Normal,
                                StockTransactionType.Return,
                                "Billing", billing.Id, salesRepId, ct: ct);
                            break;

                        // Return + Damage / Expire: billing record only — no stock movement
                    }
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

        // Stamp outlet's last bill date and bust the route cache
        var lastBillDate = billing.BillingDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        await _db.Outlets
            .Where(o => o.Id == billing.OutletId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.LastBillDate, lastBillDate), ct);
        await _cache.RemoveByPrefixAsync("outlets:route:", ct);

        // ⑫ Re-fetch read-only for DTO projection
        var created = await _billingRepository.GetByIdAsync(billing.Id, ct)
            ?? throw new DatabaseUnavailableException();

        // Notify distributor users — fire-and-forget, failure never fails the billing
        await _notificationService.SendToDistributorUsersAsync(
            billing.DistributorId,
            "New Bill Pending Approval",
            $"Bill {created.BillingNumber} from {created.SalesRep?.Name ?? "Sales Rep"} needs your approval.",
            new Dictionary<string, string>
            {
                ["type"] = "BILL_PENDING",
                ["billingId"] = billing.Id.ToString(),
                ["billingNumber"] = billing.BillingNumber
            }, ct);

        return ProjectToDto(created);
    }

    public async Task<BillingDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetByIdAsync(id, ct);
        return billing is null ? null : ProjectToDto(billing);
    }

    public Task<(List<BillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        RepBillingStatus? repStatus,
        DistributorBillingStatus? distributorStatus,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        PaymentType? paymentType = null,
        bool? isCashCollected = null,
        string? billNo = null,
        CancellationToken ct = default)
        => _billingRepository.GetListAsync(
            page, pageSize, repStatus, distributorStatus,
            outletId, distributorId, salesRepId,
            dateFrom, dateTo, paymentType, isCashCollected, billNo, ct);

    // ── Stock reversal ────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the stock movements made at bill creation — must be called inside an open transaction.
    /// Sale + Distributor FOC: credits Normal stock back.
    /// Company FOC: credits FreeIssue stock back.
    /// MarketResell return: deducts Normal stock (reverses the credit that was issued at creation).
    /// Damage/Expire returns had no stock movement, so nothing to reverse.
    /// </summary>
    private async Task ReverseStockForBillingAsync(Billing billing, int actorId, string notes, CancellationToken ct)
    {
        foreach (var item in billing.Items)
        {
            switch (item.BillingItemType)
            {
                case BillingItemType.Sale:
                    await _stockRepository.GetStockForUpdateAsync(billing.DistributorId, item.ProductId, StockType.Normal, ct);
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.FreeIssue when item.FreeIssueSource == FreeIssueSource.Distributor:
                    await _stockRepository.GetStockForUpdateAsync(billing.DistributorId, item.ProductId, StockType.Normal, ct);
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.FreeIssue:
                    await _stockRepository.GetStockForUpdateAsync(billing.DistributorId, item.ProductId, StockType.FreeIssue, ct);
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.FreeIssue, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.Return when item.ReturnType == Enums.ReturnType.MarketResell:
                    await _stockRepository.GetStockForUpdateAsync(billing.DistributorId, item.ProductId, StockType.Normal, ct);
                    await _stockRepository.DeductStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                // BillingItemType.Return (Damage / Expire): no stock movement at creation → nothing to reverse
            }
        }
    }

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
        b.ReturnValue,
        b.TotalAmount,
        b.FreeIssueValue,
        b.FreeIssueValueCompany,
        b.FreeIssueValueDistributor,
        b.RepStatus,
        b.DistributorStatus,
        b.RejectionReason,
        b.PaymentType,
        b.IsCashCollected,
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
            i.BillingItemType,
            i.ReturnType,
            i.FreeIssueSource,
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
                 .Select(r => new BillLineDto(r.Id, r.BillingNumber, r.BillingDate, r.TotalAmount, r.RepStatus.ToString()))
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

        var approved = await _billingRepository.GetRepMonthlySalesTotalAsync(salesRepId, year, month, ct);
        var pending  = await _billingRepository.GetRepMonthlySalesPendingTotalAsync(salesRepId, year, month, ct);
        var result   = new RepMonthlySalesDto(year, month, approved, pending);
        await _cache.SetAsync(cacheKey, result, SalesCacheTtl, ct);
        return result;
    }

    public async Task<RepDailySalesDto> GetRepDailySalesAsync(
        int salesRepId, DateOnly date, CancellationToken ct = default)
    {
        var cacheKey = $"rep-sales-daily:{salesRepId}:{date:yyyy-MM-dd}";
        var cached = await _cache.GetAsync<RepDailySalesDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var approved = await _billingRepository.GetRepDailySalesTotalAsync(salesRepId, date, DistributorBillingStatus.Approved, ct);
        var pending  = await _billingRepository.GetRepDailySalesTotalAsync(salesRepId, date, DistributorBillingStatus.Pending, ct);
        var result   = new RepDailySalesDto(date, approved, pending);
        await _cache.SetAsync(cacheKey, result, SalesCacheTtl, ct);
        return result;
    }

    public async Task<BillingDto> CancelAsync(int billingId, int salesRepId, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetTrackedByIdWithItemsAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        if (billing.SalesRepId != salesRepId)
            throw new AuthorizationException("Billing");

        if (billing.RepStatus != RepBillingStatus.Submitted)
            throw new BusinessRuleException(
                "BILLING_NOT_CANCELLABLE",
                $"Billing {billing.BillingNumber} cannot be cancelled — current status is {billing.RepStatus}.");

        billing.RepStatus = RepBillingStatus.Cancelled;
        billing.UpdatedAt = DateTime.UtcNow;
        billing.UpdatedBy = salesRepId;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _billingRepository.BeginTransactionAsync(ct);
            try
            {
                await _billingRepository.SaveChangesAsync(ct);
                await ReverseStockForBillingAsync(billing, salesRepId, "Stock reversed — bill cancelled by rep", ct);
                await _billingRepository.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        var updated = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();
        return ProjectToDto(updated);
    }

    public async Task<BillingDto> ApproveAsync(int billingId, int userId, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null || user.DistributorId != billing.DistributorId)
            throw new AuthorizationException("Billing");

        if (billing.RepStatus != RepBillingStatus.Submitted)
            throw new BusinessRuleException(
                "BILLING_NOT_ACTIONABLE",
                $"Billing {billing.BillingNumber} cannot be approved — rep status is {billing.RepStatus}.");

        if (billing.DistributorStatus != DistributorBillingStatus.Pending)
            throw new BusinessRuleException(
                "BILLING_ALREADY_ACTIONED",
                $"Billing {billing.BillingNumber} has already been {billing.DistributorStatus}.");

        billing.DistributorStatus = DistributorBillingStatus.Approved;
        billing.ApprovedAt        = DateTime.UtcNow;
        billing.UpdatedAt         = DateTime.UtcNow;
        billing.UpdatedBy         = userId;

        await _billingRepository.SaveChangesAsync(ct);

        var result = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();

        await _notificationService.SendToUserAsync(
            billing.SalesRepId,
            "Bill Approved",
            $"Bill {result.BillingNumber} has been approved by the distributor.",
            new Dictionary<string, string>
            {
                ["type"] = "BILL_APPROVED",
                ["billingId"] = billing.Id.ToString(),
                ["billingNumber"] = result.BillingNumber
            }, ct);

        return ProjectToDto(result);
    }

    public async Task<BillingDto> RejectAsync(int billingId, int userId, string? reason, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetTrackedByIdWithItemsAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null || user.DistributorId != billing.DistributorId)
            throw new AuthorizationException("Billing");

        if (billing.RepStatus != RepBillingStatus.Submitted)
            throw new BusinessRuleException(
                "BILLING_NOT_ACTIONABLE",
                $"Billing {billing.BillingNumber} cannot be rejected — rep status is {billing.RepStatus}.");

        if (billing.DistributorStatus != DistributorBillingStatus.Pending)
            throw new BusinessRuleException(
                "BILLING_ALREADY_ACTIONED",
                $"Billing {billing.BillingNumber} has already been {billing.DistributorStatus}.");

        billing.DistributorStatus = DistributorBillingStatus.Rejected;
        billing.RejectionReason   = reason;
        billing.RejectedAt        = DateTime.UtcNow;
        billing.UpdatedAt         = DateTime.UtcNow;
        billing.UpdatedBy         = userId;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _billingRepository.BeginTransactionAsync(ct);
            try
            {
                await _billingRepository.SaveChangesAsync(ct);
                await ReverseStockForBillingAsync(billing, userId, "Stock reversed — bill rejected by distributor", ct);
                await _billingRepository.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        var result = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();

        var rejectionMessage = string.IsNullOrWhiteSpace(reason)
            ? $"Bill {result.BillingNumber} has been rejected by the distributor."
            : $"Bill {result.BillingNumber} rejected: {reason}";

        var notificationData = new Dictionary<string, string>
        {
            ["type"] = "BILL_REJECTED",
            ["billingId"] = billing.Id.ToString(),
            ["billingNumber"] = result.BillingNumber
        };

        // Notify the sales rep who created the bill
        await _notificationService.SendToUserAsync(
            billing.SalesRepId,
            "Bill Rejected",
            rejectionMessage,
            notificationData,
            ct);

        // Notify all ancestor levels (Supervisor → ASM → RSM), excluding NSM
        foreach (var ancestorId in new[] { billing.SupervisorUserId, billing.AsmUserId, billing.RsmUserId }
            .Where(id => id.HasValue).Select(id => id!.Value))
        {
            await _notificationService.SendToUserAsync(
                ancestorId,
                "Bill Rejected",
                rejectionMessage,
                notificationData,
                ct);
        }

        return ProjectToDto(result);
    }

    public async Task<BillingDto> UpdatePaymentTypeAsync(int billingId, int userId, PaymentType paymentType, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null || user.DistributorId != billing.DistributorId)
            throw new AuthorizationException("Billing");

        billing.PaymentType = paymentType;
        billing.UpdatedAt   = DateTime.UtcNow;
        billing.UpdatedBy   = userId;

        await _billingRepository.SaveChangesAsync(ct);

        var updated = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();
        return ProjectToDto(updated);
    }

    public async Task<BillingDto> UpdateCashCollectedAsync(int billingId, int userId, bool isCashCollected, CancellationToken ct = default)
    {
        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null || user.DistributorId != billing.DistributorId)
            throw new AuthorizationException("Billing");

        billing.IsCashCollected = isCashCollected;
        billing.UpdatedAt       = DateTime.UtcNow;
        billing.UpdatedBy       = userId;

        await _billingRepository.SaveChangesAsync(ct);

        var updated = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();
        return ProjectToDto(updated);
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
