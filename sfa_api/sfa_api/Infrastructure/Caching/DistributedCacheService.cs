using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace sfa_api.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> over <see cref="IDistributedCache"/> (Redis, or the in-process
/// fallback) with prefix invalidation implemented as <b>generation counters stored in the cache
/// itself</b> — no process-local key registry, so it works across instances and nothing grows
/// unbounded.
/// <para>
/// A logical key such as <c>sales-summary:Rep:123:...</c> belongs to the namespaces formed by its
/// first <see cref="MaxNamespaceDepth"/> colon-separated segments (<c>sales-summary</c> and
/// <c>sales-summary:Rep</c>). Each namespace has a generation token at <c>cachever:{namespace}</c>, and
/// the value is physically stored under <c>{key}#{gen1}.{gen2}</c>.
/// <see cref="RemoveByPrefixAsync"/> just writes a fresh random token for the prefix's namespace: every
/// physical key built from the old token becomes unreachable and ages out on its own TTL.
/// </para>
/// <para>
/// Prefixes must be segment-aligned (<c>"outlets:route:"</c>, <c>"areas:active"</c>); a trailing
/// colon is optional. A prefix deeper than <see cref="MaxNamespaceDepth"/> segments invalidates its
/// depth-limited ancestor namespace instead — over-invalidation, never under-invalidation.
/// </para>
/// </summary>
public class DistributedCacheService(
    IDistributedCache cache,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    /// <summary>How many leading key segments participate in prefix invalidation.</summary>
    public const int MaxNamespaceDepth = 2;

    private const string GenerationKeyPrefix = "cachever:";

    /// <summary>
    /// Generation tokens outlive every data TTL in the app (minutes to hours). If one does expire or
    /// is evicted, a brand-new random token is minted — never a previous value — so old entries can't
    /// resurface; the cost is one round of cache misses.
    /// </summary>
    private static readonly TimeSpan GenerationTtl = TimeSpan.FromDays(30);

    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<DistributedCacheService> _logger = logger;

    // Per-scope (i.e. per-request) memo of generation tokens: a Get-miss followed by a Set in the same
    // request uses the same generation, so a value computed before a concurrent invalidation lands
    // under the now-dead generation instead of poisoning the new one.
    private readonly ConcurrentDictionary<string, string> _generations = new(StringComparer.Ordinal);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var physicalKey = await ResolvePhysicalKeyAsync(key, ct);
            var bytes = await _cache.GetAsync(physicalKey, ct);
            if (bytes == null) return default;
            return JsonSerializer.Deserialize<T>(bytes);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
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
            var physicalKey = await ResolvePhysicalKeyAsync(key, ct);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(physicalKey, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
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
            var physicalKey = await ResolvePhysicalKeyAsync(key, ct);
            await _cache.RemoveAsync(physicalKey, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var ns = PrefixNamespace(prefix);
        if (ns.Length == 0)
        {
            _logger.LogWarning("Ignoring cache prefix invalidation with an empty prefix");
            return;
        }

        var token = NewGenerationToken();
        try
        {
            await _cache.SetAsync(GenerationKeyPrefix + ns, Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GenerationTtl }, ct);
            _generations[ns] = token;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Can't bump the shared generation — at least stop this scope from serving the old one.
            _generations.TryRemove(ns, out _);
            _logger.LogWarning(ex, "Cache prefix invalidation failed for prefix {Prefix}", prefix);
        }
    }

    // ── Key mapping ─────────────────────────────────────────────────────────

    /// <summary>Namespaces a logical key belongs to: its first 1..MaxNamespaceDepth segments.</summary>
    public static IReadOnlyList<string> KeyNamespaces(string key)
    {
        var namespaces = new List<string>(MaxNamespaceDepth);
        var searchFrom = 0;
        for (var depth = 1; depth <= MaxNamespaceDepth; depth++)
        {
            var colon = key.IndexOf(':', searchFrom);
            if (colon < 0)
            {
                namespaces.Add(key);   // the whole key is the last (shallower) namespace
                break;
            }
            namespaces.Add(key[..colon]);
            searchFrom = colon + 1;
        }
        return namespaces;
    }

    /// <summary>The namespace a prefix invalidates (trailing colon dropped, depth-capped).</summary>
    public static string PrefixNamespace(string prefix)
    {
        var trimmed = prefix.TrimEnd(':');
        var searchFrom = 0;
        for (var depth = 1; depth <= MaxNamespaceDepth; depth++)
        {
            var colon = trimmed.IndexOf(':', searchFrom);
            if (colon < 0) return trimmed;
            if (depth == MaxNamespaceDepth) return trimmed[..colon];
            searchFrom = colon + 1;
        }
        return trimmed;
    }

    private async Task<string> ResolvePhysicalKeyAsync(string key, CancellationToken ct)
    {
        var namespaces = KeyNamespaces(key);
        var tokens = namespaces.Count == 1
            ? [await GetGenerationAsync(namespaces[0], ct)]
            : await Task.WhenAll(namespaces.Select(ns => GetGenerationAsync(ns, ct)));
        return $"{key}#{string.Join('.', tokens)}";
    }

    private async Task<string> GetGenerationAsync(string ns, CancellationToken ct)
    {
        if (_generations.TryGetValue(ns, out var memo)) return memo;

        var bytes = await _cache.GetAsync(GenerationKeyPrefix + ns, ct);
        string token;
        if (bytes is { Length: > 0 })
        {
            token = Encoding.UTF8.GetString(bytes);
        }
        else
        {
            // First use (or the token expired): mint one. Two instances racing here just write
            // different tokens; the loser's entries become unreachable — a cache miss, never stale data.
            token = NewGenerationToken();
            await _cache.SetAsync(GenerationKeyPrefix + ns, Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GenerationTtl }, ct);
        }

        return _generations.GetOrAdd(ns, token);
    }

    private static string NewGenerationToken() => Guid.NewGuid().ToString("N")[..12];
}
