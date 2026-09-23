using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Services;

public interface IStockAdjustmentService
{
    Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request, int callerId, CancellationToken ct = default);

    Task<(List<StockAdjustmentSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? distributorId, CancellationToken ct = default);

    Task<StockAdjustmentDto> GetByIdAsync(int id, CancellationToken ct = default);
}
