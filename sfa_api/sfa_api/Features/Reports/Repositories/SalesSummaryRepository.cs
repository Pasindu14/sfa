using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.SalesTargets.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Reports.Repositories;

public class SalesSummaryRepository(AppDbContext context) : ISalesSummaryRepository
{
    private readonly AppDbContext _context = context;

    // ── Grouping key selectors ────────────────────────────────────────────────────────────────
    //
    // The grouping dimension is chosen at request time, but only the KEY varies — every one of the
    // report's column formulas is written exactly once, below. That is what guarantees a
    // rep-grouped total and a product-grouped total are the same number by construction.
    //
    // Group by the raw FK int, never by a navigation: Outlet, Route and the geo entities all carry
    // a global IsActive && !IsDeleted query filter, and those navigations are required references,
    // so EF would fold the filter into an INNER JOIN and silently drop bills written against a
    // since-deactivated master row.

    private static Expression<Func<BillingItem, int?>> SalesKey(SalesSummaryGroupBy g) => g switch
    {
        SalesSummaryGroupBy.SalesRep    => bi => bi.Billing.SalesRepId,
        SalesSummaryGroupBy.Supervisor  => bi => bi.Billing.SupervisorUserId,
        SalesSummaryGroupBy.Asm         => bi => bi.Billing.AsmUserId,
        SalesSummaryGroupBy.Rsm         => bi => bi.Billing.RsmUserId,
        SalesSummaryGroupBy.Nsm         => bi => bi.Billing.NsmUserId,
        SalesSummaryGroupBy.Distributor => bi => bi.Billing.DistributorId,
        SalesSummaryGroupBy.Outlet      => bi => bi.Billing.OutletId,
        SalesSummaryGroupBy.Route       => bi => bi.Billing.RouteId,
        SalesSummaryGroupBy.Division    => bi => bi.Billing.DivisionId,
        SalesSummaryGroupBy.Territory   => bi => bi.Billing.TerritoryId,
        SalesSummaryGroupBy.Area        => bi => bi.Billing.AreaId,
        SalesSummaryGroupBy.Region      => bi => bi.Billing.RegionId,
        SalesSummaryGroupBy.Product     => bi => bi.ProductId,   // header has no ProductId — item side only
        _ => throw new ArgumentOutOfRangeException(nameof(g), g, "Unsupported grouping dimension."),
    };

    /// <summary>
    /// Null means SalesTarget carries no column for this dimension, so the report runs sales-only.
    /// This switch is the single source of truth for "are targets available for this grouping".
    /// </summary>
    private static Expression<Func<SalesTarget, int?>>? TargetKey(SalesSummaryGroupBy g) => g switch
    {
        SalesSummaryGroupBy.SalesRep    => t => t.SalesRepId,
        SalesSummaryGroupBy.Supervisor  => t => t.SupervisorUserId,
        SalesSummaryGroupBy.Asm         => t => t.AsmUserId,
        SalesSummaryGroupBy.Rsm         => t => t.RsmUserId,
        SalesSummaryGroupBy.Nsm         => t => t.NsmUserId,
        SalesSummaryGroupBy.Distributor => t => t.DistributorId,
        SalesSummaryGroupBy.Division    => t => t.DivisionId,
        SalesSummaryGroupBy.Territory   => t => t.TerritoryId,
        SalesSummaryGroupBy.Area        => t => t.AreaId,
        SalesSummaryGroupBy.Region      => t => t.RegionId,
        SalesSummaryGroupBy.Product     => t => t.ProductId,
        SalesSummaryGroupBy.Route or SalesSummaryGroupBy.Outlet => null,  // no such column on SalesTarget
        _ => throw new ArgumentOutOfRangeException(nameof(g), g, "Unsupported grouping dimension."),
    };

    // ── Sales facts ───────────────────────────────────────────────────────────────────────────

