using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.GeoConsistency.Services;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.IntegrationTests.Features.GeoConsistency;

/// <summary>
/// Exercises the geo re-parent cascade + reconciliation against the live SQLite schema. The cascade
/// itself uses constant-value ExecuteUpdate (SQLite-safe); drift detection is read-only counts. Seeds
/// the full live chain (Region → Area → Territory → Division → Route → Outlet, + Distributor) directly
/// via AppDbContext, then verifies moving the Area's region propagates to every descendant.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class GeoCascadeApiTests
{
    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GeoCascadeApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.AdminToken);
    }

    private record Chain(int RegionAId, int RegionBId, int AreaId, int TerritoryId, int DivisionId, int RouteId, int OutletId, int DistributorId);

    /// <summary>Seeds two regions and a full live chain anchored to region A, all denormalized to A.</summary>
    private async Task<Chain> SeedChainAsync()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var regionA = new Region { Name = $"RegA-{tag}" };
        var regionB = new Region { Name = $"RegB-{tag}" };
        db.Set<Region>().AddRange(regionA, regionB);
        await db.SaveChangesAsync();

        var area = new Area { Name = $"Area-{tag}", RegionId = regionA.Id };
        db.Set<Area>().Add(area);
        await db.SaveChangesAsync();

        var terr = new Territory { Name = $"Terr-{tag}", AreaId = area.Id, RegionId = regionA.Id };
        db.Set<Territory>().Add(terr);
        await db.SaveChangesAsync();

        var div = new Division { Name = $"Div-{tag}", TerritoryId = terr.Id, AreaId = area.Id, RegionId = regionA.Id };
        db.Set<Division>().Add(div);
        await db.SaveChangesAsync();

        var route = new RouteEntity
        {
            Name = $"Route-{tag}", PinColor = $"#{tag[..6]}",
            DivisionId = div.Id, TerritoryId = terr.Id, AreaId = area.Id, RegionId = regionA.Id
        };
        db.Set<RouteEntity>().Add(route);
        await db.SaveChangesAsync();

        var outlet = new Outlet
        {
            Name = $"Outlet-{tag}", Address = "addr", Tel = "011", NicNo = $"NIC-{tag}",
            Latitude = 6.9, Longitude = 79.8,
            RouteId = route.Id, DivisionId = div.Id, TerritoryId = terr.Id, AreaId = area.Id, RegionId = regionA.Id
        };
        db.Set<Outlet>().Add(outlet);

        var dist = new Distributor
        {
            Name = $"Dist-{tag}", Address = "addr", Phone = $"P{tag}", Email = $"{tag}@d.com",
            Alias = 1, Category = "A",
            TerritoryId = terr.Id, AreaId = area.Id, RegionId = regionA.Id
        };
        db.Set<Distributor>().Add(dist);
        await db.SaveChangesAsync();

        return new Chain(regionA.Id, regionB.Id, area.Id, terr.Id, div.Id, route.Id, outlet.Id, dist.Id);
    }

    [Fact]
    public async Task CascadeAreaRegionChange_UpdatesRegionIdOnEveryLiveDescendant()
    {
        var c = await SeedChainAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var cascade = scope.ServiceProvider.GetRequiredService<IGeoCascadeService>();
            var affected = await cascade.CascadeAreaRegionChangeAsync(c.AreaId, c.RegionBId);
            affected.Should().Be(5, "one territory + division + route + outlet + distributor");
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Set<Territory>().AsNoTracking().FirstAsync(x => x.Id == c.TerritoryId)).RegionId.Should().Be(c.RegionBId);
            (await db.Set<Division>().AsNoTracking().FirstAsync(x => x.Id == c.DivisionId)).RegionId.Should().Be(c.RegionBId);
            (await db.Set<RouteEntity>().AsNoTracking().FirstAsync(x => x.Id == c.RouteId)).RegionId.Should().Be(c.RegionBId);
            (await db.Set<Outlet>().AsNoTracking().FirstAsync(x => x.Id == c.OutletId)).RegionId.Should().Be(c.RegionBId);
            (await db.Set<Distributor>().AsNoTracking().FirstAsync(x => x.Id == c.DistributorId)).RegionId.Should().Be(c.RegionBId);
        }
    }

    [Fact]
    public async Task CascadeTerritoryAreaChange_UpdatesAreaAndRegionOnLiveDescendants()
    {
        var c = await SeedChainAsync();

        // Create a move target: a new area under region B.
        int newAreaId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var newArea = new Area { Name = $"AreaB-{Guid.NewGuid():N}"[..20], RegionId = c.RegionBId };
            db.Set<Area>().Add(newArea);
            await db.SaveChangesAsync();
            newAreaId = newArea.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var cascade = scope.ServiceProvider.GetRequiredService<IGeoCascadeService>();
            // Territory moved to the new area (region B) → divisions, routes, outlets, distributors follow.
            var affected = await cascade.CascadeTerritoryAreaChangeAsync(c.TerritoryId, newAreaId, c.RegionBId);
            affected.Should().Be(4, "division + route + outlet + distributor");
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var div = await db.Set<Division>().AsNoTracking().FirstAsync(x => x.Id == c.DivisionId);
            div.AreaId.Should().Be(newAreaId);
            div.RegionId.Should().Be(c.RegionBId);
            var outlet = await db.Set<Outlet>().AsNoTracking().FirstAsync(x => x.Id == c.OutletId);
            outlet.AreaId.Should().Be(newAreaId);
            outlet.RegionId.Should().Be(c.RegionBId);
            var dist = await db.Set<Distributor>().AsNoTracking().FirstAsync(x => x.Id == c.DistributorId);
            dist.AreaId.Should().Be(newAreaId);
            dist.RegionId.Should().Be(c.RegionBId);
        }
    }

    [Fact]
    public async Task UpdateArea_ChangingRegion_PropagatesThroughHttpPipeline()
    {
        var c = await SeedChainAsync();

        // Fetch the seeded area to obtain its current rowVersion for the optimistic-concurrency PUT.
        var getArea = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/areas/{c.AreaId}", _jsonOpts);
        var rowVersion = getArea.GetProperty("data").GetProperty("rowVersion").GetUInt32();

        var put = await _client.PutAsJsonAsync($"/api/v1/areas/{c.AreaId}",
            new { name = getArea.GetProperty("data").GetProperty("name").GetString(), regionId = c.RegionBId, rowVersion });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        // The division and outlet under the moved area must now carry the new region.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<Division>().AsNoTracking().FirstAsync(x => x.Id == c.DivisionId)).RegionId.Should().Be(c.RegionBId);
        (await db.Set<Outlet>().AsNoTracking().FirstAsync(x => x.Id == c.OutletId)).RegionId.Should().Be(c.RegionBId);
    }

    [Fact]
    public async Task UpdateArea_RenameOnly_DoesNotChangeDescendants()
    {
        var c = await SeedChainAsync();

        var getArea = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/areas/{c.AreaId}", _jsonOpts);
        var rowVersion = getArea.GetProperty("data").GetProperty("rowVersion").GetUInt32();

        // Same region, new name → no cascade.
        var put = await _client.PutAsJsonAsync($"/api/v1/areas/{c.AreaId}",
            new { name = $"Renamed-{Guid.NewGuid():N}"[..20], regionId = c.RegionAId, rowVersion });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<Division>().AsNoTracking().FirstAsync(x => x.Id == c.DivisionId)).RegionId.Should().Be(c.RegionAId);
    }

    [Fact]
    public async Task Reconciliation_DetectsInjectedDrift_ThenRepairFixesIt()
    {
        var c = await SeedChainAsync();

        // Inject drift: point the division's denormalized RegionId at the wrong region without cascading.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var div = await db.Set<Division>().FirstAsync(x => x.Id == c.DivisionId);
            div.RegionId = c.RegionBId;   // territory still says region A → drift
            await db.SaveChangesAsync();
        }

        // Detect.
        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IGeoConsistencyService>();
            var result = await svc.RunAsync("manual:test");
            result.DriftCount.Should().BeGreaterThan(0);
            result.Drifts.Should().Contain(d => d.EntityType == "Division" && d.EntityId == c.DivisionId);
        }

        // Repair, then re-scan → clean for our division.
        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IGeoConsistencyService>();
            await svc.RepairAsync();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Set<Division>().AsNoTracking().FirstAsync(x => x.Id == c.DivisionId)).RegionId.Should().Be(c.RegionAId);
        }
    }

    [Fact]
    public async Task RepairEndpoint_AsNonAdmin_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.SalesRepToken);

        var response = await client.PostAsync("/api/v1/geo-consistency/repair", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RepairEndpoint_AsAdmin_Returns200WithFixCounts()
    {
        var response = await _client.PostAsync("/api/v1/geo-consistency/repair", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").TryGetProperty("totalFixed", out _).Should().BeTrue();
    }
}
