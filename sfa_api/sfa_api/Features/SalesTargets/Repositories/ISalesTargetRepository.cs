using sfa_api.Features.SalesTargets.Entities;

namespace sfa_api.Features.SalesTargets.Repositories;

public interface ISalesTargetRepository
{
    /// <summary>Returns existing targets keyed by (SalesRepId, ProductId) for fast upsert lookup.</summary>
    Task<Dictionary<(int SalesRepId, int ProductId), SalesTarget>> GetExistingForMonthAsync(
        int year, int month,
        IEnumerable<int> salesRepIds,
        IEnumerable<int> productIds,
        CancellationToken ct = default);

    void AddRange(IEnumerable<SalesTarget> targets);
    void UpdateRange(IEnumerable<SalesTarget> targets);

    Task<SalesTarget?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IEnumerable<SalesTarget> Items, int TotalCount)> GetPagedAsync(
        int skip, int take,
        int? year = null,
        int? month = null,
        int? salesRepId = null,
        int? productId = null,
        string? search = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
