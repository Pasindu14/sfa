using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Distributors.Repositories;

public class DistributorRepository(AppDbContext context) : IDistributorRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Distributor?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Distributors
            .Include(d => d.Territory)
            .Include(d => d.Fleet)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Distributor?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Distributors.AsNoTracking().FirstOrDefaultAsync(d => d.Email == email, ct);

    public async Task<Distributor?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Distributors.AsNoTracking().FirstOrDefaultAsync(d => d.Phone == phone, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.Email == email, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.Phone == phone, ct);

    public async Task<bool> ExistsByEmailAsync(string email, int excludeId, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.Email == email && d.Id != excludeId, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, int excludeId, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.Phone == phone && d.Id != excludeId, ct);

    public async Task<bool> ExistsByTerritoryIdAsync(int territoryId, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.TerritoryId == territoryId && !d.IsDeleted, ct);

    public async Task<bool> ExistsByTerritoryIdAsync(int territoryId, int excludeId, CancellationToken ct = default)
        => await _context.Distributors.AnyAsync(d => d.TerritoryId == territoryId && d.Id != excludeId && !d.IsDeleted, ct);

    public async Task<Distributor?> GetByTerritoryIdAsync(int territoryId, CancellationToken ct = default)
        => await _context.Distributors
            .AsNoTracking()
            .Include(d => d.Fleet)
            .FirstOrDefaultAsync(d => d.TerritoryId == territoryId && d.IsActive && !d.IsDeleted, ct);

    public async Task<(IEnumerable<Distributor> Distributors, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, bool? isActive = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Distributors.Where(d => !d.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(d => EF.Functions.ILike(d.Name, pattern) || EF.Functions.ILike(d.Email, pattern) || EF.Functions.ILike(d.Phone, pattern))
                : query.Where(d => EF.Functions.Like(d.Name, pattern) || EF.Functions.Like(d.Email, pattern) || EF.Functions.Like(d.Phone, pattern));
        }
        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);
        var distributors = await query
            .AsNoTracking()
            .Include(d => d.Territory)
            .Include(d => d.Fleet)
            .OrderBy(d => d.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (distributors, totalCount);
    }

    public async Task CreateAsync(Distributor distributor, CancellationToken ct = default)
        => await _context.Distributors.AddAsync(distributor, ct);

    public Task UpdateAsync(Distributor distributor, CancellationToken ct = default)
    {
        _context.Distributors.Update(distributor);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.Distributors
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.IsActive, false)
                .SetProperty(d => d.IsDeleted, true)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
