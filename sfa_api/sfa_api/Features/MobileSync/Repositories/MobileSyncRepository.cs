using Microsoft.EntityFrameworkCore;
using sfa_api.Features.MobileSync.DTOs;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.MobileSync.Repositories;

public class MobileSyncRepository(AppDbContext db) : IMobileSyncRepository
{
    private readonly AppDbContext _db = db;

    public Task<List<MobileSyncProductDto>> GetActiveProductsAsync(CancellationToken ct = default)
        => _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Code)
            .Select(p => new MobileSyncProductDto(
                p.Id,
                p.Code,
                p.ItemDescription,
                p.PrintDescription,
                p.PiecesPerPack,
                p.ImageUrl,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.DealerPackPrice,
                p.DealerCasePrice,
                p.Mrp))
            .ToListAsync(ct);

    public Task<List<MobileProductCategoryDto>> GetActiveProductCategoriesAsync(CancellationToken ct = default)
        => _db.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Id)
            .Select(c => new MobileProductCategoryDto(c.Id, c.Name))
            .ToListAsync(ct);
}
