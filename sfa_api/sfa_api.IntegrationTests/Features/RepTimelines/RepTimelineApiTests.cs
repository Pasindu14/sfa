using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using sfa_api.Common.Extensions;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Requests;
using sfa_api.Features.LocationPings.Services;
using sfa_api.Features.NotBillings.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Features.RouteUnlockRequests;
using sfa_api.IntegrationTests.Infrastructure;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.IntegrationTests.Features.RepTimelines;

/// <summary>
/// Covers GET /api/v1/rep-timeline/{repId}?date= end to end, plus the capturedAt round-trip
/// through POST /api/v1/billings.
///
/// The GPS route half of the timeline runs a DateTimeOffset range query that the SQLite test
/// provider cannot translate (see LocationPingsApiTests), so ILocationPingService is replaced
/// with an empty-route stub. Everything else — auth, 404, bill / no-sale / assignment /
/// route-outlet queries, and the CapturedAt persistence — runs for real. The pure timeline
/// rules are covered by RepTimelineBuilderTests.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class RepTimelineApiTests
{
    private const string BaseUrl = "/api/v1/rep-timeline";
    private const double OutletLat = 6.9271, OutletLng = 79.8612;

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RepTimelineApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        // Same in-memory database (the connection lives on the parent factory), stubbed ping service.
        _client = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<ILocationPingService>();
            services.AddScoped<ILocationPingService, EmptyRouteLocationPingService>();
        })).CreateClient();
    }

    private sealed class EmptyRouteLocationPingService : ILocationPingService
    {
        public Task<int> RecordAsync(int repId, CreateLocationPingsRequest request, CancellationToken ct = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RepLocationPingDto>>([]);

        public Task<RepRouteDto> GetRepRouteAsync(int repId, DateOnly date, CancellationToken ct = default)
            => Task.FromResult(new RepRouteDto(
                repId, "stub", date,
                new RepRouteSummaryDto(0, null, null, 0, 0, 15),
                [], null));

        public Task<RepLastLocationDto?> GetLatestForRepAsync(int repId, CancellationToken ct = default)
            => Task.FromResult<RepLastLocationDto?>(null);

        public Task ReportTrackingStatusAsync(int repId, ReportTrackingStatusRequest request, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<RepTrackingStatusDto?> GetTrackingStatusAsync(int repId, CancellationToken ct = default)
            => Task.FromResult<RepTrackingStatusDto?>(null);
    }

    // ── Seed ─────────────────────────────────────────────────────────────

    private static volatile bool _seeded;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static int _divisionId, _territoryId, _areaId, _regionId, _productId;

    private async Task EnsureSharedSeedAsync()
    {
        if (_seeded) return;
        await Gate.WaitAsync();
        try
        {
            if (_seeded) return;
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = Guid.NewGuid().ToString("N")[..8];

            var region = new Region { Name = $"RTRegion-{s}", IsActive = true };
            db.Regions.Add(region);
            await db.SaveChangesAsync();
            var area = new Area { Name = $"RTArea-{s}", RegionId = region.Id, IsActive = true };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            var territory = new Territory { Name = $"RTTerr-{s}", AreaId = area.Id, RegionId = region.Id, IsActive = true };
            db.Territories.Add(territory);
            await db.SaveChangesAsync();
            var division = new Division
            {
                Name = $"RTDiv-{s}", TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Divisions.Add(division);
            await db.SaveChangesAsync();

            var distributor = new Distributor
            {
                Name = $"RTDist-{s}", Address = "1 Dist St", Phone = $"03{s}", Email = $"rtdist-{s}@sfa.com",
                Alias = Math.Abs(Guid.NewGuid().GetHashCode()), Category = "A",
                TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Distributors.Add(distributor);
            var product = new Product { Code = $"RTP-{s}", ItemDescription = "Timeline Product", IsActive = true };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            db.DistributorStocks.Add(new DistributorStock
            {
                DistributorId = distributor.Id, ProductId = product.Id, StockType = StockType.Normal,
                QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            _divisionId = division.Id; _territoryId = territory.Id; _areaId = area.Id;
            _regionId = region.Id; _productId = product.Id;
            _seeded = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    private sealed record Scenario(int RepId, int AdminId, int RouteId, int OutletId, int SecondOutletId);

    private async Task<int> SeedUserAsync(AppDbContext db, UserRole role, string label)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var u = new User
        {
            Name = $"{label}-{s}", Username = $"rt{s}", Email = $"rt-{s}@sfa.com", Phone = $"07{s}",
            PasswordHash = "x", Role = role, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    private Outlet NewOutlet(int routeId, string label)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        return new Outlet
        {
            Name = $"{label}-{s}", Address = "1 Test St", Tel = $"01{s}", NicNo = $"NIC{s}",
            RouteId = routeId, DivisionId = _divisionId, TerritoryId = _territoryId,
            AreaId = _areaId, RegionId = _regionId, Latitude = OutletLat, Longitude = OutletLng, IsActive = true
        };
    }

    /// A rep assigned today to a fresh route with two outlets, plus an admin.
    private async Task<Scenario> SeedScenarioAsync(bool assign = true)
    {
        await EnsureSharedSeedAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var repId = await SeedUserAsync(db, UserRole.SalesRep, "TimelineRep");
        var adminId = await SeedUserAsync(db, UserRole.Admin, "TimelineAdmin");

        var route = new RouteEntity
        {
            Name = $"TLRoute-{Guid.NewGuid().ToString("N")[..8]}", PinColor = "#3366FF",
            DivisionId = _divisionId, TerritoryId = _territoryId, AreaId = _areaId, RegionId = _regionId,
            IsActive = true
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        var outlet1 = NewOutlet(route.Id, "TLOutletA");
        var outlet2 = NewOutlet(route.Id, "TLOutletB");
        db.Outlets.AddRange(outlet1, outlet2);

        if (assign)
            db.DailyRouteAssignments.Add(new DailyRouteAssignment
                { UserId = repId, RouteId = route.Id, AssignedDate = SriLankaTime.Today, IsActive = true });
        db.UserGeoAssignments.Add(new UserGeoAssignment
        {
            UserId = repId, DivisionId = _divisionId, TerritoryId = _territoryId, AreaId = _areaId,
            RegionId = _regionId, IsActive = true, EffectiveFrom = SriLankaTime.Today
        });
        await db.SaveChangesAsync();

        return new Scenario(repId, adminId, route.Id, outlet1.Id, outlet2.Id);
    }

    // ── HTTP helpers ─────────────────────────────────────────────────────

    private void As(int userId, string role)
        => _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(userId, role));

    private sealed record Reply(HttpStatusCode Status, JsonElement Root, string Raw)
    {
        public JsonElement Data => Root.GetProperty("data");
        public string? ErrorCode => Root.TryGetProperty("error", out var e) ? e.GetProperty("code").GetString() : null;
    }

    private async Task<Reply> GetTimelineAsync(int repId, DateOnly? date = null)
    {
        var d = (date ?? SriLankaTime.Today).ToString("yyyy-MM-dd");
        var resp = await _client.GetAsync($"{BaseUrl}/{repId}?date={d}");
        var raw = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        return new Reply(resp.StatusCode, doc.RootElement.Clone(), raw);
    }

    private async Task<Reply> PostBillAsync(int outletId, DateTimeOffset? capturedAt)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billings")
        {
            Content = JsonContent.Create(new
            {
                outletId,
                billDiscountRate = 0m,
                notes = "timeline bill",
                billingDate = SriLankaTime.Today.ToString("yyyy-MM-dd"),
                latitude = OutletLat,
                longitude = OutletLng,
                capturedAt,
                items = new object[]
                {
                    new { productId = _productId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
                }
            })
        };
        req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var resp = await _client.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        return new Reply(resp.StatusCode, doc.RootElement.Clone(), raw);
    }

    private static List<JsonElement> EventsOfKind(JsonElement data, string kind)
        => data.GetProperty("events").EnumerateArray()
            .Where(e => e.GetProperty("kind").GetString() == kind).ToList();

    // ── Authorization / 404 ──────────────────────────────────────────────

    [Fact]
    public async Task Get_Anonymous_IsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        (await GetTimelineAsync(1)).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("SalesRep")]
    [InlineData("Supervisor")]
    public async Task Get_NonAdmin_IsForbidden(string role)
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, role);

        (await GetTimelineAsync(s.RepId)).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_UnknownRep_IsNotFound()
    {
        var s = await SeedScenarioAsync();
        As(s.AdminId, "Admin");

        var r = await GetTimelineAsync(9_999_999);

        r.Status.Should().Be(HttpStatusCode.NotFound, r.Raw);
    }

    // ── Data path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_RepWithNothingToday_ReturnsEmptyTimelineWithNoAssignment()
    {
        var s = await SeedScenarioAsync(assign: false);
        As(s.AdminId, "Admin");

        var r = await GetTimelineAsync(s.RepId);

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("assignment").ValueKind.Should().Be(JsonValueKind.Null);
        r.Data.GetProperty("events").GetArrayLength().Should().Be(0);
        r.Data.GetProperty("summary").GetProperty("coveragePercent").ValueKind.Should().Be(JsonValueKind.Null);
        r.Data.GetProperty("route").GetProperty("points").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_AdminForRepWithBillAndNoSale_ReturnsAssignmentEventsSummaryAndRoute()
    {
        var s = await SeedScenarioAsync();
        var capturedAt = DateTimeOffset.UtcNow.AddMinutes(-90);

        As(s.RepId, "SalesRep");
        var bill = await PostBillAsync(s.OutletId, capturedAt);
        bill.Status.Should().Be(HttpStatusCode.Created, bill.Raw);
        var billingId = bill.Data.GetProperty("id").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NotBillings.Add(new NotBilling
            {
                NotBillingNumber = $"NBL-TL-{Guid.NewGuid().ToString("N")[..8]}",
                NotBillingDate = SriLankaTime.Today, OutletId = s.SecondOutletId, SalesRepId = s.RepId,
                Reason = NotBillingReason.OutletClosed, RouteId = s.RouteId,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = s.RepId
            });
            await db.SaveChangesAsync();
        }

        As(s.AdminId, "Admin");
        var r = await GetTimelineAsync(s.RepId);

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("repId").GetInt32().Should().Be(s.RepId);

        var assignment = r.Data.GetProperty("assignment");
        assignment.GetProperty("routeId").GetInt32().Should().Be(s.RouteId);
        assignment.GetProperty("plannedOutlets").GetInt32().Should().Be(2);

        var billEvent = EventsOfKind(r.Data, "Bill").Should().ContainSingle().Subject;
        billEvent.GetProperty("billingId").GetInt32().Should().Be(billingId);
        billEvent.GetProperty("outletId").GetInt32().Should().Be(s.OutletId);
        billEvent.GetProperty("timeSource").GetString().Should().Be("Device");
        billEvent.GetProperty("at").GetDateTimeOffset().UtcDateTime
            .Should().BeCloseTo(capturedAt.UtcDateTime, TimeSpan.FromSeconds(1));
        billEvent.GetProperty("cancelled").GetBoolean().Should().BeFalse();

        var visit = EventsOfKind(r.Data, "NoSale").Should().ContainSingle().Subject;
        visit.GetProperty("outletId").GetInt32().Should().Be(s.SecondOutletId);
        visit.GetProperty("detail").GetString().Should().Be("OutletClosed");
        visit.GetProperty("timeSource").GetString().Should().Be("Server");

        EventsOfKind(r.Data, "DayStart").Should().ContainSingle();
        EventsOfKind(r.Data, "DayEnd").Should().ContainSingle();

        var summary = r.Data.GetProperty("summary");
        summary.GetProperty("billCount").GetInt32().Should().Be(1);
        summary.GetProperty("noSaleCount").GetInt32().Should().Be(1);
        summary.GetProperty("outletsCovered").GetInt32().Should().Be(2);
        summary.GetProperty("plannedOutlets").GetInt32().Should().Be(2);
        summary.GetProperty("coveragePercent").GetDouble().Should().Be(100.0);

        r.Data.GetProperty("route").GetProperty("repId").GetInt32().Should().Be(s.RepId);
    }

    // ── capturedAt persistence via POST /billings ────────────────────────

    [Fact]
    public async Task CreateBill_WithPlausibleCapturedAt_PersistsItAndTimelineShowsDeviceTime()
    {
        var s = await SeedScenarioAsync();
        var capturedAt = DateTimeOffset.UtcNow.AddHours(-3);
        As(s.RepId, "SalesRep");

        var bill = await PostBillAsync(s.OutletId, capturedAt);
        bill.Status.Should().Be(HttpStatusCode.Created, bill.Raw);
        var billingId = bill.Data.GetProperty("id").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Billings.AsNoTracking().FirstAsync(b => b.Id == billingId);
            stored.CapturedAt.Should().NotBeNull();
            stored.CapturedAt!.Value.AsUtc().Should().BeCloseTo(capturedAt.UtcDateTime, TimeSpan.FromSeconds(1));
        }

        As(s.AdminId, "Admin");
        var r = await GetTimelineAsync(s.RepId);
        var e = EventsOfKind(r.Data, "Bill").Should().ContainSingle().Subject;
        e.GetProperty("timeSource").GetString().Should().Be("Device");
        e.GetProperty("syncedLate").GetBoolean().Should().BeTrue();   // received ~3 h after capture
        r.Data.GetProperty("summary").GetProperty("lateSyncCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CreateBill_WithWildlyFutureCapturedAt_DropsItAndTimelineShowsServerTime()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        var bill = await PostBillAsync(s.OutletId, DateTimeOffset.UtcNow.AddDays(30));
        bill.Status.Should().Be(HttpStatusCode.Created, bill.Raw);
        var billingId = bill.Data.GetProperty("id").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Billings.AsNoTracking().FirstAsync(b => b.Id == billingId))
                .CapturedAt.Should().BeNull();
        }

        As(s.AdminId, "Admin");
        var r = await GetTimelineAsync(s.RepId);
        var e = EventsOfKind(r.Data, "Bill").Should().ContainSingle().Subject;
        e.GetProperty("timeSource").GetString().Should().Be("Server");
        e.GetProperty("syncedLate").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateBill_WithoutCapturedAt_FallsBackToServerTime()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        var bill = await PostBillAsync(s.OutletId, null);
        bill.Status.Should().Be(HttpStatusCode.Created, bill.Raw);

        As(s.AdminId, "Admin");
        var r = await GetTimelineAsync(s.RepId);
        EventsOfKind(r.Data, "Bill").Should().ContainSingle()
            .Which.GetProperty("timeSource").GetString().Should().Be("Server");
    }
}
