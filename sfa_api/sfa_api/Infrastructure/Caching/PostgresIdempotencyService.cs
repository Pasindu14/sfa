using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Audit;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Infrastructure.Caching;

public class PostgresIdempotencyService(AppDbContext db,
    ILogger<PostgresIdempotencyService> logger) : IIdempotencyService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<PostgresIdempotencyService> _logger = logger;

    public async Task<IdempotencyResult?> GetAsync(string key, CancellationToken ct = default)
    {
        var record = await _db.IdempotencyKeys
            .Where(x => x.Key == key && x.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (record is null) return null;

        _logger.LogInformation(
            "Idempotency key {Key} found — returning cached response", key);

        return new IdempotencyResult(record.StatusCode, record.ResponseJson);
    }

    public async Task StoreAsync(string key, int statusCode, string responseJson,
        CancellationToken ct = default)
    {
        // A row for this key may already exist — a prior completed request still within TTL, or
        // a concurrent store. Storing again is harmless (the response is already cached), so we
        // must never let a primary-key violation bubble up as a 500 for a request that succeeded.
        if (await _db.IdempotencyKeys.AnyAsync(x => x.Key == key, ct))
            return;

        var entity = new IdempotencyKey
        {
            Key = key,
            StatusCode = statusCode,
            ResponseJson = responseJson,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        _db.IdempotencyKeys.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a concurrent insert race for the same key — the winner already cached an
            // equivalent response, so treat this as success and drop our duplicate insert.
            _db.Entry(entity).State = EntityState.Detached;
            _logger.LogInformation(
                "Idempotency key {Key} already stored by a concurrent request — skipping duplicate insert", key);
        }
    }
}
