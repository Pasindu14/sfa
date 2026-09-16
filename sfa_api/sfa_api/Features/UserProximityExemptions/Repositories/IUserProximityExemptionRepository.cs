using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserProximityExemptions.Repositories;

public interface IUserProximityExemptionRepository
{
    Task<UserProximityExemption?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// The live grant covering <paramref name="atUtc"/> for this rep, or null.
    /// "Live" means not revoked, not soft-deleted, and inside [ValidFrom, ValidTo).
    /// Newest ValidTo wins if grants overlap, so a renewal extends rather than shortens.
    /// </summary>
    Task<UserProximityExemption?> GetEffectiveAsync(int userId, DateTime atUtc, CancellationToken ct = default);

    /// <summary>Grant history for one rep, newest first — drives the admin panel.</summary>
    Task<IEnumerable<UserProximityExemption>> GetHistoryByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Every rep currently exempt — the audit list an admin reviews.</summary>
    Task<(IEnumerable<UserProximityExemption> Items, int TotalCount)> GetActiveAsync(
        int skip, int take, DateTime atUtc, string? search = null, CancellationToken ct = default);

    Task<User?> GetUserAsync(int userId, CancellationToken ct = default);

    Task AddAsync(UserProximityExemption entity, CancellationToken ct = default);
    void ApplyConcurrencyToken(UserProximityExemption entity, uint rowVersion);
    Task SaveChangesAsync(CancellationToken ct = default);
}
