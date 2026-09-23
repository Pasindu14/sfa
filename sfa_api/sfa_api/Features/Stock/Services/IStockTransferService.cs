using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Services;

public interface IStockTransferService
{
    Task<StockTransferDto> CreateAsync(CreateStockTransferRequest request, int callerId, CancellationToken ct = default);

    Task<(List<StockTransferSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? distributorId, CancellationToken ct = default);

    Task<StockTransferDto> GetByIdAsync(int id, CancellationToken ct = default);
}
