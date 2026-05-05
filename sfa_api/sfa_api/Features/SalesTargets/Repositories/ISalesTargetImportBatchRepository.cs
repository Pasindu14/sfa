using sfa_api.Features.SalesTargets.Entities;

namespace sfa_api.Features.SalesTargets.Repositories;

public interface ISalesTargetImportBatchRepository
{
    Task<long> GetNextBatchNumberAsync(CancellationToken ct = default);
    Task<SalesTargetImportBatch?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<SalesTargetImportBatch> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, CancellationToken ct = default);
    Task CreateAsync(SalesTargetImportBatch batch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
