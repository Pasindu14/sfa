using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Stock.Entities;

namespace sfa_api.Features.Billings.Repositories;

public interface IBillingRepository
{
    Task<long> GetNextBillingNumberAsync(CancellationToken ct = default);
    Task<Outlet?> GetOutletAsync(int outletId, CancellationToken ct = default);
    Task<Distributor?> GetDistributorByTerritoryAsync(int territoryId, CancellationToken ct = default);
    Task<List<int>> GetActiveProductIdsAsync(IEnumerable<int> productIds, CancellationToken ct = default);

    /// <summary>
    /// Returns Id → ItemDescription for active products. Used by the create-billing flow
    /// so stock-out errors can name the offending products rather than citing bare IDs.
    /// </summary>
    Task<Dictionary<int, string>> GetActiveProductNamesAsync(IEnumerable<int> productIds, CancellationToken ct = default);

    /// <summary>AsNoTracking pre-check before acquiring locks.</summary>
    Task<List<DistributorStock>> GetStockSnapshotAsync(int distributorId, IEnumerable<int> productIds, CancellationToken ct = default);

    Task<Billing?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<BillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        BillingStatus? status,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default);

    Task<List<OutletBillingSummaryRawRow>> GetOutletSummaryRawAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default);

    Task<decimal> GetRepMonthlySalesTotalAsync(int salesRepId, int year, int month, CancellationToken ct = default);

    Task AddAsync(Billing billing, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
