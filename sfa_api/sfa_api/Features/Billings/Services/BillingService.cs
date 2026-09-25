using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Billings.Requests;
using sfa_api.Features.PricingStructures.Repositories;
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
using sfa_api.Common.Geo;
using sfa_api.Features.UserProximityExemptions.Services;

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
    AppDbContext db,
    IProximityPolicyResolver policyResolver,
    IPricingStructureRepository pricingStructureRepository,
    ILogger<BillingService> logger) : IBillingService
{
    private readonly IPricingStructureRepository _pricingStructureRepository = pricingStructureRepository;
    private readonly ILogger<BillingService> _logger = logger;
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
    private readonly IProximityPolicyResolver _policyResolver = policyResolver;

    private static readonly TimeSpan SalesCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<BillingDto> CreateAsync(CreateBillingRequest request, int salesRepId, string? clientBillId = null, CancellationToken ct = default)
    {
        // ⓪ Idempotent fast-path — if this client bill id was already persisted (a retry of a
        // submission that previously succeeded), return the existing bill without redoing any
        // work or burning a billing-number sequence value. The unique index on ClientBillId is
        // the concurrency backstop for the narrower race where two replays arrive at once.
        if (!string.IsNullOrWhiteSpace(clientBillId))
        {
            var existingId = await _billingRepository.FindIdByClientBillIdAsync(clientBillId, ct);
            if (existingId is not null)
            {
                var existing = await _billingRepository.GetByIdAsync(existingId.Value, ct)
                    ?? throw new DatabaseUnavailableException();
                return ProjectToDto(existing);
            }
        }

        // ① Validate outlet
        var outlet = await _billingRepository.GetOutletAsync(request.OutletId, ct)
            ?? throw new NotFoundException("Outlet", request.OutletId);

        // ② Proximity gate — fails fast before any further DB work.
        //
        // The rep position is mandatory and is re-checked here, not only in the
        // validator, so that a direct service call cannot bypass the geofence by
        // simply omitting the coordinates. (0,0) is rejected for the same reason:
        // GeoMath treats it as "no coordinate", which would skip the check.
        //
        // This is the real gate. The mobile app's own distance filter is UX — a
        // rooted handset or a spoofed GPS makes a client-side check worthless —
        // so the policy is resolved here per rep, from the same resolver the
        // outlet sync reads, and never from the config options directly.
        if (request.Latitude is not { } repLat
            || request.Longitude is not { } repLng
            || (repLat == 0 && repLng == 0))
            throw new BillingLocationRequiredException();

        var policy = await _policyResolver.ResolveAsync(salesRepId, ct: ct);

        // A MaxValue distance means the OUTLET has no stored coordinates (a 0,0
        // placeholder). Those outlets stay billable from anywhere until someone
        // captures their real position — the alternative is computing them as
        // ~10,000 km away and making them permanently unbillable.
        double? distanceFromOutletMeters = null;
        var proximityOverridden = false;
        var dist = GeoMath.HaversineMeters(repLat, repLng, outlet.Latitude, outlet.Longitude);
        if (dist < double.MaxValue)
        {
            distanceFromOutletMeters = dist;
            var outOfRange = dist > policy.LimitMeters;

            if (outOfRange && policy.Enforced)
                throw new OutletProximityException(dist, policy.RadiusMeters);

            // Stamp the bill only when the exemption actually did something. A bill
            // taken inside the radius by an exempt rep is an ordinary bill, and
            // flagging it would drown the exception report in noise.
            proximityOverridden = outOfRange;
        }

        // ③ Validate all products exist and are active
        var requestedProductIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productNames        = await _billingRepository.GetActiveProductNamesAsync(requestedProductIds, ct);
        var missingIds          = requestedProductIds.Except(productNames.Keys).ToList();
        if (missingIds.Count > 0)
            throw new NotFoundException("Product", string.Join(", ", missingIds));

        // ③½ Resolve pricing structures — line → bill → default. Older app builds send no structure
        // at all; they priced from the legacy product fields, which are served from the default
        // structure, so stamping the default is accurate. The price itself is never re-derived: an
        // offline bill keeps the price the customer was shown even if the structure changed since.
        var headerStructureId = request.PricingStructureId
            ?? await _pricingStructureRepository.GetDefaultIdAsync(ct);
        int? LineStructureId(CreateBillingItemRequest item) => item.PricingStructureId ?? headerStructureId;

        var explicitStructureIds = request.Items
            .Select(i => i.PricingStructureId)
            .Append(request.PricingStructureId)
            .OfType<int>()
            .Distinct()
            .ToList();
        if (explicitStructureIds.Count > 0)
        {
            // Existence only — inactive or deleted is fine: a bill raised offline can arrive after
            // its structure was retired, and it is still a true record of what was charged.
            var knownStructures = await _pricingStructureRepository.GetExistingIdsAsync(explicitStructureIds, ct);
            var unknownStructures = explicitStructureIds.Where(id => !knownStructures.Contains(id)).ToList();
            if (unknownStructures.Count > 0)
                throw new NotFoundException("PricingStructure", string.Join(", ", unknownStructures));
        }

        await LogPriceMismatchesAsync(request, LineStructureId, ct);

        // ④ Resolve geo from UserGeoAssignment
        var geo = await _geoAssignmentRepository.GetActiveByUserIdAsync(salesRepId, ct)
            ?? throw new BusinessRuleException(
                "GEO_ASSIGNMENT_NOT_FOUND",
                $"Sales rep {salesRepId} has no active geographic assignment.");

        // ⑤ Resolve distributor by territory
        var territoryId  = geo.TerritoryId
            ?? throw new BusinessRuleException(
                "GEO_TERRITORY_NOT_SET",
                $"Sales rep {salesRepId} has no territory assigned.");

        var distributor = await _billingRepository.GetDistributorByTerritoryAsync(territoryId, ct)
            ?? throw new BusinessRuleException(
                "DISTRIBUTOR_NOT_FOUND",
                $"No active distributor found for territory {territoryId}.");

        // ⑥ Walk org chain — 4 hops up UserReportingLine
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

        // ⑦ Pre-check stock availability before acquiring lock (fast fail).
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

        // ⑧ Compute amounts — the per-line and bill-level math both live in ApplyLineMath /
        // RecomputeTotals so that this path and AdjustItemsAsync can never drift apart.
        var billDiscountRate = request.BillDiscountRate;
        var lineItems = request.Items.Select((item, idx) =>
        {
            var line = new BillingItem
            {
                ProductId        = item.ProductId,
                Quantity         = item.Quantity,
                UnitPrice        = item.UnitPrice,
                DiscountRate     = item.DiscountRate,
                BillingItemType  = item.BillingItemType,
                ReturnType       = item.ReturnType,
                FreeIssueSource  = item.BillingItemType == BillingItemType.FreeIssue ? item.FreeIssueSource : null,
                ExpireDate       = item.ExpireDate,
                LineNumber       = idx + 1,
                Source           = BillingItemSource.SalesRep,
                PricingStructureId = LineStructureId(item),
                PriceBasis         = item.PriceBasis,
                ListUnitPrice      = item.ListUnitPrice,
                CreatedAt        = DateTime.UtcNow
            };
            ApplyLineMath(line);
            return line;
        }).ToList();

        var totals             = RecomputeTotals(lineItems, billDiscountRate);
        var subTotal           = totals.SubTotal;
        var returnValue        = totals.ReturnValue;
        var billDiscountAmount = totals.BillDiscountAmount;
        var totalAmount        = totals.TotalAmount;

        // ⑧½ Guard against a negative grand total (finding #4). The bill-level discount plus market
        // returns must not exceed the sale sub-total — otherwise TotalAmount goes negative and is
        // persisted as negative revenue that then flows into sales aggregates. Checked here with the
        // actual rounded figures (the request validator's return-vs-sale math ignores the bill
        // discount) and BEFORE the billing-number sequence is burned.
        if (totalAmount < 0m)
            throw new BusinessRuleException(
                "BILL_TOTAL_NEGATIVE",
                $"Bill total would be negative ({totalAmount:F2}): sub-total {subTotal:F2} minus bill " +
                $"discount {billDiscountAmount:F2} and returns {returnValue:F2}. Reduce the bill discount " +
                $"or return quantities.",
                new { subTotal, billDiscountAmount, returnValue, totalAmount });

        // ⑨ Acquire advisory lock scoped to sales rep (BillingId not yet known). It guards the
        // number + insert + stock movement only, and is released the moment that transaction has
        // committed (or failed) — the post-commit stamp, cache invalidation, re-fetch and
        // notification below don't need it, and holding it through them only widens the window in
        // which a rep's next submission is bounced with a 409. Idempotency doesn't rely on it: the
        // ClientBillId fast-path and unique index cover replays.
        var advisoryLock = await _lockService.AcquireAsync($"billing:create:{salesRepId}", ct)
            ?? throw new ConcurrencyConflictException(
                new { salesRepId, message = "Another billing creation is already in progress for this sales rep." });

        Billing billing;
        try
        {
            // ⑩ Generate billing number
            var seqNo         = await _billingRepository.GetNextBillingNumberAsync(ct);
            var billingNumber = $"BIL-{SriLankaTime.Year}-{seqNo:D5}";

            // ⑪ Build entity
            billing = new Billing
            {
                BillingNumber = billingNumber,
                ClientBillId  = clientBillId,
                BillingDate   = request.BillingDate ?? SriLankaTime.Today,
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
                BillDiscountRate  = billDiscountRate,
                RepStatus         = RepBillingStatus.Submitted,
                DistributorStatus = DistributorBillingStatus.Pending,
                Notes                    = request.Notes,
                Latitude                 = request.Latitude,
                Longitude                = request.Longitude,
                DistanceFromOutletMeters = distanceFromOutletMeters,
                GpsAccuracyMeters        = request.GpsAccuracyMeters,
                ProximityOverridden      = proximityOverridden,
                ProximityExemptionId     = proximityOverridden ? policy.ExemptionId : null,
                PricingStructureId       = headerStructureId,
                CreatedAt                = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow,
                CreatedBy         = salesRepId,
                Items             = lineItems
            };
            ApplyTotals(billing, totals);

            // ⑫ ExecutionStrategy + transaction + stock movement (atomic).
            // Wrapped so a ClientBillId unique-index violation from a concurrent duplicate (two
            // replays of the same client bill id arriving at once) becomes an idempotent success:
            // we return the bill the winning request created instead of surfacing a 500.
            var strategy = _db.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _billingRepository.BeginTransactionAsync(ct);
                    try
                    {
                        await _billingRepository.AddAsync(billing, ct);
                        await _billingRepository.SaveChangesAsync(ct);  // billingId assigned here

                        // One SELECT … ORDER BY "Id" FOR UPDATE for every stock row the bill moves (Id order,
                        // so concurrent bills on overlapping products can't deadlock). Deduct/Credit below
                        // then work on these tracked rows without reloading them.
                        await _stockRepository.LockStocksForUpdateAsync(StockKeysFor(distributor.Id, billing.Items), ct);

                        foreach (var item in billing.Items)
                        {
                            switch (item.BillingItemType)
                            {
                                case BillingItemType.Sale:
                                    await _stockRepository.DeductStockAsync(
                                        distributor.Id, item.ProductId, item.Quantity,
                                        StockType.Normal,
                                        StockTransactionType.Sale,
                                        "Billing", billing.Id, salesRepId, ct: ct);
                                    break;

                                case BillingItemType.FreeIssue when item.FreeIssueSource == FreeIssueSource.Distributor:
                                    // Distributor-funded FOC: distributor gives away their own saleable stock as a promotion.
                                    // Deduct from Normal pool — same physical inventory the Sale lines compete for.
                                    await _stockRepository.DeductStockAsync(
                                        distributor.Id, item.ProductId, item.Quantity,
                                        StockType.Normal,
                                        StockTransactionType.FreeIssue,
                                        "Billing", billing.Id, salesRepId,
                                        notes: "Distributor-funded FOC", ct: ct);
                                    break;

                                case BillingItemType.FreeIssue:
                                    // Company-funded FOC (default): drawn from the FOC pool the manufacturer ships to the distributor.
                                    await _stockRepository.DeductStockAsync(
                                        distributor.Id, item.ProductId, item.Quantity,
                                        StockType.FreeIssue,
                                        StockTransactionType.FreeIssue,
                                        "Billing", billing.Id, salesRepId, ct: ct);
                                    break;

                                case BillingItemType.Return when item.ReturnType == Enums.ReturnType.MarketResell:
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
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(clientBillId))
            {
                // A concurrent request with the same client bill id won the insert race; the unique
                // index rejected ours. Return the winner's bill so this retry is an idempotent success.
                var winnerId = await _billingRepository.FindIdByClientBillIdAsync(clientBillId, ct);
                if (winnerId is null) throw;
                var winner = await _billingRepository.GetByIdAsync(winnerId.Value, ct)
                    ?? throw new DatabaseUnavailableException();
                return ProjectToDto(winner);
            }
        }
        finally
        {
            await advisoryLock.DisposeAsync();
        }

        // Stamp outlet's last bill date. The per-route outlet cache (mobile outlet sync) carries
        // LastBillDate — the app shows a "NEW" badge for never-billed outlets and persists the value
        // to its local DB on sync, so a stale null would resurrect that badge after a re-sync. Evict
        // only THIS outlet's route entry, and only when the stamped value actually changed (the first
        // bill of the day for the outlet) — not every route in the company on every bill.
        var lastBillDate = billing.BillingDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (outlet.LastBillDate != lastBillDate)
        {
            await _db.Outlets
                .Where(o => o.Id == billing.OutletId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.LastBillDate, lastBillDate), ct);
            await _cache.RemoveAsync(Outlets.Services.OutletService.RouteOutletsCacheKey(outlet.RouteId), ct);
            await _cache.RemoveAsync(Outlets.OutletCacheKeys.ActiveAll, ct);   // GET /outlets/active carries LastBillDate too
        }

        // A new bill lands as Pending, so it does not move the approved-only sales summary — but it
        // does move the pending totals on the rep's own screens, which are cached the same way.
        await InvalidateSalesCachesAsync(billing, affectsApprovedTotals: false, ct);

        // ⑬ Re-fetch read-only for DTO projection
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

    // ── Mobile local-store rehydration ────────────────────────────────────

    /// <summary>Hard ceiling on a single sync pull, so the payload to a phone stays bounded.</summary>
    private const int RepBillSyncMaxRows = 500;

    public async Task<List<RepBillSyncDto>> GetRepBillsForSyncAsync(
        int salesRepId, DateOnly since, CancellationToken ct = default)
    {
        var bills = await _billingRepository.GetRepBillsForSyncAsync(
            salesRepId, since, RepBillSyncMaxRows, ct);

        return [.. bills.Select(b => new RepBillSyncDto(
            b.Id,
            b.BillingNumber,
            b.ClientBillId,
            b.BillingDate,
            b.OutletId,
            b.Outlet?.Name ?? string.Empty,
            b.SubTotalAmount,
            b.BillDiscountRate,
            b.BillDiscountAmount,
            b.TotalAmount,
            b.Notes,
            b.Latitude,
            b.Longitude,
            b.RepStatus,
            b.DistributorStatus,
            b.CreatedAt,
            [.. b.Items.OrderBy(i => i.LineNumber).Select(i => new RepBillSyncItemDto(
                i.ProductId,
                i.Quantity,
                i.UnitPrice,
                i.DiscountRate,
                i.BillingItemType,
                i.ReturnType,
                i.FreeIssueSource,
                i.ExpireDate,
                i.LineNumber,
                i.PricingStructureId,
                i.PriceBasis,
                i.ListUnitPrice))],
            b.PricingStructureId))];
    }

    // ── Money math (shared by CreateAsync and AdjustItemsAsync) ───────────

    /// <summary>
    /// Per-line money math. Reads Quantity / UnitPrice / DiscountRate off the line and writes back
    /// DiscountAmount and TotalPrice.
    /// Sale + Return → discountAmount = qty × price × rate/100; totalPrice = qty×price − discountAmount.
    /// FreeIssue     → no discount ever; totalPrice = qty × price (informational FOC value).
    /// </summary>
    private static void ApplyLineMath(BillingItem line)
    {
        if (line.BillingItemType == BillingItemType.FreeIssue)
        {
            line.DiscountRate   = 0m;
            line.DiscountAmount = 0m;
            line.TotalPrice     = Math.Round(line.Quantity * line.UnitPrice, 2);
            return;
        }

        line.DiscountAmount = Math.Round(line.Quantity * line.UnitPrice * line.DiscountRate / 100m, 2);
        line.TotalPrice     = Math.Round(line.Quantity * line.UnitPrice - line.DiscountAmount, 2);
    }

    private readonly record struct BillTotals(
        decimal SubTotal,
        decimal BillDiscountAmount,
        decimal TotalAmount,
        decimal FreeIssueValue,
        decimal FreeIssueValueCompany,
        decimal FreeIssueValueDistributor,
        decimal ReturnValue,
        decimal DistributorReturnValue,
        decimal ItemWiseTotalDiscount,
        decimal TotalDiscount);

    /// <summary>
    /// Rolls the bill-level amounts up from the lines. Every line must already have had
    /// <see cref="ApplyLineMath"/> applied.
    /// <para>
    /// Sale and outlet-return amounts are summed from the UNROUNDED line math and rounded once per
    /// header field, so the bill total matches the mobile cart and the client's old app to the cent
    /// (BIL-2026-00002: 3734.94). Rounding each line first and summing drifts by a cent whenever
    /// several lines carry a sub-cent discount. Lines still store their own rounded TotalPrice, so
    /// Σ line.TotalPrice may differ from the header by a cent — the header is authoritative.
    /// </para>
    /// </summary>
    private static BillTotals RecomputeTotals(IEnumerable<BillingItem> lines, decimal billDiscountRate)
    {
        decimal subTotal               = 0m;
        decimal freeIssueCompany       = 0m;
        decimal freeIssueDistributor   = 0m;
        decimal returnValue            = 0m;
        decimal distributorReturnValue = 0m;
        decimal itemWiseTotalDiscount  = 0m;   // Σ discountAmount for Sale lines only

        foreach (var line in lines)
        {
            switch (line.BillingItemType)
            {
                case BillingItemType.FreeIssue:
                    if (line.FreeIssueSource == FreeIssueSource.Distributor)
                        freeIssueDistributor += line.TotalPrice;
                    else
                        freeIssueCompany += line.TotalPrice;
                    break;

                case BillingItemType.Sale:
                    subTotal              += ExactNet(line);
                    itemWiseTotalDiscount += ExactDiscount(line);
                    break;

                default: // Return
                    // Every outlet return is credited to the outlet — Damage and Expire included —
                    // matching the total the mobile cart shows the rep at the counter. Only the stock
                    // effect differs: MarketResell goes back to Normal stock, Damage/Expire are write-offs.
                    if (line.ReturnType is ReturnType.MarketResell or ReturnType.Damage or ReturnType.Expire)
                        returnValue += ExactNet(line);
                    // A DistributorReturn is tracked separately and deliberately NOT added to
                    // returnValue. returnValue is subtracted from TotalAmount, but the parent
                    // Sale/FreeIssue line has already been reduced by this same quantity — counting
                    // it here too would deduct the distributor's reduction twice.
                    else if (line.ReturnType == ReturnType.DistributorReturn)
                        distributorReturnValue += line.TotalPrice;
                    break;
            }
        }

        var billDiscount = subTotal * billDiscountRate / 100m;

        return new BillTotals(
            SubTotal:                  Money(subTotal),
            BillDiscountAmount:        Money(billDiscount),
            TotalAmount:               Money(subTotal - billDiscount - returnValue),
            FreeIssueValue:            freeIssueCompany + freeIssueDistributor,
            FreeIssueValueCompany:     freeIssueCompany,
            FreeIssueValueDistributor: freeIssueDistributor,
            ReturnValue:               Money(returnValue),
            DistributorReturnValue:    distributorReturnValue,
            ItemWiseTotalDiscount:     Money(itemWiseTotalDiscount),
            TotalDiscount:             Money(itemWiseTotalDiscount + billDiscount));
    }

    private static decimal ExactDiscount(BillingItem line) => line.Quantity * line.UnitPrice * line.DiscountRate / 100m;
    private static decimal ExactNet(BillingItem line)      => line.Quantity * line.UnitPrice - ExactDiscount(line);

    // Half away from zero — what the mobile cart's toStringAsFixed(2) and Postgres ROUND(numeric, 2) do.
    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static void ApplyTotals(Billing billing, BillTotals t)
    {
        billing.SubTotalAmount            = t.SubTotal;
        billing.BillDiscountAmount        = t.BillDiscountAmount;
        billing.TotalAmount               = t.TotalAmount;
        billing.FreeIssueValue            = t.FreeIssueValue;
        billing.FreeIssueValueCompany     = t.FreeIssueValueCompany;
        billing.FreeIssueValueDistributor = t.FreeIssueValueDistributor;
        billing.ReturnValue               = t.ReturnValue;
        billing.DistributorReturnValue    = t.DistributorReturnValue;
        billing.ItemWiseTotalDiscount     = t.ItemWiseTotalDiscount;
        billing.TotalDiscount             = t.TotalDiscount;
    }

    // ── Stock reversal ────────────────────────────────────────────────────

    /// <summary>
    /// The stock pool a bill line moves at creation (and therefore on reversal), or null when the
    /// line moves no stock. Must stay in step with the switch in CreateAsync ⑫ and
    /// <see cref="ReverseStockForBillingAsync"/>:
    /// Sale and Distributor-funded FOC → Normal; Company-funded FOC → FreeIssue;
    /// MarketResell return → Normal; Damage / Expire / DistributorReturn → none.
    /// </summary>
    private static StockType? StockPoolOf(BillingItem item) => item.BillingItemType switch
    {
        BillingItemType.Sale => StockType.Normal,
        BillingItemType.FreeIssue when item.FreeIssueSource == FreeIssueSource.Distributor => StockType.Normal,
        BillingItemType.FreeIssue => StockType.FreeIssue,
        BillingItemType.Return when item.ReturnType == Enums.ReturnType.MarketResell => StockType.Normal,
        _ => null
    };

    private static IEnumerable<StockKey> StockKeysFor(int distributorId, IEnumerable<BillingItem> items)
        => items.Select(i => (i.ProductId, Pool: StockPoolOf(i)))
                .Where(x => x.Pool.HasValue)
                .Select(x => new StockKey(distributorId, x.ProductId, x.Pool!.Value))
                .Distinct();

    /// <summary>
    /// Mirrors the stock movements made at bill creation — must be called inside an open transaction.
    /// Sale + Distributor FOC: credits Normal stock back.
    /// Company FOC: credits FreeIssue stock back.
    /// MarketResell return: deducts Normal stock (reverses the credit that was issued at creation).
    /// Damage/Expire returns had no stock movement, so nothing to reverse.
    /// </summary>
    private async Task ReverseStockForBillingAsync(Billing billing, int actorId, string notes, CancellationToken ct)
    {
        // Lock every row the reversal touches in one ordered FOR UPDATE; the credits/deducts below
        // then work on the tracked rows without reloading them.
        await _stockRepository.LockStocksForUpdateAsync(StockKeysFor(billing.DistributorId, billing.Items), ct);

        foreach (var item in billing.Items)
        {
            switch (item.BillingItemType)
            {
                case BillingItemType.Sale:
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.FreeIssue when item.FreeIssueSource == FreeIssueSource.Distributor:
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.FreeIssue:
                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.FreeIssue, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                case BillingItemType.Return when item.ReturnType == Enums.ReturnType.MarketResell:
                    await _stockRepository.DeductStockAsync(
                        billing.DistributorId, item.ProductId, item.Quantity,
                        StockType.Normal, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, actorId, notes: notes, ct: ct);
                    break;

                // BillingItemType.Return (Damage / Expire): no stock movement at creation → nothing to reverse
                //
                // BillingItemType.Return (DistributorReturn): also nothing to reverse, and the
                // `when ReturnType == MarketResell` guard above is what keeps it that way. Those units
                // were already credited back to the distributor by AdjustItemsAsync at the moment the
                // quantity was reduced, and the parent Sale/FreeIssue line — which this loop does
                // reverse — now carries only the reduced quantity. Reversing the return line as well
                // would credit the same physical units twice.
            }
        }
    }

    // ── Distributor quantity adjustment ───────────────────────────────────

    public async Task<BillingDto> AdjustItemsAsync(
        int billingId, int userId, AdjustBillingItemsRequest request, CancellationToken ct = default)
    {
        // Same lock key as approve/reject/cancel/payment-type, so an adjustment can never interleave
        // with a status transition on the same bill.
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

        var billing = await _billingRepository.GetTrackedByIdWithItemsAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserAccessInfoAsync(userId, ct);
        if (user?.DistributorId == null || user.DistributorId != billing.DistributorId)
            throw new AuthorizationException("Billing");

        if (billing.RepStatus != RepBillingStatus.Submitted)
            throw new BusinessRuleException(
                "BILLING_NOT_ACTIONABLE",
                $"Billing {billing.BillingNumber} cannot be adjusted — rep status is {billing.RepStatus}.");

        if (billing.DistributorStatus != DistributorBillingStatus.Pending)
            throw new BusinessRuleException(
                "BILLING_ALREADY_ACTIONED",
                $"Billing {billing.BillingNumber} has already been {billing.DistributorStatus}.");

        var itemsById = billing.Items.ToDictionary(i => i.Id);

        // ① Validate every requested line against the bill as it stands right now, before mutating
        //    anything. A second adjustment round compares against the already-reduced quantity.
        var reductions = new List<(BillingItem Line, decimal OldQuantity, decimal NewQuantity)>();
        foreach (var requested in request.Items)
        {
            if (!itemsById.TryGetValue(requested.BillingItemId, out var line))
                throw new BusinessRuleException(
                    "BILLING_ITEM_NOT_ON_BILL",
                    $"Billing item {requested.BillingItemId} does not belong to bill {billing.BillingNumber}.");

            if (line.BillingItemType is not (BillingItemType.Sale or BillingItemType.FreeIssue))
                throw new BusinessRuleException(
                    "BILLING_LINE_NOT_ADJUSTABLE",
                    $"Line {line.LineNumber} is a {line.BillingItemType} line — only Sale and Free Issue lines can be adjusted.");

            if (requested.Quantity > line.Quantity)
                throw new BusinessRuleException(
                    "BILLING_QTY_INCREASE_NOT_ALLOWED",
                    $"Line {line.LineNumber} cannot be increased above the billed quantity of {line.Quantity:0.####}.",
                    new { billingItemId = line.Id, currentQuantity = line.Quantity, requestedQuantity = requested.Quantity });

            if (requested.Quantity < 0m)
                throw new BusinessRuleException(
                    "BILLING_QTY_NEGATIVE",
                    $"Line {line.LineNumber} cannot have a negative quantity.");

            if (requested.Quantity < line.Quantity)
                reductions.Add((line, line.Quantity, requested.Quantity));
        }

        if (reductions.Count == 0)
            throw new BusinessRuleException(
                "BILLING_NO_CHANGES",
                "No quantities were changed.");

        // ② A bill where every sale and free-issue line ends at zero is not an adjustment — the
        //    distributor should reject it so the rep is notified and the trail reads correctly.
        var wouldBeZero = reductions.Where(r => r.NewQuantity == 0m).Select(r => r.Line.Id).ToHashSet();
        var anyRemaining = billing.Items.Any(i =>
            i.BillingItemType is BillingItemType.Sale or BillingItemType.FreeIssue
            && !wouldBeZero.Contains(i.Id)
            && i.Quantity > 0m);
        if (!anyRemaining)
            throw new BusinessRuleException(
                "BILLING_ALL_LINES_ZERO",
                "Every line would be reduced to zero. Reject the bill instead of zeroing it.");

        var now              = DateTime.UtcNow;
        var oldTotalAmount   = billing.TotalAmount;
        var nextLineNumber   = billing.Items.Count == 0 ? 1 : billing.Items.Max(i => i.LineNumber) + 1;
        var adjustment = new BillingAdjustment
        {
            BillingId        = billing.Id,
            AdjustedByUserId = userId,
            AdjustedAt       = now,
            Note             = request.Note,
            OldTotalAmount   = oldTotalAmount
        };
        var returnLines = new List<BillingItem>();

        // ③ Reduce each line and carve the difference off into a DistributorReturn line, priced at
        //    the parent line's unit price and discount rate as they stand at this moment.
        foreach (var (line, oldQuantity, newQuantity) in reductions)
        {
            var oldTotalPrice = line.TotalPrice;
            var returnedQty   = oldQuantity - newQuantity;

            line.OriginalQuantity ??= oldQuantity;   // first adjustment only — 10→7→5 still reports 10
            line.Quantity           = newQuantity;
            ApplyLineMath(line);

            var returnLine = new BillingItem
            {
                BillingId           = billing.Id,
                ProductId           = line.ProductId,
                Quantity            = returnedQty,
                UnitPrice           = line.UnitPrice,
                DiscountRate        = line.DiscountRate,
                BillingItemType     = BillingItemType.Return,
                ReturnType          = ReturnType.DistributorReturn,
                // Carried over so a later reversal can tell which stock pool this line's parent drew
                // from; null on a Sale parent.
                FreeIssueSource     = line.FreeIssueSource,
                ExpireDate          = line.ExpireDate,
                LineNumber          = nextLineNumber++,
                Source              = BillingItemSource.DistributorReturn,
                SourceBillingItemId = line.Id,
                // Same frozen pricing as the parent — the reduction is valued exactly as it was sold.
                PricingStructureId  = line.PricingStructureId,
                PriceBasis          = line.PriceBasis,
                ListUnitPrice       = line.ListUnitPrice,
                CreatedAt           = now
            };
            // DiscountRate is 0 on a FreeIssue parent, so ApplyLineMath prices a FOC return at the
            // full unit value — the same basis the original line used.
            ApplyLineMath(returnLine);
            returnLines.Add(returnLine);

            adjustment.Lines.Add(new BillingAdjustmentLine
            {
                BillingItemId    = line.Id,
                ProductId        = line.ProductId,
                OldQuantity      = oldQuantity,
                NewQuantity      = newQuantity,
                OldTotalPrice    = oldTotalPrice,
                NewTotalPrice    = line.TotalPrice,
                ReturnedQuantity = returnedQty,
                ReturnValue      = returnLine.TotalPrice
            });
        }

        foreach (var returnLine in returnLines)
            billing.Items.Add(returnLine);

        // ④ Roll the header up from the lines, using the same math the create path uses.
        var totals = RecomputeTotals(billing.Items, billing.BillDiscountRate);

        if (totals.TotalAmount < 0m)
            throw new BusinessRuleException(
                "BILL_TOTAL_NEGATIVE",
                $"Bill total would be negative ({totals.TotalAmount:F2}): sub-total {totals.SubTotal:F2} minus " +
                $"bill discount {totals.BillDiscountAmount:F2} and returns {totals.ReturnValue:F2}.",
                new { subTotal = totals.SubTotal, billDiscountAmount = totals.BillDiscountAmount, returnValue = totals.ReturnValue, totalAmount = totals.TotalAmount });

        ApplyTotals(billing, totals);
        adjustment.NewTotalAmount = billing.TotalAmount;
        billing.Adjustments.Add(adjustment);
        billing.LastAdjustedAt = now;
        billing.AdjustmentCount += 1;
        billing.UpdatedAt = now;
        billing.UpdatedBy = userId;

        // ⑤ Persist and credit the returned quantities back, atomically. EnableRetryOnFailure is on,
        //    so a manual transaction must run inside the execution strategy or it faults in prod.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _billingRepository.BeginTransactionAsync(ct);
            try
            {
                await _billingRepository.SaveChangesAsync(ct);

                // One ordered FOR UPDATE over every pool row the credits touch.
                await _stockRepository.LockStocksForUpdateAsync(
                    StockKeysFor(billing.DistributorId, reductions.Select(r => r.Line)), ct);

                foreach (var (line, oldQuantity, newQuantity) in reductions)
                {
                    var returnedQty = oldQuantity - newQuantity;

                    // Credit back into whichever pool the parent line drew from at creation
                    // (BillingService.CreateAsync ⑫). Company-funded FOC came out of the FreeIssue
                    // pool; everything else out of Normal.
                    var pool = line.BillingItemType == BillingItemType.FreeIssue
                               && line.FreeIssueSource != FreeIssueSource.Distributor
                        ? StockType.FreeIssue
                        : StockType.Normal;

                    await _stockRepository.CreditStockAsync(
                        billing.DistributorId, line.ProductId, returnedQty,
                        pool, StockTransactionType.BillingReversal,
                        "Billing", billing.Id, userId,
                        notes: "Distributor return on bill adjustment", ct: ct);
                }

                await _billingRepository.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        // A pending bill does not move the approved-only sales summary, but it does move the pending
        // totals on the rep's screens, which are cached the same way.
        await InvalidateSalesCachesAsync(billing, affectsApprovedTotals: false, ct);

        var result = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();

        await _notificationService.SendToUserAsync(
            billing.SalesRepId,
            "Bill Quantities Adjusted",
            $"The distributor adjusted quantities on bill {result.BillingNumber}. New total: {result.TotalAmount:N2}.",
            new Dictionary<string, string>
            {
                ["type"] = "BILL_ADJUSTED",
                ["billingId"] = billing.Id.ToString(),
                ["billingNumber"] = result.BillingNumber
            }, ct);

        return ProjectToDto(result);
    }

    // ── Pricing audit ─────────────────────────────────────────────────────

    /// <summary>
    /// Warns when a Pack/Case line's price differs from its structure's CURRENT price. Never rejects:
    /// the phone is trusted (an offline bill legitimately carries a price that has since changed), so
    /// this only surfaces drift or tampering in the logs. Legacy lines (no PriceBasis) are skipped.
    /// </summary>
    private async Task LogPriceMismatchesAsync(
        CreateBillingRequest request, Func<CreateBillingItemRequest, int?> lineStructureId, CancellationToken ct)
    {
        var checkable = request.Items
            .Where(i => i.PriceBasis is PriceBasis.Pack or PriceBasis.Case && lineStructureId(i) is not null)
            .ToList();
        if (checkable.Count == 0) return;

        var prices = await _pricingStructureRepository.GetPricesAsync(
            checkable.Select(i => lineStructureId(i)!.Value).Distinct().ToList(),
            checkable.Select(i => i.ProductId).Distinct().ToList(), ct);

        foreach (var item in checkable)
        {
            var structureId = lineStructureId(item)!.Value;
            prices.TryGetValue((structureId, item.ProductId), out var current);
            // A structure without a case price is legitimate: the phone then derives the case price
            // as pack × packs-per-case, so there is nothing to compare against.
            if (item.PriceBasis == PriceBasis.Case && current is { DealerPackPrice: not null, DealerCasePrice: null })
                continue;
            var expected = item.PriceBasis == PriceBasis.Pack ? current?.DealerPackPrice : current?.DealerCasePrice;
            var sent     = item.PriceBasis == PriceBasis.Pack ? item.UnitPrice : item.ListUnitPrice;
            if (expected is null || sent is null || Math.Round(expected.Value, 2) != Math.Round(sent.Value, 2))
                _logger.LogWarning(
                    "Bill price differs from pricing structure {PricingStructureId} for product {ProductId} ({PriceBasis}): sent {Sent}, structure has {Expected}",
                    structureId, item.ProductId, item.PriceBasis, sent, expected);
        }
    }

    // ── Projection ────────────────────────────────────────────────────────

    private static BillingDto ProjectToDto(Billing b)
    {
        // Adjustment lines carry only a ProductId; the products themselves are already loaded on the
        // bill's items (an adjusted line is by definition a line on this bill), so resolve names from
        // there rather than issuing another query.
        var productsById = b.Items
            .Where(i => i.Product is not null)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.First().Product);

        return ProjectToDto(b, productsById);
    }

    private static BillingDto ProjectToDto(
        Billing b,
        IReadOnlyDictionary<int, sfa_api.Features.Products.Entities.Product> productsById) => new(
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
        b.ItemWiseTotalDiscount,
        b.TotalDiscount,
        b.DistributorReturnValue,
        b.RepStatus,
        b.DistributorStatus,
        b.RejectionReason,
        b.PaymentType,
        b.IsCashCollected,
        b.Notes,
        b.Latitude,
        b.Longitude,
        b.DistanceFromOutletMeters,
        b.GpsAccuracyMeters,
        b.ProximityOverridden,
        b.PricingStructureId,
        b.PricingStructure?.Name,
        b.CreatedAt,
        // Explicit Id tiebreaks: GetByIdAsync uses AsSplitQuery, which (unlike the old single JOIN query,
        // ordered by keys) doesn't guarantee child-row order — these reproduce the previous output order.
        b.Items.OrderBy(i => i.LineNumber).ThenBy(i => i.Id).Select(i => new BillingItemDto(
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
            i.LineNumber,
            i.Source,
            i.SourceBillingItemId,
            i.OriginalQuantity,
            i.PricingStructureId,
            i.PricingStructure?.Name,
            i.PriceBasis,
            i.ListUnitPrice)).ToList(),
        b.LastAdjustedAt,
        b.AdjustmentCount,
        b.Adjustments.OrderByDescending(a => a.AdjustedAt).ThenBy(a => a.Id).Select(a => new BillingAdjustmentDto(
            a.Id,
            a.AdjustedByUserId,
            a.AdjustedBy?.Name ?? string.Empty,
            a.AdjustedAt,
            a.Note,
            a.OldTotalAmount,
            a.NewTotalAmount,
            a.Lines.OrderBy(l => l.Id).Select(l => new BillingAdjustmentLineDto(
                l.BillingItemId,
                l.ProductId,
                productsById.TryGetValue(l.ProductId, out var p) ? p.Code : string.Empty,
                productsById.TryGetValue(l.ProductId, out var p2) ? p2.ItemDescription : string.Empty,
                l.OldQuantity,
                l.NewQuantity,
                l.OldTotalPrice,
                l.NewTotalPrice,
                l.ReturnedQuantity,
                l.ReturnValue)).ToList())).ToList()
    );

    public async Task<OutletBillingSummaryResponseDto> GetOutletSummaryAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default)
    {
        var rows = await _billingRepository.GetOutletSummaryRawAsync(
            salesRepId, routeId, dateFrom, dateTo, ct);

        // Cancelled (rep) and Rejected (distributor) bills are still returned in the line list
        // so the UI can show them, but they must NOT inflate revenue (finding #5). The per-outlet
        // TotalAmount, BillingCount, and the GrandTotal count only revenue-contributing bills, so
        // this report reconciles with the monthly/daily aggregates (which already filter state).
        static bool IsRevenueBill(OutletBillingSummaryRawRow r)
            => r.RepStatus != RepBillingStatus.Cancelled
            && r.DistributorStatus != DistributorBillingStatus.Rejected;

        var outletSummaries = rows
            .GroupBy(r => new { r.OutletId, r.OutletName })
            .Select(g => new OutletBillingSummaryDto(
                g.Key.OutletId,
                g.Key.OutletName,
                g.Count(IsRevenueBill),
                g.Where(IsRevenueBill).Sum(r => r.TotalAmount),
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

    /// <summary>
    /// Evicts the cached sales aggregates a change to <paramref name="billing"/> can move. Must be
    /// called after ANY change to a bill's state or amounts — create, adjust, cancel, approve, reject.
    /// <para>
    /// Without this, a distributor approves a bill and then sees a report that predates their own
    /// action for up to the cache TTL, which is indistinguishable from the feature being broken.
    /// Read-through caches for aggregates need an invalidation hook on the write path; a TTL alone
    /// is only acceptable when nothing in the product lets a user cause the change and then
    /// immediately look at the result.
    /// </para>
    /// <para>
    /// Scope: the rep's own screens are keyed by rep + bill month/day (both filter on
    /// <see cref="Billing.BillingDate"/>), so exactly those three keys are evicted instead of every
    /// rep's. The sales summary aggregates across reps with arbitrary filters, so it has no narrower
    /// key — but it counts Approved, non-cancelled bills only, so its namespace is bumped only when
    /// <paramref name="affectsApprovedTotals"/> (approve, or cancel of an already-approved bill).
    /// </para>
    /// </summary>
    private async Task InvalidateSalesCachesAsync(Billing billing, bool affectsApprovedTotals, CancellationToken ct)
    {
        if (affectsApprovedTotals)
            await _cache.RemoveByPrefixAsync("sales-summary:", ct);      // Features/Reports

        var date = billing.BillingDate;
        await _cache.RemoveAsync(RepMonthlySalesCacheKey(billing.SalesRepId, date.Year, date.Month), ct);
        await _cache.RemoveAsync(RepDailySalesCacheKey(billing.SalesRepId, date), ct);
        await _cache.RemoveAsync(RepMonthlySalesItemwiseCacheKey(billing.SalesRepId, date.Year, date.Month), ct);

        // Supervisor dashboard counts/sums this supervisor's bills per day — evict only their entries.
        if (billing.SupervisorUserId is int supervisorId)
            await _cache.RemoveByPrefixAsync(Supervisor.SupervisorSummaryCacheKeys.ForSupervisor(supervisorId), ct);
    }

    private static string RepMonthlySalesCacheKey(int salesRepId, int year, int month)
        => $"rep-sales:{salesRepId}:{year}:{month}";

    private static string RepDailySalesCacheKey(int salesRepId, DateOnly date)
        => $"rep-sales-daily:{salesRepId}:{date:yyyy-MM-dd}";

    private static string RepMonthlySalesItemwiseCacheKey(int salesRepId, int year, int month)
        => $"rep-sales-itemwise:{salesRepId}:{year}:{month}";

    public async Task<RepMonthlySalesDto> GetRepMonthlySalesAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
    {
        var cacheKey = RepMonthlySalesCacheKey(salesRepId, year, month);
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
        var cacheKey = RepDailySalesCacheKey(salesRepId, date);
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
        // Serialize all status transitions for this bill so cancel/approve/reject cannot race
        // each other (e.g. a concurrent cancel + approve both observing 'Submitted').
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

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

        await InvalidateSalesCachesAsync(billing, affectsApprovedTotals: billing.DistributorStatus == DistributorBillingStatus.Approved, ct);

        var updated = await _billingRepository.GetByIdAsync(billingId, ct)
            ?? throw new DatabaseUnavailableException();
        return ProjectToDto(updated);
    }

    public async Task<BillingDto> ApproveAsync(int billingId, int userId, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserAccessInfoAsync(userId, ct);
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
        await InvalidateSalesCachesAsync(billing, affectsApprovedTotals: true, ct);

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
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

        var billing = await _billingRepository.GetTrackedByIdWithItemsAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserAccessInfoAsync(userId, ct);
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

        await InvalidateSalesCachesAsync(billing, affectsApprovedTotals: false, ct);

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
        // Serialize with other transitions on this bill (approve/reject/cancel/cash) so two
        // concurrent distributor edits can't race into a lost update (audit finding #12).
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserAccessInfoAsync(userId, ct);
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
        // Serialize with other transitions on this bill so two concurrent edits can't race
        // into a lost update (audit finding #12).
        await using var @lock = await _lockService.AcquireAsync($"billing:transition:{billingId}", ct)
            ?? throw new ConcurrencyConflictException(new { billingId, message = "Another operation is in progress for this billing." });

        var billing = await _billingRepository.GetTrackedByIdAsync(billingId, ct)
            ?? throw new NotFoundException("Billing", billingId);

        var user = await _userRepository.GetUserAccessInfoAsync(userId, ct);
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
        var cacheKey = RepMonthlySalesItemwiseCacheKey(salesRepId, year, month);
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