    public async Task<List<SalesSummarySalesAgg>> GetSalesAggregatesAsync(
        SalesSummaryQuery q, int maxGroups, CancellationToken ct = default)
    {
        // HISTORICAL FACTS universe (.claude/docs/reporting-conventions.md): filter the BILL's own
        // state and status. The current IsActive of the referenced Product/Outlet/Route/geo row is
        // deliberately NOT filtered — a sale of a since-discontinued SKU is still revenue, and this
        // report must not silently shrink when master data is deactivated.
        //
        // RepStatus != Cancelled is required IN ADDITION to the Approved check: CancelAsync
        // (BillingService.cs:635) gates only on RepStatus == Submitted and never inspects
        // DistributorStatus, so a bill can be approved and THEN cancelled by the rep, landing at
        // (Cancelled, Approved) with its stock already reversed. Matches IsRevenueBill
        // (BillingService.cs:571) and BinCardRepository.cs:75.
        //
        // !IsDeleted on both Billing and BillingItem arrives via the global query filters.
        var items = _context.BillingItems
            .AsNoTracking()
            .Where(bi => bi.Billing.DistributorStatus == DistributorBillingStatus.Approved
                      && bi.Billing.RepStatus != RepBillingStatus.Cancelled
                      && bi.Billing.IsActive
                      && bi.Billing.BillingDate >= q.From
                      && bi.Billing.BillingDate <= q.To);

        if (q.RegionId      is int rg) items = items.Where(bi => bi.Billing.RegionId         == rg);
        if (q.AreaId        is int ar) items = items.Where(bi => bi.Billing.AreaId           == ar);
        if (q.TerritoryId   is int te) items = items.Where(bi => bi.Billing.TerritoryId      == te);
        if (q.DivisionId    is int dv) items = items.Where(bi => bi.Billing.DivisionId       == dv);
        if (q.RouteId       is int ro) items = items.Where(bi => bi.Billing.RouteId          == ro);
        if (q.DistributorId is int di) items = items.Where(bi => bi.Billing.DistributorId    == di);
        if (q.SalesRepId    is int sr) items = items.Where(bi => bi.Billing.SalesRepId       == sr);
        if (q.SupervisorId  is int su) items = items.Where(bi => bi.Billing.SupervisorUserId == su);
        if (q.AsmId         is int am) items = items.Where(bi => bi.Billing.AsmUserId        == am);
        if (q.RsmId         is int rm) items = items.Where(bi => bi.Billing.RsmUserId        == rm);
        if (q.NsmId         is int nm) items = items.Where(bi => bi.Billing.NsmUserId        == nm);
        if (q.ProductId     is int pr) items = items.Where(bi => bi.ProductId                == pr);

        // EF cannot bind a GroupBy result to a positional record ctor (BinCardRepository.cs:16-17),
        // so project to an anonymous type and map in memory. The aggregation still runs in SQL.
        var raw = await items
            .GroupBy(SalesKey(q.GroupBy))
            .Select(g => new
            {
                Key = g.Key,

                // Sale value BEFORE any discount. Written as TotalPrice + DiscountAmount rather than
                // Quantity * UnitPrice so it is bit-identical to the per-line rounding the write path
                // applied (BillingService.cs:218-219) — that is what keeps the header cross-check in
                // SalesSummaryService exact.
                SaleGross = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                       ? x.TotalPrice + x.DiscountAmount : 0m),
                SaleQty   = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                       ? x.Quantity : 0m),                    // already in packs
                ItemWise  = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                       ? x.DiscountAmount : 0m),

