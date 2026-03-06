using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Infrastructure.Caching;

public class PostgresTokenRevocationService(AppDbContext db) : ITokenRevocationService
{
    private readonly AppDbContext _db = db;

    public async Task RevokeAsync(string jti, DateTime tokenExpiry,
        CancellationToken ct = default)
    {
        _db.RevokedTokens.Add(new RevokedToken { Jti = jti, ExpiresAt = tokenExpiry });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
        => await _db.RevokedTokens
            .AnyAsync(x => x.Jti == jti && x.ExpiresAt > DateTime.UtcNow, ct);
}
