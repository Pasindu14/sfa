using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Infrastructure.Caching;

public class DistributedCacheServiceTests
{
    // One backing store shared by every service instance = one Redis shared by every scope/instance.
    private readonly MemoryDistributedCache _store = new(Options.Create(new MemoryDistributedCacheOptions()));

    private DistributedCacheService NewScope() => new(_store, NullLogger<DistributedCacheService>.Instance);

    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task SetThenGet_RoundTrips()
    {
        await NewScope().SetAsync("rep-sales:1:2026:9", 42m, Ttl);

        (await NewScope().GetAsync<decimal?>("rep-sales:1:2026:9")).Should().Be(42m);
    }

    [Fact]
    public async Task RemoveByPrefix_InvalidatesKeysUnderPrefix_ForEveryOtherScope()
    {
        await NewScope().SetAsync("outlets:route:v2:1", "route-1", Ttl);
        await NewScope().SetAsync("outlets:route:v2:2", "route-2", Ttl);
        await NewScope().SetAsync("outlets:other:1", "other", Ttl);

        // Invalidation happens in a different scope (another request / another instance) - nothing
        // process-local is needed for it to take effect.
        await NewScope().RemoveByPrefixAsync("outlets:route:");

        var reader = NewScope();
        (await reader.GetAsync<string>("outlets:route:v2:1")).Should().BeNull();
        (await reader.GetAsync<string>("outlets:route:v2:2")).Should().BeNull();
        (await reader.GetAsync<string>("outlets:other:1")).Should().Be("other");
    }

    [Fact]
    public async Task RemoveByPrefix_SingleSegmentPrefix_InvalidatesDeepKeys()
    {
        const string key = "sales-summary:Rep:100:130:::::::::::::";
        await NewScope().SetAsync(key, "report", Ttl);
        await NewScope().SetAsync("sales-summary-other:1", "keep", Ttl);

        await NewScope().RemoveByPrefixAsync("sales-summary:");

        (await NewScope().GetAsync<string>(key)).Should().BeNull();
        (await NewScope().GetAsync<string>("sales-summary-other:1")).Should().Be("keep");
    }

    [Fact]
    public async Task RemoveByPrefix_PrefixEqualToWholeKey_InvalidatesKeyAndChildren()
    {
        await NewScope().SetAsync("areas:active", "all", Ttl);
        await NewScope().SetAsync("areas:active:5", "region-5", Ttl);
        await NewScope().SetAsync("areas:list:1:20", "list", Ttl);

        await NewScope().RemoveByPrefixAsync("areas:active");

        (await NewScope().GetAsync<string>("areas:active")).Should().BeNull();
        (await NewScope().GetAsync<string>("areas:active:5")).Should().BeNull();
        (await NewScope().GetAsync<string>("areas:list:1:20")).Should().Be("list");
    }

    [Fact]
    public async Task ValuesSetAfterInvalidation_AreVisible()
    {
        await NewScope().SetAsync("products:list:1", "old", Ttl);
        await NewScope().RemoveByPrefixAsync("products:list:");
        await NewScope().SetAsync("products:list:1", "new", Ttl);

        (await NewScope().GetAsync<string>("products:list:1")).Should().Be("new");
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyThatKey()
    {
        await NewScope().SetAsync("rep-sales:1:2026:9", 1, Ttl);
        await NewScope().SetAsync("rep-sales:2:2026:9", 2, Ttl);

        await NewScope().RemoveAsync("rep-sales:1:2026:9");

        (await NewScope().GetAsync<int?>("rep-sales:1:2026:9")).Should().BeNull();
        (await NewScope().GetAsync<int?>("rep-sales:2:2026:9")).Should().Be(2);
    }

    [Fact]
    public async Task StaleValueComputedBeforeConcurrentInvalidation_DoesNotSurviveIt()
    {
        await NewScope().SetAsync("fleets:list:1", "v1", Ttl);

        // Request A reads under the current generation and starts computing from the DB...
        var requestA = NewScope();
        await requestA.GetAsync<string>("fleets:list:1");

        // ...meanwhile request B writes and invalidates...
        await NewScope().RemoveByPrefixAsync("fleets:list:");

        // ...then A stores what it computed before B's write. It lands under the dead generation.
        await requestA.SetAsync("fleets:list:1", "stale", Ttl);

        (await NewScope().GetAsync<string>("fleets:list:1")).Should().BeNull();
    }

    [Theory]
    [InlineData("outlets:route:v2:5", new[] { "outlets", "outlets:route" })]
    [InlineData("areas:active", new[] { "areas", "areas:active" })]
    [InlineData("mobile", new[] { "mobile" })]
    public void KeyNamespaces_AreLeadingSegments(string key, string[] expected)
        => DistributedCacheService.KeyNamespaces(key).Should().Equal(expected);

    [Theory]
    [InlineData("sales-summary:", "sales-summary")]
    [InlineData("outlets:route:", "outlets:route")]
    [InlineData("areas:active", "areas:active")]
    [InlineData("a:b:c:", "a:b")]   // deeper than MaxNamespaceDepth -> ancestor (over-invalidates, never under)
    public void PrefixNamespace_IsTrimmedAndDepthCapped(string prefix, string expected)
        => DistributedCacheService.PrefixNamespace(prefix).Should().Be(expected);
}
