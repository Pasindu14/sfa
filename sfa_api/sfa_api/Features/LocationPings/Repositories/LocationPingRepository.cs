using Microsoft.EntityFrameworkCore;
using sfa_api.Features.LocationPings.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.LocationPings.Repositories;

public class LocationPingRepository(AppDbContext db) : ILocationPingRepository
{
    public async Task BulkInsertAsync(IEnumerable<RepLocationPing> pings, CancellationToken ct = default)
    {
        db.RepLocationPings.AddRange(pings);
        await db.SaveChangesAsync(ct);
    }

    /// Returns the most-recent ping for every rep that has sent at least one ping.
    /// Uses PostgreSQL DISTINCT ON so only one row per rep is returned,
    /// ordered by RecordedAt descending within each rep group.
    public async Task<IReadOnlyList<RepLocationPing>> GetLatestPerRepAsync(CancellationToken ct = default)
    {
        return await db.RepLocationPings
            .FromSqlRaw("""
                SELECT DISTINCT ON ("RepId")
                    "Id", "RepId", "Latitude", "Longitude", "Accuracy",
                    "RecordedAt", "ReceivedAt"
                FROM "RepLocationPings"
                ORDER BY "RepId", "RecordedAt" DESC
                """)
            .Include(p => p.Rep)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// Plain LINQ — unlike GetLatestPerRepAsync this needs no DISTINCT ON, and the
    /// (RepId, RecordedAt) composite index covers both the filter and the ordering.
    /// No Include(p => p.Rep): the caller already resolves the rep, and joining the
    /// user row onto every one of ~288 daily pings would be pure repetition.
    public async Task<IReadOnlyList<RepLocationPing>> GetForRepBetweenAsync(
        int repId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken ct = default)
    {
        return await db.RepLocationPings
            .Where(p => p.RepId == repId
                     && p.RecordedAt >= fromInclusive
                     && p.RecordedAt < toExclusive)
            .OrderBy(p => p.RecordedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
