using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;

namespace sfa_api.Features.Stock.Repositories;

public interface IStockAdjustmentRepository
{
    /// <summary>AsNoTracking. Includes inactive distributors (only soft-deleted ones are hidden).</summary>
    Task<Distributor?> GetDistributorAsync(int distributorId, CancellationToken ct = default);

    /// <summary>AsNoTracking, keyed by Id. Missing ids are absent from the result.</summary>
    Task<Dictionary<int, Product>> GetProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default);

    Task<(List<StockAdjustmentSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, int? distributorId, CancellationToken ct = default);

    Task<StockAdjustmentDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task AddAsync(StockAdjustment adjustment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
