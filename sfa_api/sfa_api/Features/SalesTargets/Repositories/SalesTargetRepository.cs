using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.SalesTargets.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesTargets.Repositories;

public class SalesTargetRepository(AppDbContext context) : ISalesTargetRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Dictionary<(int SalesRepId, int ProductId), SalesTarget>> GetExistingForMonthAsync(
        int year, int month,
        IEnumerable<int> salesRepIds,
        IEnumerable<int> productIds,
        CancellationToken ct = default)
    {
        var repList     = salesRepIds.ToList();
        var productList = productIds.ToList();

        return await _context.SalesTargets
            .Where(t => t.Year == year
                     && t.Month == month
                     && repList.Contains(t.SalesRepId)
                     && productList.Contains(t.ProductId))
            .ToDictionaryAsync(t => (t.SalesRepId, t.ProductId), ct);
    }

    public async Task<SalesTarget?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.SalesTargets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public void AddRange(IEnumerable<SalesTarget> targets)
        => _context.SalesTargets.AddRange(targets);

    public void UpdateRange(IEnumerable<SalesTarget> targets)
        => _context.SalesTargets.UpdateRange(targets);

    public async Task<(IEnumerable<SalesTarget> Items, int TotalCount)> GetPagedAsync(
        int skip, int take,
        int? year = null,
        int? month = null,
        int? salesRepId = null,
        int? productId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.SalesTargets.Where(t => !t.IsDeleted);

        if (year.HasValue)
            query = query.Where(t => t.Year == year.Value);

        if (month.HasValue)
            query = query.Where(t => t.Month == month.Value);

        if (salesRepId.HasValue)
            query = query.Where(t => t.SalesRepId == salesRepId.Value);

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(t =>
                    EF.Functions.ILike(t.SalesRep!.Name, pattern) ||
                    EF.Functions.ILike(t.Product!.Code, pattern) ||
                    EF.Functions.ILike(t.Product.ItemDescription, pattern))
                : query.Where(t =>
                    EF.Functions.Like(t.SalesRep!.Name, pattern) ||
                    EF.Functions.Like(t.Product!.Code, pattern) ||
                    EF.Functions.Like(t.Product.ItemDescription, pattern));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .Include(t => t.SalesRep)
            .Include(t => t.Product)
            .Include(t => t.Supervisor)
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .ThenBy(t => t.SalesRep!.Name)
            .ThenBy(t => t.Product!.Code)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<SalesTarget>> GetByRepAndMonthAsync(
        int salesRepId, int year, int month, CancellationToken ct = default)
        => await _context.SalesTargets
            .AsNoTracking()
            .Where(t => t.SalesRepId == salesRepId
                     && t.Year       == year
                     && t.Month      == month
                     && t.IsActive
                     && !t.IsDeleted)
            .Select(t => new SalesTarget
            {
                ProductId      = t.ProductId,
                TargetQuantity = t.TargetQuantity,
                // Price now lives on the product itself (PricingStructures removed).
                Product = t.Product == null ? null : new Product { DealerCasePrice = t.Product.DealerCasePrice }
            })
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
