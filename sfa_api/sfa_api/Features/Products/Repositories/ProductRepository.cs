using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Products.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Products.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Products.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(p => EF.Functions.ILike(p.ItemDescription, pattern) || EF.Functions.ILike(p.Code, pattern))
                : query.Where(p => EF.Functions.Like(p.ItemDescription, pattern) || EF.Functions.Like(p.Code, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.ItemDescription)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (products, totalCount);
    }

    public async Task<HashSet<int>> GetActiveProductIdsInSetAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var result = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => idList.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);
        return [.. result];
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => await _context.Products.IgnoreQueryFilters().AnyAsync(p => p.Code == code, ct);

    public async Task<bool> ExistsByCodeAsync(string code, int excludeProductId, CancellationToken ct = default)
        => await _context.Products.IgnoreQueryFilters().AnyAsync(p => p.Code == code && p.Id != excludeProductId, ct);

    public async Task CreateAsync(Product product, CancellationToken ct = default)
        => await _context.Products.AddAsync(product, ct);

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
        => await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
