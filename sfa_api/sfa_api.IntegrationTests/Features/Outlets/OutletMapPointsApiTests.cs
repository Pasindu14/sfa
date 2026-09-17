using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Outlets;

/// <summary>
/// GET /api/v1/outlets/map-points with the optional viewport box (minLat, minLng, maxLat, maxLng).
/// Outlets are seeded directly (FK enforcement briefly off — only their own columns matter) in a
/// far-south-Pacific box no other test uses, and removed afterwards.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class OutletMapPointsApiTests(SfaWebApplicationFactory factory)
{
    private const string Url = "/api/v1/outlets/map-points";
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record Point(int Id, string Name, double Latitude, double Longitude);

    private async Task<HttpResponseMessage> GetAsync(string query)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, Url + query);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.AdminToken);
        return await _client.SendAsync(req);
    }

    private static List<Point> ReadPoints(string raw)
        => JsonDocument.Parse(raw).RootElement.GetProperty("data").EnumerateArray()
            .Select(e => new Point(
                e.GetProperty("id").GetInt32(),
                e.GetProperty("name").GetString()!,
                e.GetProperty("latitude").GetDouble(),
                e.GetProperty("longitude").GetDouble()))
            .ToList();

    /// <summary>Seeds outlets around (-60, -150), runs <paramref name="body"/>, then deletes them.</summary>
    private async Task WithSeededOutletsAsync(Func<Dictionary<string, int>, Task> body)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Outlet O(string tag, double lat, double lng, bool active = true, bool deleted = false) => new()
        {
            Name = $"Map-{suffix}-{tag}", Address = "1 Map St", Tel = "0770000000", NicNo = $"MAP{suffix}",
            Latitude = lat, Longitude = lng, OutletType = OutletType.Small, OutletCategory = OutletCategory.Wholesale,
            RouteId = 1, DivisionId = 1, TerritoryId = 1, AreaId = 1, RegionId = 1,
            IsActive = active, IsDeleted = deleted,
        };

        var outlets = new Dictionary<string, Outlet>
        {
            ["inside"]      = O("inside", -60.5, -150.5),
            ["cornerMin"]   = O("cornerMin", -61.0, -151.0),   // exactly on min bounds → inclusive
            ["cornerMax"]   = O("cornerMax", -60.0, -150.0),   // exactly on max bounds → inclusive
            ["latOutside"]  = O("latOutside", -59.9, -150.5),
            ["lngOutside"]  = O("lngOutside", -60.5, -149.9),
            ["inactive"]    = O("inactive", -60.5, -150.5, active: false),
            ["deleted"]     = O("deleted", -60.5, -150.5, active: false, deleted: true),
        };

        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        try
        {
            db.Outlets.AddRange(outlets.Values);
            await db.SaveChangesAsync();
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        try
        {
            await body(outlets.ToDictionary(kv => kv.Key, kv => kv.Value.Id));
        }
        finally
        {
            var ids = outlets.Values.Select(o => o.Id).ToList();
            await db.Outlets.IgnoreQueryFilters().Where(o => ids.Contains(o.Id)).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task MapPoints_NoBounds_ReturnsEveryActiveOutlet_AsBefore()
    {
        await WithSeededOutletsAsync(async ids =>
        {
            var response = await GetAsync("");
            var raw = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, raw);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var expectedIds = await db.Outlets.Where(o => o.IsActive).Select(o => o.Id).ToListAsync();

            var points = ReadPoints(raw);
            points.Select(p => p.Id).Should().BeEquivalentTo(expectedIds);
            points.Select(p => p.Id).Should().Contain([ids["inside"], ids["latOutside"], ids["lngOutside"]])
                .And.NotContain([ids["inactive"], ids["deleted"]]);
        });
    }

    [Fact]
    public async Task MapPoints_AllFourBounds_ReturnsOnlyPointsInsideTheBox_Inclusive()
    {
        await WithSeededOutletsAsync(async ids =>
        {
            var response = await GetAsync("?minLat=-61&minLng=-151&maxLat=-60&maxLng=-150");
            var raw = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, raw);

            var points = ReadPoints(raw);
            points.Select(p => p.Id).Should().BeEquivalentTo([ids["inside"], ids["cornerMin"], ids["cornerMax"]]);

            var inside = points.Single(p => p.Id == ids["inside"]);
            inside.Latitude.Should().Be(-60.5);
            inside.Longitude.Should().Be(-150.5);
            inside.Name.Should().EndWith("-inside");
        });
    }

    [Theory]
    [InlineData("?minLat=-61", new[] { "minLng", "maxLat", "maxLng" })]
    [InlineData("?minLat=-61&minLng=-151&maxLat=-60", new[] { "maxLng" })]
    [InlineData("?maxLng=10", new[] { "minLat", "minLng", "maxLat" })]
    public async Task MapPoints_PartialBounds_Returns400WithFieldErrors(string query, string[] missing)
    {
        var response = await GetAsync(query);
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);

        var root = JsonDocument.Parse(raw).RootElement.GetProperty("error");
        root.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        var fields = root.GetProperty("fields").EnumerateObject().Select(p => p.Name).ToList();
        fields.Should().BeEquivalentTo(missing);
    }

    [Theory]
    [InlineData("?minLat=10&minLng=0&maxLat=5&maxLng=1", "minLat")]      // min > max (lat)
    [InlineData("?minLat=0&minLng=10&maxLat=1&maxLng=5", "minLng")]      // min > max (lng)
    [InlineData("?minLat=-91&minLng=0&maxLat=1&maxLng=1", "minLat")]     // lat out of range
    [InlineData("?minLat=0&minLng=0&maxLat=90.5&maxLng=1", "maxLat")]
    [InlineData("?minLat=0&minLng=-180.1&maxLat=1&maxLng=1", "minLng")]  // lng out of range
    [InlineData("?minLat=0&minLng=0&maxLat=1&maxLng=181", "maxLng")]
    [InlineData("?minLat=0&minLng=0&maxLat=1&maxLng=NaN", "maxLng")]     // non-finite
    [InlineData("?minLat=abc&minLng=0&maxLat=1&maxLng=1", "minLat")]     // not a number (model binding)
    public async Task MapPoints_InvalidBounds_Returns400WithFieldError(string query, string field)
    {
        var response = await GetAsync(query);
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);

        var root = JsonDocument.Parse(raw).RootElement.GetProperty("error");
        root.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        root.GetProperty("fields").EnumerateObject().Select(p => p.Name)
            .Should().Contain(n => string.Equals(n, field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MapPoints_FullWorldBounds_AreValid()
    {
        var response = await GetAsync("?minLat=-90&minLng=-180&maxLat=90&maxLng=180");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
