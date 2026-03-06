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
        _db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            StatusCode = statusCode,
            ResponseJson = responseJson,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });

        await _db.SaveChangesAsync(ct);
    }
}
