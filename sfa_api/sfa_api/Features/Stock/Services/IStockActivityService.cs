using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Services;

public interface IStockActivityService
{
    /// <summary>Expects a query already checked by StockActivityQueryValidator.</summary>
    Task<(List<StockActivityDto> Items, int TotalCount)> GetActivityAsync(StockActivityQuery query, CancellationToken ct = default);

    Task<List<StockActivityUserDto>> GetUsersAsync(CancellationToken ct = default);
}