                // Bill-header discount, allocated across the bill's sale lines. It is a flat
                // percentage of the sale sub-total (BillingService.cs:249), so the per-line shares
                // sum back to Billing.BillDiscountAmount up to the one Math.Round(.,2) per bill —
                // and exactly, not approximately, whenever BillDiscountRate is 0 (the default).
                BillDisc  = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                       ? x.TotalPrice * x.Billing.BillDiscountRate / 100m : 0m),

                GoodRetVal = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                     && x.ReturnType == ReturnType.MarketResell ? x.TotalPrice : 0m),
                GoodRetQty = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                     && x.ReturnType == ReturnType.MarketResell ? x.Quantity : 0m),

                // Damage/Expire contribute ZERO to every Billing header column
                // (BillingService.cs:226 accumulates MarketResell only), so they are reachable only
                // from the item rows — same predicate shape as BinCardRepository.cs:70-76.
                MktRetVal = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                    && (x.ReturnType == ReturnType.Damage
                                     || x.ReturnType == ReturnType.Expire) ? x.TotalPrice : 0m),
                MktRetQty = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                    && (x.ReturnType == ReturnType.Damage
                                     || x.ReturnType == ReturnType.Expire) ? x.Quantity : 0m),

                DbDiscount = g.Sum(x => x.BillingItemType == BillingItemType.FreeIssue
                                     && x.FreeIssueSource == FreeIssueSource.Distributor
                                        ? x.TotalPrice : 0m),
            })
            .Take(maxGroups + 1)
            .ToListAsync(ct);

        return [.. raw.Select(r => new SalesSummarySalesAgg(
            r.Key, r.SaleGross, r.SaleQty, r.ItemWise, r.BillDisc,
            r.GoodRetVal, r.GoodRetQty, r.MktRetVal, r.MktRetQty, r.DbDiscount))];
    }

    // ── Targets ───────────────────────────────────────────────────────────────────────────────

    public async Task<List<SalesSummaryTargetAgg>> GetTargetAggregatesAsync(
        SalesSummaryQuery q,
        IReadOnlyList<(int Year, int Month)> months,
        int maxGroups,
        CancellationToken ct = default)
    {
        var key = TargetKey(q.GroupBy);
        if (key is null) return [];          // Route/Outlet — SalesTarget has no such column

        // SalesTarget has no RouteId either, so a route filter cannot be honoured on the target
        // side. Returning targets anyway would pair one route's sales with the rep's company-wide
        // target — a wrong number that looks plausible. Suppress instead.
        if (q.RouteId is not null) return [];

        var results = new List<SalesSummaryTargetAgg>();

        // Sequential by necessity — AppDbContext does not support concurrent operations
        // (SupervisorService.cs:19). One query per month keeps Year/Month as EQUALITY predicates so
        // the (dimension, Year, Month) composite indexes (AppDbContext.cs:1129-1139) stay seekable;
        // a range predicate across months would degrade them. The validator caps the window at
        // 366 days, so this runs at most 13 times — and exactly once for a single-month report.
        foreach (var (year, month) in months)
        {
            // Mirrors GetByRepAndMonthAsync (SalesTargetRepository.cs:99-101): IsActive explicit,
            // !IsDeleted from the global filter. Product carries NO global query filter by design,
            // so this required-navigation join drops nothing even for retired SKUs.
            var qm = _context.SalesTargets
                .AsNoTracking()
                .Where(t => t.Year == year && t.Month == month && t.IsActive);

            if (q.RegionId      is int rg) qm = qm.Where(t => t.RegionId         == rg);
            if (q.AreaId        is int ar) qm = qm.Where(t => t.AreaId           == ar);
            if (q.TerritoryId   is int te) qm = qm.Where(t => t.TerritoryId      == te);
            if (q.DivisionId    is int dv) qm = qm.Where(t => t.DivisionId       == dv);
            if (q.DistributorId is int di) qm = qm.Where(t => t.DistributorId    == di);
            if (q.SalesRepId    is int sr) qm = qm.Where(t => t.SalesRepId       == sr);
            if (q.SupervisorId  is int su) qm = qm.Where(t => t.SupervisorUserId == su);
            if (q.AsmId         is int am) qm = qm.Where(t => t.AsmUserId        == am);
            if (q.RsmId         is int rm) qm = qm.Where(t => t.RsmUserId        == rm);
            if (q.NsmId         is int nm) qm = qm.Where(t => t.NsmUserId        == nm);
            if (q.ProductId     is int pr) qm = qm.Where(t => t.ProductId        == pr);

            // TargetQuantity is in CASES; PiecesPerPack is semantically packs-per-case
            // (IProductRepository.cs:11-13). Both outputs are therefore in PACKS, matching
            // BillingItem.Quantity. The > 0 guard mirrors BillingService.cs:880 so a product with
            // PiecesPerPack unset converts 1:1 on BOTH sides and target stays comparable to actual —
            // multiplying by a raw 0 would silently erase a real target.
            var raw = await qm
                .GroupBy(key)
                .Select(g => new
                {
                    Key   = g.Key,
                    Qty   = g.Sum(x => x.TargetQuantity
                                       * (x.Product!.PiecesPerPack > 0 ? x.Product.PiecesPerPack : 1)),
                    Value = g.Sum(x => x.TargetQuantity
                                       * (x.Product!.PiecesPerPack > 0 ? x.Product.PiecesPerPack : 1)
                                       * x.Product!.DealerPackPrice),
                })
                .Take(maxGroups + 1)
                .ToListAsync(ct);

            results.AddRange(raw.Select(x => new SalesSummaryTargetAgg(x.Key, year, month, x.Qty, x.Value)));
        }

        return results;
    }

    // ── Labels ────────────────────────────────────────────────────────────────────────────────

    public async Task<Dictionary<int, SalesSummaryLabel>> GetLabelsAsync(
        SalesSummaryGroupBy groupBy, IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];

        // IgnoreQueryFilters throughout — see the interface docs. These label historical facts, so a
        // deactivated outlet or a retired product must still resolve to its name rather than vanish.
        return groupBy switch
        {
            SalesSummaryGroupBy.Product => await _context.Products.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(x.Code, x.ItemDescription) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.SalesRep
            or SalesSummaryGroupBy.Supervisor
            or SalesSummaryGroupBy.Asm
            or SalesSummaryGroupBy.Rsm
            or SalesSummaryGroupBy.Nsm => await _context.Users.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Distributor => await _context.Distributors.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Outlet => await _context.Outlets.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Route => await _context.Routes.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Division => await _context.Divisions.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Territory => await _context.Territories.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Area => await _context.Areas.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            SalesSummaryGroupBy.Region => await _context.Regions.IgnoreQueryFilters().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, Label = new SalesSummaryLabel(string.Empty, x.Name) })
                .ToDictionaryAsync(x => x.Id, x => x.Label, ct),

            _ => throw new ArgumentOutOfRangeException(nameof(groupBy), groupBy, "Unsupported grouping dimension."),
        };
    }
}
