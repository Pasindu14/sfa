using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Infrastructure.Persistence;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Repositories;

public class RouteRepository(AppDbContext context) : IRouteRepository
{
    private readonly AppDbContext _context = context;

    public async Task<RouteEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Routes
            .IgnoreQueryFilters()
            .Include(r => r.Division)
                .ThenInclude(d => d!.Territory)
                    .ThenInclude(t => t!.Area)
                        .ThenInclude(a => a!.Region)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(IEnumerable<RouteEntity> Routes, int TotalCount)> GetAllAsync(
        int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _context.Routes.IgnoreQueryFilters().Where(x => !x.IsDeleted).AsQueryable();

        if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = _context.Database.ProviderName?.Contains("Npgsql") == true
                ? query.Where(r => EF.Functions.ILike(r.Name, pattern))
                : query.Where(r => EF.Functions.Like(r.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.Division)
                .ThenInclude(d => d!.Territory)
                    .ThenInclude(t => t!.Area)
                        .ThenInclude(a => a!.Region)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<RouteEntity>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Routes
            .Where(r => r.IsActive)
            .Include(r => r.Division)
                .ThenInclude(d => d!.Territory)
                    .ThenInclude(t => t!.Area)
                        .ThenInclude(a => a!.Region)
            .Include(r => r.Territory)
            .Include(r => r.Area)
            .Include(r => r.Region)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<IEnumerable<RouteEntity>> GetActiveByDivisionIdAsync(int divisionId, CancellationToken ct = default)
        => await _context.Routes
            .Where(r => r.IsActive && r.DivisionId == divisionId)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default)
        => await _context.Divisions
            .IgnoreQueryFilters()
            .Include(d => d.Territory)
                .ThenInclude(t => t!.Area)
                    .ThenInclude(a => a!.Region)
            .Include(d => d.Area)
            .Include(d => d.Region)
            .FirstOrDefaultAsync(d => d.Id == divisionId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int divisionId, CancellationToken ct = default)
        => await _context.Routes.IgnoreQueryFilters().AnyAsync(r => r.Name == name && r.DivisionId == divisionId, ct);

    public async Task<bool> ExistsByNameAsync(string name, int divisionId, int excludeId, CancellationToken ct = default)
        => await _context.Routes.IgnoreQueryFilters().AnyAsync(r => r.Name == name && r.DivisionId == divisionId && r.Id != excludeId, ct);

    public async Task<HashSet<string>> GetUsedPinColorsAsync(int? excludeRouteId = null, CancellationToken ct = default)
    {
        var query = _context.Routes
            .IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.PinColor != "");

        if (excludeRouteId.HasValue)
            query = query.Where(r => r.Id != excludeRouteId.Value);

        var colors = await query.Select(r => r.PinColor).ToListAsync(ct);
        return new HashSet<string>(colors, StringComparer.OrdinalIgnoreCase);
    }

    public async Task CreateAsync(RouteEntity route, CancellationToken ct = default)
        => await _context.Routes.AddAsync(route, ct);

    public Task UpdateAsync(RouteEntity route, CancellationToken ct = default)
    {
        _context.Routes.Update(route);
        return Task.CompletedTask;
    }

    public async Task<bool> HasActiveOutletsAsync(int routeId, CancellationToken ct = default)
        => await _context.Outlets
            .IgnoreQueryFilters()
            .AnyAsync(o => o.RouteId == routeId && o.IsActive && !o.IsDeleted, ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(ct);

    public IExecutionStrategy CreateExecutionStrategy()
        => _context.Database.CreateExecutionStrategy();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
