using sfa_api.Features.Stock.Entities;

namespace sfa_api.Features.Stock.Repositories;

public interface IStockRepository
{
    Task<List<DistributorStock>> GetStockByDistributorAsync(int distributorId, CancellationToken ct = default);
    Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default);
}
