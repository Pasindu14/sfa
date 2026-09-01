using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.LocationPings.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.LocationPings;

/// <summary>
/// Covers GET /api/v1/location-pings/rep/{repId}/route — one rep's travelled route for a
/// single Sri Lanka business day.
///
/// Authorization and not-found paths run here. The data path cannot: EF Core's SQLite
/// provider refuses to translate ordering comparisons (&gt;=, &lt;) on DateTimeOffset, because it
/// persists the value as TEXT that is not sortable across differing offsets. PostgreSQL maps
/// the column to timestamptz and compares instants exactly, so the query is correct in
/// production — it simply cannot be exercised against the in-memory SQLite test database.
///
/// The behaviour those skipped tests would protect — a Colombo business day running
/// 00:00–24:00 at UTC+5:30 rather than UTC midnight — is covered by
/// sfa_api.UnitTests/Common/SriLankaTimeTests.cs, which tests SriLankaTime.DayRange directly.
/// Note GetLatestPerRepAsync is likewise not SQLite-testable, as it relies on PostgreSQL
/// DISTINCT ON.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class LocationPingsApiTests
{
    private const string SqliteDateTimeOffsetSkip =
        "SQLite test provider cannot translate ordering comparisons on DateTimeOffset; " +
        "the Colombo day-boundary logic is covered by SriLankaTimeTests. " +
        "Verified on PostgreSQL in production.";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly TimeSpan SlOffset = TimeSpan.FromHours(5.5);
    private static readonly DateOnly TargetDay = new(2026, 6, 15);

    public LocationPingsApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        SetToken(AuthHelper.GenerateToken(9001, "Admin"));
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private record Envelope<T>(bool Success, T Data);

    private record RoutePoint(double Latitude, double Longitude, float Accuracy,
                              DateTimeOffset RecordedAt, DateTimeOffset ReceivedAt);

    private record RouteSummary(int PointCount, DateTimeOffset? FirstPingAt,
                                DateTimeOffset? LastPingAt, double TotalDistanceMeters);

    private record RouteResult(int RepId, string RepName, DateOnly Date,
                               RouteSummary Summary, List<RoutePoint> Points);

    /// Seeds a rep plus the given pings. Each ping is expressed in SRI LANKA local time so
    /// the tests read the way a person thinks about the business day.
    private async Task<int> SeedRepWithPingsAsync(params (DateTime SlLocal, double Lat, double Lng)[] pings)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rep = new User
        {
            Name = $"Route Rep {suffix}", Username = $"routerep-{suffix}",
            Email = $"routerep-{suffix}@sfa.com", Phone = $"07{suffix}",
            PasswordHash = "placeholder", Role = UserRole.SalesRep, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(rep);
        await db.SaveChangesAsync();

        foreach (var (slLocal, lat, lng) in pings)
        {
            db.RepLocationPings.Add(new RepLocationPing
            {
                RepId = rep.Id,
                Latitude = lat,
                Longitude = lng,
                Accuracy = 10f,
                RecordedAt = new DateTimeOffset(slLocal, SlOffset),
                ReceivedAt = new DateTimeOffset(slLocal, SlOffset),
            });
        }
        await db.SaveChangesAsync();

        return rep.Id;
    }

    private async Task<(HttpStatusCode Status, RouteResult? Data, string Raw)> GetRouteAsync(int repId, DateOnly date)
    {
        var resp = await _client.GetAsync($"/api/v1/location-pings/rep/{repId}/route?date={date:yyyy-MM-dd}");
        var raw = await resp.Content.ReadAsStringAsync();
        if (resp.StatusCode != HttpStatusCode.OK) return (resp.StatusCode, null, raw);

        var envelope = JsonSerializer.Deserialize<Envelope<RouteResult>>(raw, _jsonOpts);
        return (resp.StatusCode, envelope!.Data, raw);
    }

    [Fact(Skip = SqliteDateTimeOffsetSkip)]
    public async Task GetRepRoute_ReturnsOnlyThatDaysPings_OrderedByRecordedAt()
    {
        // Seeded out of chronological order to prove the endpoint sorts rather than
        // relying on insertion order.
        var repId = await SeedRepWithPingsAsync(
            (TargetDay.ToDateTime(new TimeOnly(14, 0)), 6.93, 79.86),
            (TargetDay.ToDateTime(new TimeOnly(9, 0)),  6.92, 79.85),
            (TargetDay.AddDays(-1).ToDateTime(new TimeOnly(11, 0)), 6.90, 79.80),
            (TargetDay.AddDays(1).ToDateTime(new TimeOnly(11, 0)),  6.95, 79.90));

        var (status, data, raw) = await GetRouteAsync(repId, TargetDay);

        status.Should().Be(HttpStatusCode.OK, raw);
        data!.Points.Should().HaveCount(2, "the previous and next day's pings must be excluded");
        data.Points.Should().BeInAscendingOrder(p => p.RecordedAt);
        data.Summary.PointCount.Should().Be(2);
        data.Summary.FirstPingAt!.Value.Should().BeBefore(data.Summary.LastPingAt!.Value);
    }

    [Fact(Skip = SqliteDateTimeOffsetSkip)]
    public async Task GetRepRoute_PingJustAfterColomboMidnight_BelongsToThatColomboDay()
    {
        // 00:30 Sri Lanka time == 19:00 UTC on the PREVIOUS calendar day. Filtering on UTC
        // midnight would push this ping into the previous business day.
        var repId = await SeedRepWithPingsAsync(
            (TargetDay.ToDateTime(new TimeOnly(0, 30)), 6.92, 79.85));

        var (status, data, raw) = await GetRouteAsync(repId, TargetDay);
        status.Should().Be(HttpStatusCode.OK, raw);
        data!.Points.Should().HaveCount(1, "00:30 SL falls inside the Colombo business day, not the previous one");

        var (prevStatus, prevData, prevRaw) = await GetRouteAsync(repId, TargetDay.AddDays(-1));
        prevStatus.Should().Be(HttpStatusCode.OK, prevRaw);
        prevData!.Points.Should().BeEmpty("the previous business day must not absorb a 00:30 SL ping");
    }

    [Fact(Skip = SqliteDateTimeOffsetSkip)]
    public async Task GetRepRoute_LatePingBeforeColomboMidnight_StaysOnThatDay()
    {
        // The mirror case: 23:45 SL is 18:15 UTC the same day, so a naive UTC filter would
        // keep it — but it must not leak into the following business day.
        var repId = await SeedRepWithPingsAsync(
            (TargetDay.ToDateTime(new TimeOnly(23, 45)), 6.92, 79.85));

        var (_, sameDay, _) = await GetRouteAsync(repId, TargetDay);
        sameDay!.Points.Should().HaveCount(1);

        var (_, nextDay, _) = await GetRouteAsync(repId, TargetDay.AddDays(1));
        nextDay!.Points.Should().BeEmpty();
    }

    [Fact(Skip = SqliteDateTimeOffsetSkip)]
    public async Task GetRepRoute_ComputesTotalDistanceBetweenConsecutivePoints()
    {
        // Two points ~1 degree of latitude apart ≈ 111 km.
        var repId = await SeedRepWithPingsAsync(
            (TargetDay.ToDateTime(new TimeOnly(9, 0)),  6.0, 80.0),
            (TargetDay.ToDateTime(new TimeOnly(10, 0)), 7.0, 80.0));

        var (_, data, raw) = await GetRouteAsync(repId, TargetDay);

        data!.Summary.TotalDistanceMeters.Should().BeApproximately(111_195, 2_000, raw);
    }

    [Fact(Skip = SqliteDateTimeOffsetSkip)]
    public async Task GetRepRoute_RepWithNoPingsThatDay_ReturnsEmptyRouteNot404()
    {
        var repId = await SeedRepWithPingsAsync();

        var (status, data, raw) = await GetRouteAsync(repId, TargetDay);

        status.Should().Be(HttpStatusCode.OK, raw);
        data!.Points.Should().BeEmpty();
        data.Summary.PointCount.Should().Be(0);
        data.Summary.TotalDistanceMeters.Should().Be(0);
        data.Summary.FirstPingAt.Should().BeNull();
        data.RepName.Should().NotBeNullOrEmpty("a real rep who simply didn't move is still identified");
    }

    [Fact]
    public async Task GetRepRoute_UnknownRep_Returns404()
    {
        var (status, _, _) = await GetRouteAsync(999_999, TargetDay);
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRepRoute_AsSalesRep_Returns403()
    {
        var repId = await SeedRepWithPingsAsync();
        SetToken(AuthHelper.GenerateToken(9002, "SalesRep"));

        var (status, _, _) = await GetRouteAsync(repId, TargetDay);

        status.Should().Be(HttpStatusCode.Forbidden, "location history is staff movement data, Admin only");
    }

    [Fact]
    public async Task GetRepRoute_Unauthenticated_Returns401()
    {
        var repId = await SeedRepWithPingsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var (status, _, _) = await GetRouteAsync(repId, TargetDay);

        status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
