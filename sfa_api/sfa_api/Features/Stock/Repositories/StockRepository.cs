using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockRepository(AppDbContext db) : IStockRepository
{
    private readonly AppDbContext _db = db;

    public Task<List<DistributorStock>> GetStockByDistributorAsync(int distributorId, CancellationToken ct = default)
        => _db.DistributorStocks
              .AsNoTracking()
              .Include(x => x.Product)
              .Include(x => x.Distributor)
              .Where(x => x.DistributorId == distributorId)
              .OrderBy(x => x.Product.Code)
              .ToListAsync(ct);

    public Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default)
        => _db.StockTransactions
              .AsNoTracking()
              .Include(x => x.Product)
              .Where(x => x.DistributorId == distributorId && x.ProductId == productId)
              .OrderByDescending(x => x.TransactedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync(ct);

    public Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default)
        => _db.StockTransactions
              .CountAsync(x => x.DistributorId == distributorId && x.ProductId == productId, ct);
}
