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
                p.ImageUrl))
            .ToListAsync(ct);
}
