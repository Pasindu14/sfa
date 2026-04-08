using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;

namespace sfa_api.Infrastructure.Caching;

public class DistributedCacheService(
    IDistributedCache cache,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<DistributedCacheService> _logger = logger;

    // Static so it's shared across all scoped instances in this process.
    private static readonly ConcurrentDictionary<string, bool> _trackedKeys = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes == null) return default;
            return JsonSerializer.Deserialize<T>(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
            _trackedKeys.TryAdd(key, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _trackedKeys.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keys = _trackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in keys)
        {
            try
            {
                await _cache.RemoveAsync(key, ct);
                _trackedKeys.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache prefix remove failed for key {Key}", key);
            }
        }
    }
}
