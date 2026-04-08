using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace sfa_api.Infrastructure.Caching;

public class MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<MemoryCacheService> _logger = logger;

    private static readonly ConcurrentDictionary<string, bool> _trackedKeys = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
        _trackedKeys.TryAdd(key, true);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keys = _trackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in keys)
        {
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
