using Microsoft.EntityFrameworkCore;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.UserProximityExemptions.Repositories;

public class UserProximityExemptionRepository(AppDbContext context) : IUserProximityExemptionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<UserProximityExemption?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.UserProximityExemptions
            .Include(x => x.User)
            .Include(x => x.GrantedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<UserProximityExemption?> GetEffectiveAsync(
        int userId, DateTime atUtc, CancellationToken ct = default)
        => await _context.UserProximityExemptions
            .AsNoTracking()
            .Where(x => x.UserId == userId
                     && x.IsActive
                     && x.ValidFrom <= atUtc
                     && x.ValidTo > atUtc)
            .OrderByDescending(x => x.ValidTo)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<UserProximityExemption>> GetHistoryByUserIdAsync(
        int userId, CancellationToken ct = default)
        => await _context.UserProximityExemptions
            .AsNoTracking()
            .Include(x => x.GrantedByUser)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ValidFrom)
            .ToListAsync(ct);

    public async Task<(IEnumerable<UserProximityExemption> Items, int TotalCount)> GetActiveAsync(
        int skip, int take, DateTime atUtc, string? search = null, CancellationToken ct = default)
    {
        var query = _context.UserProximityExemptions
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.GrantedByUser)
            .Where(x => x.IsActive && x.ValidFrom <= atUtc && x.ValidTo > atUtc);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.User != null
                && (EF.Functions.ILike(x.User.Name, $"%{term}%")
                 || EF.Functions.ILike(x.User.Username, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.ValidTo)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<User?> GetUserAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task AddAsync(UserProximityExemption entity, CancellationToken ct = default)
        => await _context.UserProximityExemptions.AddAsync(entity, ct);

    public void ApplyConcurrencyToken(UserProximityExemption entity, uint rowVersion)
        => _context.Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new Common.Errors.ConcurrencyConflictException();
        }
    }
}
