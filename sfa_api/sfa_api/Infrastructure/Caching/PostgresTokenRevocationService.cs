using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using sfa_api.Common.Audit;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Infrastructure.Caching;

/// <summary>
/// Access-token denylist. Postgres (<c>RevokedTokens</c>) stays the source of truth; the per-request
/// check in <c>JwtExtensions.OnTokenValidated</c> is served from cache so an authenticated request no
/// longer costs a database round-trip.
/// <para>
/// Read path, in order:
/// <list type="number">
/// <item>Process-local "revoked" marker — set by <see cref="RevokeAsync"/> on this instance and by any
///   positive lookup, so a token revoked here is rejected on the very next request, even if the
///   shared cache is down.</item>
/// <item>Shared cache (Redis) — when <c>REDIS_CONNECTION</c> is configured, every revoke writes
///   <c>revoked-jti:{jti}</c> with a TTL equal to the token's remaining lifetime, so a miss means
///   "not revoked" across all instances.</item>
/// <item>Postgres fallback — used when there is no shared cache (the in-process
///   <see cref="MemoryDistributedCache"/> fallback cannot see other instances' revokes) or when the
///   shared cache throws. A "not revoked" answer is memoised in-process for
///   <see cref="NotRevokedMemoTtl"/>, which bounds DB load and bounds cross-instance staleness.</item>
/// </list>
/// </para>
/// </summary>
public class PostgresTokenRevocationService(
    AppDbContext db,
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    ILogger<PostgresTokenRevocationService> logger) : ITokenRevocationService
{
    public static readonly TimeSpan NotRevokedMemoTtl = TimeSpan.FromSeconds(30);

    private static readonly byte[] RevokedMarker = [1];

    private readonly AppDbContext _db = db;
    private readonly IDistributedCache _distributedCache = distributedCache;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<PostgresTokenRevocationService> _logger = logger;

    /// <summary>
    /// True when the registered <see cref="IDistributedCache"/> is shared across instances. The
    /// in-process fallback (<see cref="MemoryDistributedCache"/>) is not, so its misses prove nothing.
    /// </summary>
    private bool HasSharedCache => _distributedCache is not MemoryDistributedCache;

    public static string DistributedKey(string jti) => $"revoked-jti:{jti}";
    private static string LocalRevokedKey(string jti) => $"revoked-jti:local:{jti}";
    private static string LocalNotRevokedKey(string jti) => $"revoked-jti:local-not:{jti}";

    public async Task RevokeAsync(string jti, DateTime tokenExpiry, CancellationToken ct = default)
    {
        // 1. Source of truth.
        _db.RevokedTokens.Add(new RevokedToken { Jti = jti, ExpiresAt = tokenExpiry });
        await _db.SaveChangesAsync(ct);

        // JWT ValidTo is UTC; treat an Unspecified kind as UTC too rather than as server-local time.
        var expiryUtc = tokenExpiry.Kind == DateTimeKind.Local
            ? tokenExpiry.ToUniversalTime()
            : DateTime.SpecifyKind(tokenExpiry, DateTimeKind.Utc);
        var remaining = expiryUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return;   // already expired — lifetime validation rejects it anyway

        // 2. Process-local marker — guarantees this instance never accepts the token again,
        //    independent of the shared cache's health.
        _memoryCache.Remove(LocalNotRevokedKey(jti));
        _memoryCache.Set(LocalRevokedKey(jti), true, remaining);

        // 3. Shared cache for the other instances. A failure here is logged, not thrown: the row is
        //    committed, and instances that fall back to Postgres will still see it.
        try
        {
            await _distributedCache.SetAsync(DistributedKey(jti), RevokedMarker,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write token revocation to the distributed cache for jti {Jti}", jti);
        }
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(LocalRevokedKey(jti), out _))
            return true;

        if (HasSharedCache)
        {
            try
            {
                var hit = await _distributedCache.GetAsync(DistributedKey(jti), ct);
                if (hit is null) return false;

                // Remember positives locally so a later cache outage can't un-revoke the token here.
                // The exact expiry isn't known from the marker; the shared entry's own TTL already
                // matched the token lifetime, and lifetime validation rejects it after that.
                _memoryCache.Set(LocalRevokedKey(jti), true, TimeSpan.FromHours(1));
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Distributed cache unavailable for token revocation check; falling back to Postgres");
            }
        }

        return await IsRevokedInDatabaseAsync(jti, ct);
    }

    private async Task<bool> IsRevokedInDatabaseAsync(string jti, CancellationToken ct)
    {
        if (_memoryCache.TryGetValue(LocalNotRevokedKey(jti), out _))
            return false;

        var expiresAt = await _db.RevokedTokens
            .AsNoTracking()
            .Where(x => x.Jti == jti && x.ExpiresAt > DateTime.UtcNow)
            .Select(x => (DateTime?)x.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (expiresAt is { } exp)
        {
            var remaining = DateTime.SpecifyKind(exp, DateTimeKind.Utc) - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
                _memoryCache.Set(LocalRevokedKey(jti), true, remaining);
            return true;
        }

        _memoryCache.Set(LocalNotRevokedKey(jti), true, NotRevokedMemoTtl);
        return false;
    }
}
