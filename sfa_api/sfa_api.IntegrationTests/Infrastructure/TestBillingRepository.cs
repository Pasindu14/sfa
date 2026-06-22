using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Stock.Entities;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IBillingRepository wrapper that replaces GetNextBillingNumberAsync
/// (which calls PostgreSQL nextval('billing_number_seq')) with an in-process atomic
/// counter. SQLite in-memory has no sequences. All other calls delegate to the real
/// BillingRepository.
/// </summary>
public sealed class TestBillingRepository(IBillingRepository inner) : IBillingRepository
{
    private static long _counter = 0;

    public Task<long> GetNextBillingNumberAsync(CancellationToken ct = default)
        => Task.FromResult(Interlocked.Increment(ref _counter));

    public Task<Outlet?> GetOutletAsync(int outletId, CancellationToken ct = default)
        => inner.GetOutletAsync(outletId, ct);

    public Task<Distributor?> GetDistributorByTerritoryAsync(int territoryId, CancellationToken ct = default)
        => inner.GetDistributorByTerritoryAsync(territoryId, ct);

    public Task<List<int>> GetActiveProductIdsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
        => inner.GetActiveProductIdsAsync(productIds, ct);

    public Task<Dictionary<int, string>> GetActiveProductNamesAsync(IEnumerable<int> productIds, CancellationToken ct = default)
        => inner.GetActiveProductNamesAsync(productIds, ct);

    public Task<List<DistributorStock>> GetStockSnapshotAsync(int distributorId, IEnumerable<int> productIds, CancellationToken ct = default)
        => inner.GetStockSnapshotAsync(distributorId, productIds, ct);

    public Task<Billing?> GetByIdAsync(int id, CancellationToken ct = default)
        => inner.GetByIdAsync(id, ct);

    public Task<int?> FindIdByClientBillIdAsync(string clientBillId, CancellationToken ct = default)
        => inner.FindIdByClientBillIdAsync(clientBillId, ct);

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
        => inner.GetListAsync(page, pageSize, repStatus, distributorStatus,
            outletId, distributorId, salesRepId, dateFrom, dateTo,
            paymentType, isCashCollected, billNo, ct);

    public Task<List<OutletBillingSummaryRawRow>> GetOutletSummaryRawAsync(
        int salesRepId, int routeId, DateOnly dateFrom, DateOnly dateTo, CancellationToken ct = default)
        => inner.GetOutletSummaryRawAsync(salesRepId, routeId, dateFrom, dateTo, ct);

    public Task<decimal> GetRepMonthlySalesTotalAsync(int salesRepId, int year, int month, CancellationToken ct = default)
        => inner.GetRepMonthlySalesTotalAsync(salesRepId, year, month, ct);

    public Task<decimal> GetRepMonthlySalesPendingTotalAsync(int salesRepId, int year, int month, CancellationToken ct = default)
        => inner.GetRepMonthlySalesPendingTotalAsync(salesRepId, year, month, ct);

    public Task<decimal> GetRepDailySalesTotalAsync(int salesRepId, DateOnly date, DistributorBillingStatus status, CancellationToken ct = default)
        => inner.GetRepDailySalesTotalAsync(salesRepId, date, status, ct);

    public Task<List<RepProductSalesRow>> GetRepMonthlySalesByProductAsync(int salesRepId, int year, int month, CancellationToken ct = default)
        => inner.GetRepMonthlySalesByProductAsync(salesRepId, year, month, ct);

    public Task AddAsync(Billing billing, CancellationToken ct = default)
        => inner.AddAsync(billing, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => inner.BeginTransactionAsync(ct);

    public Task<Billing?> GetTrackedByIdAsync(int id, CancellationToken ct = default)
        => inner.GetTrackedByIdAsync(id, ct);

    public Task<Billing?> GetTrackedByIdWithItemsAsync(int id, CancellationToken ct = default)
        => inner.GetTrackedByIdWithItemsAsync(id, ct);
}
