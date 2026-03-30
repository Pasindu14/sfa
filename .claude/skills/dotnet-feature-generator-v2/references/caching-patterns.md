# Reference: Caching Patterns

Load this reference when the feature needs non-standard caching — custom TTLs, list caching, cache warming, or complex invalidation.

---

## Standard Cache-Aside (Built Into Skill)

The default service template already implements single-entity cache-aside:

```
Read:  cache.GetStringAsync(key) → miss → DB → cache.SetStringAsync(key, dto, TTL)
Write: DB update → cache.RemoveAsync(key)
```

This reference covers **advanced patterns** beyond the default.

---

## TTL Guidelines

| Data Type | TTL | Rationale |
|-----------|-----|-----------|
| Geo hierarchy (Region/Area/Territory) | 1–24 hours | Rarely changes; used in every query |
| Config/settings | 1–24 hours | Changed via admin panel, not frequently |
| Product catalogue / prices | 15–60 minutes | Updated by operations team periodically |
| Rep's outlet list for today | 30–60 minutes | Stable within a day |
| Dashboard counts / aggregates | 5 minutes | Needs near-real-time accuracy |
| Approval status, order status | **NEVER cache** | Must reflect real-time state |
| Financial data (invoices, payments) | **NEVER cache** | Accuracy is critical |

---

## List Cache Pattern

For paginated list endpoints with low write frequency:

```csharp
public async Task<{FeatureName}ListDto> GetAllAsync(
    int page, int pageSize, string? search = null, CancellationToken ct = default)
{
    // Only cache first page with no search — most common request
    string? cacheKey = null;
    if (page == 1 && string.IsNullOrWhiteSpace(search))
    {
        cacheKey = $"{entities}:list:page1:size{pageSize}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<{FeatureName}ListDto>(cached)!;
    }

    var skip = (page - 1) * pageSize;
    var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
    var result = new {FeatureName}ListDto(
        {Entities}: items.Select(MapToDto),
        TotalCount: totalCount,
        Page: page,
        PageSize: pageSize
    );

    if (cacheKey != null)
    {
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, ct);
    }

    return result;
}
```

**Invalidation on write:**

```csharp
// After any create/update/delete — invalidate list cache
await _cache.RemoveAsync($"{entities}:list:page1:size10", ct);
await _cache.RemoveAsync($"{entities}:list:page1:size25", ct);
await _cache.RemoveAsync($"{entities}:list:page1:size50", ct);
```

---

## Cache Warming

For reference data that's expensive to compute and read on every request:

```csharp
// IHostedService — runs on app startup
public class {FeatureName}CacheWarmer(
    IServiceProvider sp,
    ILogger<{FeatureName}CacheWarmer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<I{FeatureName}Repository>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        var (items, _) = await repo.GetAllAsync(0, 1000, null, ct);
        foreach (var item in items)
        {
            var key = $"{entities}:{item.Id}";
            await cache.SetStringAsync(key, JsonSerializer.Serialize(MapToDto(item)),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                }, ct);
        }

        logger.LogInformation("Warmed {Count} {entities} into cache", items.Count());
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

Register: `builder.Services.AddHostedService<{FeatureName}CacheWarmer>();`

---

## Stampede Prevention

When many requests hit the same expired cache key simultaneously:

```csharp
// Use SemaphoreSlim for in-process dedup (works per instance)
private static readonly SemaphoreSlim _cacheLock = new(1, 1);

public async Task<{FeatureName}Dto> GetByIdAsync(int id, CancellationToken ct = default)
{
    var key = $"{entities}:{id}";
    var cached = await _cache.GetStringAsync(key, ct);
    if (cached != null)
        return JsonSerializer.Deserialize<{FeatureName}Dto>(cached)!;

    await _cacheLock.WaitAsync(ct);
    try
    {
        // Double-check after acquiring lock
        cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<{FeatureName}Dto>(cached)!;

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("{FeatureName}", id);

        var dto = MapToDto(entity);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }, ct);
        return dto;
    }
    finally
    {
        _cacheLock.Release();
    }
}
```

---

## Cache Key Conventions

```
{entity}:{id}                          → single entity
{entity}:list:page{N}:size{M}         → paginated list
{entity}:by-parent:{parentId}          → entities by FK
{entity}:count:active                  → aggregate count
{entity}:exists:{uniqueField}:{value}  → existence check
```
