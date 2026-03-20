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
        var query = _context.Products.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                EF.Functions.ILike(p.ItemDescription, $"%{search}%") ||
                EF.Functions.ILike(p.Code, $"%{search}%"));

        var totalCount = await query.CountAsync(ct);
        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.ItemDescription)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (products, totalCount);
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

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product != null)
        {
            product.IsActive = false;
            _context.Products.Update(product);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
