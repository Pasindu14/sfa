using sfa_api.Features.SalesTargets.DTOs;

namespace sfa_api.Features.SalesTargets.Services;

public interface ISalesTargetService
{
    Task<(IEnumerable<SalesTargetDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        int? year = null,
        int? month = null,
        int? salesRepId = null,
        int? productId = null,
        string? search = null,
        CancellationToken ct = default);

    Task<(IEnumerable<SalesTargetImportBatchDto> Items, int TotalCount)> GetBatchesPagedAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<SalesTargetImportBatchDto?> GetBatchByIdAsync(int id, CancellationToken ct = default);

    Task<SalesTargetDto?> UpdateQuantityAsync(int id, decimal targetQuantity, int callerId, CancellationToken ct = default);
}
