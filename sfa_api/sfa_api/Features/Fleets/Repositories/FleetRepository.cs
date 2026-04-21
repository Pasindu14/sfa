using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Fleets.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Fleets.Repositories;

public class FleetRepository(AppDbContext context) : IFleetRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Fleet?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Fleets.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<(IEnumerable<Fleet> Fleets, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Fleets.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(f => EF.Functions.ILike(f.Name, pattern))
                : query.Where(f => EF.Functions.Like(f.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var fleets = await query
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (fleets, totalCount);
    }

    public async Task<IEnumerable<Fleet>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Fleets
            .AsNoTracking()
            .Where(f => f.IsActive && !f.IsDeleted)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct = default)
        => await _context.Fleets.IgnoreQueryFilters().AnyAsync(f => f.Id == id && f.IsActive && !f.IsDeleted, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Fleets.IgnoreQueryFilters().AnyAsync(f => f.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
        => await _context.Fleets.IgnoreQueryFilters().AnyAsync(f => f.Name == name && f.Id != excludeId, ct);

    public async Task CreateAsync(Fleet fleet, CancellationToken ct = default)
        => await _context.Fleets.AddAsync(fleet, ct);

    public Task UpdateAsync(Fleet fleet, CancellationToken ct = default)
    {
        _context.Fleets.Update(fleet);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
