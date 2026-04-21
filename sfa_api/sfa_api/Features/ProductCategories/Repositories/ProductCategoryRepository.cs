using Microsoft.EntityFrameworkCore;
using sfa_api.Features.ProductCategories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.ProductCategories.Repositories;

public class ProductCategoryRepository(AppDbContext context) : IProductCategoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<ProductCategory?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.ProductCategories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(IEnumerable<ProductCategory> ProductCategories, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.ProductCategories.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(c => EF.Functions.ILike(c.Name, pattern))
                : query.Where(c => EF.Functions.Like(c.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<ProductCategory>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct = default)
        => await _context.ProductCategories.IgnoreQueryFilters().AnyAsync(c => c.Id == id && c.IsActive && !c.IsDeleted, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.ProductCategories.IgnoreQueryFilters().AnyAsync(c => c.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
        => await _context.ProductCategories.IgnoreQueryFilters().AnyAsync(c => c.Name == name && c.Id != excludeId, ct);

    public async Task CreateAsync(ProductCategory category, CancellationToken ct = default)
        => await _context.ProductCategories.AddAsync(category, ct);

    public Task UpdateAsync(ProductCategory category, CancellationToken ct = default)
    {
        _context.ProductCategories.Update(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
