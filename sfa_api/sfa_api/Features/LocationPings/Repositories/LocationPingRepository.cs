using Microsoft.EntityFrameworkCore;
using sfa_api.Features.LocationPings.DTOs;
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

    /// Returns the most-recent ping for every rep, optionally bounded to pings recorded at or
    /// after <paramref name="sinceUtc"/>. Uses PostgreSQL DISTINCT ON so only one row per rep is
    /// returned, ordered by RecordedAt descending within each rep group — served by the
    /// (RepId, RecordedAt DESC) index.
    ///
    /// The rep name is projected via an inner join rather than Include(p => p.Rep): only one
    /// column is needed, and the inner join keeps the previous semantics exactly (RepId is a
    /// required FK, so Include was already an INNER JOIN honouring User's !IsDeleted filter —
    /// pings of soft-deleted users stay hidden).
    public async Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(
        DateTimeOffset? sinceUtc, CancellationToken ct = default)
    {
        IQueryable<RepLocationPing> latest;
        if (sinceUtc is { } since)
        {
            // timestamptz parameters must be UTC for Npgsql.
            var sinceParam = since.ToUniversalTime();
            latest = db.RepLocationPings.FromSqlInterpolated($"""
                SELECT DISTINCT ON ("RepId")
                    "Id", "RepId", "Latitude", "Longitude", "Accuracy",
                    "RecordedAt", "ReceivedAt"
                FROM "RepLocationPings"
                WHERE "RecordedAt" >= {sinceParam}
                ORDER BY "RepId", "RecordedAt" DESC
                """);
        }
        else
        {
            // Unbounded "last-ever ping per rep": one (RepId, RecordedAt DESC) index probe per
            // user via LATERAL, so cost scales with user count rather than ping history.
            latest = db.RepLocationPings.FromSqlRaw("""
                SELECT lp."Id", lp."RepId", lp."Latitude", lp."Longitude", lp."Accuracy",
                       lp."RecordedAt", lp."ReceivedAt"
                FROM "Users" u
                CROSS JOIN LATERAL (
                    SELECT rp."Id", rp."RepId", rp."Latitude", rp."Longitude", rp."Accuracy",
                           rp."RecordedAt", rp."ReceivedAt"
                    FROM "RepLocationPings" rp
                    WHERE rp."RepId" = u."Id"
                    ORDER BY rp."RecordedAt" DESC
                    LIMIT 1
                ) lp
                """);
        }

        return await latest
            .AsNoTracking()
            .Join(db.Users, p => p.RepId, u => u.Id, (p, u) => new RepLocationPingDto(
                p.RepId,
                u.Name,
                p.Latitude,
                p.Longitude,
                p.Accuracy,
                p.RecordedAt,
                p.ReceivedAt))
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
