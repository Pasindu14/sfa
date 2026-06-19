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
}
