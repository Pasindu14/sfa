using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Common.Extensions;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.IntegrationTests.Features.RouteUnlockRequests;

/// <summary>
/// Covers /api/v1/route-unlock-requests end to end, including the two places the
/// approval actually changes behaviour: the mobile outlet sync
/// (GET /outlets/by-route/{id}) and the bill-create geofence gate.
///
/// Each test builds its own rep, supervisor and route because a rep may hold only
/// one open request per day and the SQLite database is shared across the whole
/// collection. The xmin RowVersion 409 path is not testable on SQLite.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class RouteUnlockRequestsApiTests
{
    private const string BaseUrl = "/api/v1/route-unlock-requests";
    private const double OutletLat = 6.9271, OutletLng = 79.8612;   // Colombo
    private const double FarLat = 9.6615, FarLng = 80.0255;         // Jaffna

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RouteUnlockRequestsApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Seed ─────────────────────────────────────────────────────────────

    private static volatile bool _seeded;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static int _divisionId, _territoryId, _areaId, _regionId, _productId;

    /// Shared geographic hierarchy, distributor (resolved by territory at bill
    /// time) and stocked product — everything a bill needs that is not per-rep.
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

            var region = new Region { Name = $"RURegion-{s}", IsActive = true };
            db.Regions.Add(region);
            await db.SaveChangesAsync();
            var area = new Area { Name = $"RUArea-{s}", RegionId = region.Id, IsActive = true };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            var territory = new Territory { Name = $"RUTerr-{s}", AreaId = area.Id, RegionId = region.Id, IsActive = true };
            db.Territories.Add(territory);
            await db.SaveChangesAsync();
            var division = new Division
            {
                Name = $"RUDiv-{s}", TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Divisions.Add(division);
            await db.SaveChangesAsync();

            var distributor = new Distributor
            {
                Name = $"RUDist-{s}", Address = "1 Dist St", Phone = $"02{s}", Email = $"rudist-{s}@sfa.com",
                Alias = Math.Abs(Guid.NewGuid().GetHashCode()), Category = "A",
                TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Distributors.Add(distributor);
            var product = new Product { Code = $"RUP-{s}", ItemDescription = "Unlock Product", IsActive = true };
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

    private sealed record Scenario(
        int RepId, int SupervisorId, int OtherSupervisorId, int OtherRepId, int AdminId,
        int RouteId, int OutletId, int OtherRouteId, int OtherRouteOutletId);

    private async Task<int> SeedUserAsync(AppDbContext db, UserRole role, string label)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var u = new User
        {
            Name = $"{label}-{s}", Username = $"ru{s}", Email = $"ru-{s}@sfa.com", Phone = $"07{s}",
            PasswordHash = "x", Role = role, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    private async Task<(int RouteId, int OutletId)> SeedRouteWithOutletAsync(AppDbContext db, string label)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var route = new RouteEntity
        {
            Name = $"{label}-{s}", PinColor = "#3366FF", DivisionId = _divisionId, TerritoryId = _territoryId,
            AreaId = _areaId, RegionId = _regionId, IsActive = true
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        var outlet = new Outlet
        {
            Name = $"{label}Outlet-{s}", Address = "1 Test St", Tel = $"01{s}", NicNo = $"NIC{s}",
            RouteId = route.Id, DivisionId = _divisionId, TerritoryId = _territoryId,
            AreaId = _areaId, RegionId = _regionId, Latitude = OutletLat, Longitude = OutletLng, IsActive = true
        };
        db.Outlets.Add(outlet);
        await db.SaveChangesAsync();
        return (route.Id, outlet.Id);
    }

    /// A rep assigned to a fresh route today and reporting to supervisor 1, plus a
    /// second supervisor and rep who are unrelated, an admin, and a second route.
    private async Task<Scenario> SeedScenarioAsync(bool assignRepToday = true, bool repHasSupervisor = true)
    {
        await EnsureSharedSeedAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var repId = await SeedUserAsync(db, UserRole.SalesRep, "UnlockRep");
        var otherRepId = await SeedUserAsync(db, UserRole.SalesRep, "OtherRep");
        var supId = await SeedUserAsync(db, UserRole.Supervisor, "Sup1");
        var otherSupId = await SeedUserAsync(db, UserRole.Supervisor, "Sup2");
        var adminId = await SeedUserAsync(db, UserRole.Admin, "UnlockAdmin");

        var (routeId, outletId) = await SeedRouteWithOutletAsync(db, "RouteA");
        var (otherRouteId, otherOutletId) = await SeedRouteWithOutletAsync(db, "RouteB");
        var (repsOtherRoute, _) = await SeedRouteWithOutletAsync(db, "RouteC");

        if (assignRepToday)
            db.DailyRouteAssignments.Add(new DailyRouteAssignment
                { UserId = repId, RouteId = routeId, AssignedDate = SriLankaTime.Today, IsActive = true });
        db.DailyRouteAssignments.Add(new DailyRouteAssignment
            { UserId = otherRepId, RouteId = repsOtherRoute, AssignedDate = SriLankaTime.Today, IsActive = true });

        if (repHasSupervisor)
            db.UserReportingLines.Add(new UserReportingLine
                { UserId = repId, ReportsToUserId = supId, EffectiveFrom = SriLankaTime.Today, IsActive = true });
        db.UserReportingLines.Add(new UserReportingLine
            { UserId = otherRepId, ReportsToUserId = otherSupId, EffectiveFrom = SriLankaTime.Today, IsActive = true });

        db.UserGeoAssignments.Add(new UserGeoAssignment
        {
            UserId = repId, DivisionId = _divisionId, TerritoryId = _territoryId, AreaId = _areaId,
            RegionId = _regionId, IsActive = true, EffectiveFrom = SriLankaTime.Today
        });
        await db.SaveChangesAsync();

        return new Scenario(repId, supId, otherSupId, otherRepId, adminId,
            routeId, outletId, otherRouteId, otherOutletId);
    }

    // ── HTTP helpers ─────────────────────────────────────────────────────

    // SQLite hands back DateTimes with Kind=Unspecified (no trailing Z); on PostgreSQL they are UTC.


    private void As(int userId, string role)
        => _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(userId, role));

    private sealed record Reply(HttpStatusCode Status, JsonElement Root, string Raw)
    {
        public JsonElement Data => Root.GetProperty("data");
        public string? ErrorCode => Root.TryGetProperty("error", out var e) ? e.GetProperty("code").GetString() : null;
    }

    private async Task<Reply> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (body is not null) req.Content = JsonContent.Create(body);
        var resp = await _client.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        return new Reply(resp.StatusCode, doc.RootElement.Clone(), raw);
    }

    private Task<Reply> CreateAsync(string reason = "Need to cover the full route")
        => SendAsync(HttpMethod.Post, BaseUrl, new { reason, latitude = OutletLat, longitude = OutletLng, gpsAccuracyMeters = 9.5 });

    private async Task<(int Id, uint RowVersion)> RepCreatesAsync(Scenario s)
    {
        As(s.RepId, "SalesRep");
        var r = await CreateAsync();
        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        return (r.Data.GetProperty("id").GetInt32(), r.Data.GetProperty("rowVersion").GetUInt32());
    }


    private Task<Reply> ApproveAsync(int id, string note = "ok")
        => SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/approve", new { rowVersion = 1u, note });


    private Task<Reply> ReasonedAsync(int id, string verb, string reason)
        => SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/{verb}", new { rowVersion = 1u, reason });

    private async Task<JsonElement> OutletSyncAsync(int routeId)
    {
        var r = await SendAsync(HttpMethod.Get, $"/api/v1/outlets/by-route/{routeId}");
        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        return r.Data;
    }

    // ── Auth ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Anonymous_IsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        (await CreateAsync()).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Supervisor")]
    [InlineData("Admin")]
    public async Task Create_NonRep_IsForbidden(string role)
    {
        As(9300, role);

        (await CreateAsync()).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_AsRep_IsForbidden()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        (await SendAsync(HttpMethod.Get, BaseUrl)).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_RepWithAssignment_ReturnsPendingRoutedToSupervisor()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        var r = await CreateAsync("GPS not accurate");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("status").GetString().Should().Be("Pending");
        r.Data.GetProperty("effectiveStatus").GetString().Should().Be("Pending");
        r.Data.GetProperty("routeId").GetInt32().Should().Be(s.RouteId);
        r.Data.GetProperty("userId").GetInt32().Should().Be(s.RepId);
        r.Data.GetProperty("supervisorUserId").GetInt32().Should().Be(s.SupervisorId);
        r.Data.GetProperty("businessDate").GetString().Should().Be(SriLankaTime.Today.ToString("yyyy-MM-dd"));
        r.Data.GetProperty("requestReason").GetString().Should().Be("GPS not accurate");
        r.Data.GetProperty("isCurrentlyEffective").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Create_SecondWhileOneIsOpen_Returns409AlreadyOpen()
    {
        var s = await SeedScenarioAsync();
        await RepCreatesAsync(s);

        var second = await CreateAsync();

        second.Status.Should().Be(HttpStatusCode.Conflict, second.Raw);
        second.ErrorCode.Should().Be("ROUTE_UNLOCK_ALREADY_OPEN");
    }

    [Fact]
    public async Task Create_NoAssignmentToday_Returns422()
    {
        var s = await SeedScenarioAsync(assignRepToday: false);
        As(s.RepId, "SalesRep");

        var r = await CreateAsync();

        r.Status.Should().Be(HttpStatusCode.UnprocessableEntity, r.Raw);
        r.ErrorCode.Should().Be("ROUTE_UNLOCK_NO_ASSIGNMENT");
    }

    [Fact]
    public async Task Create_ReasonTooShort_Returns400Validation()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        var r = await CreateAsync("ab");

        r.Status.Should().Be(HttpStatusCode.BadRequest, r.Raw);
        r.ErrorCode.Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Create_RepWithNoSupervisor_StillCreatesWithNullSupervisor()
    {
        var s = await SeedScenarioAsync(repHasSupervisor: false);
        As(s.RepId, "SalesRep");

        var r = await CreateAsync();

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("supervisorUserId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_AfterRejection_AllowsAReAsk()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        (await ReasonedAsync(id, "reject", "Not now")).Status.Should().Be(HttpStatusCode.OK);

        As(s.RepId, "SalesRep");
        var again = await CreateAsync();

        again.Status.Should().Be(HttpStatusCode.OK, again.Raw);
        again.Data.GetProperty("id").GetInt32().Should().NotBe(id);
    }

    [Fact]
    public async Task Create_FourthRequestInADay_Returns422DailyLimit()
    {
        var s = await SeedScenarioAsync();
        for (var i = 0; i < 3; i++)
        {
            var (id, _) = await RepCreatesAsync(s);
            As(s.RepId, "SalesRep");
            (await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/cancel", new { rowVersion = 1u }))
                .Status.Should().Be(HttpStatusCode.OK);
        }

        var fourth = await CreateAsync();

        fourth.Status.Should().Be(HttpStatusCode.UnprocessableEntity, fourth.Raw);
        fourth.ErrorCode.Should().Be("ROUTE_UNLOCK_DAILY_LIMIT");
    }

    // ── Scoping of lists ─────────────────────────────────────────────────

    [Fact]
    public async Task ListAndPendingCount_SupervisorSeesOwnRepsOnly_AdminSeesAll()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);

        As(s.SupervisorId, "Supervisor");
        var list = await SendAsync(HttpMethod.Get, $"{BaseUrl}?status=Pending&pageSize=200");
        list.Status.Should().Be(HttpStatusCode.OK, list.Raw);
        list.Data.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).Should().Contain(id);
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/pending-count")).Data.GetProperty("count").GetInt32()
            .Should().Be(1);

        As(s.OtherSupervisorId, "Supervisor");
        var otherList = await SendAsync(HttpMethod.Get, $"{BaseUrl}?pageSize=200");
        otherList.Data.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).Should().NotContain(id);
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/pending-count")).Data.GetProperty("count").GetInt32()
            .Should().Be(0);

        As(s.AdminId, "Admin");
        var adminList = await SendAsync(HttpMethod.Get, $"{BaseUrl}?pageSize=200&search=");
        adminList.Data.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).Should().Contain(id);
    }

    [Fact]
    public async Task GetMyToday_ReturnsLatestOrNull()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");
        var none = await SendAsync(HttpMethod.Get, $"{BaseUrl}/my/today");
        none.Status.Should().Be(HttpStatusCode.OK, none.Raw);
        none.Data.ValueKind.Should().Be(JsonValueKind.Null);

        var (id, _) = await RepCreatesAsync(s);

        var mine = await SendAsync(HttpMethod.Get, $"{BaseUrl}/my/today");
        mine.Data.GetProperty("id").GetInt32().Should().Be(id);
        mine.Data.GetProperty("status").GetString().Should().Be("Pending");
    }

    // ── Approve / role gates ─────────────────────────────────────────────

    [Fact]
    public async Task Approve_ByAnotherSupervisor_IsForbidden()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.OtherSupervisorId, "Supervisor");

        var r = await ApproveAsync(id);

        r.Status.Should().Be(HttpStatusCode.Forbidden, r.Raw);
        r.ErrorCode.Should().Be("FORBIDDEN_ACCESS");
    }

    [Fact]
    public async Task Approve_ByRep_IsForbidden()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.RepId, "SalesRep");

        (await ApproveAsync(id)).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_BySupervisor_MarksApprovedWithMidnightWindow()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        var r = await ApproveAsync(id, "go ahead");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("status").GetString().Should().Be("Approved");
        r.Data.GetProperty("isCurrentlyEffective").GetBoolean().Should().BeTrue();
        r.Data.GetProperty("reviewedByRole").GetString().Should().Be("Supervisor");
        r.Data.GetProperty("reviewedByUserId").GetInt32().Should().Be(s.SupervisorId);
        r.Data.GetProperty("reviewNote").GetString().Should().Be("go ahead");
        r.Data.GetProperty("validTo").GetDateTime().AsUtc()
            .Should().Be(SriLankaTime.StartOfDayUtc(SriLankaTime.Today.AddDays(1)));
    }

    [Fact]
    public async Task Approve_ByAdmin_Works()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.AdminId, "Admin");

        var r = await ApproveAsync(id);

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("reviewedByRole").GetString().Should().Be("Admin");
    }

    [Fact]
    public async Task Approve_Twice_Returns409InvalidState()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        (await ApproveAsync(id)).Status.Should().Be(HttpStatusCode.OK);

        var again = await ApproveAsync(id);

        again.Status.Should().Be(HttpStatusCode.Conflict, again.Raw);
        again.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    [Fact]
    public async Task Approve_AfterRepMovedRoutes_Returns422AssignmentChanged()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var a = await db.DailyRouteAssignments.FirstAsync(x => x.UserId == s.RepId && x.AssignedDate == SriLankaTime.Today);
            a.RouteId = s.OtherRouteId;
            await db.SaveChangesAsync();
        }
        As(s.SupervisorId, "Supervisor");

        var r = await ApproveAsync(id);

        r.Status.Should().Be(HttpStatusCode.UnprocessableEntity, r.Raw);
        r.ErrorCode.Should().Be("ROUTE_UNLOCK_ASSIGNMENT_CHANGED");
    }

    [Fact]
    public async Task Approve_NotFound_Returns404()
    {
        As(9301, "Admin");

        (await ApproveAsync(987654)).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ZeroRowVersion_Returns400()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        var r = await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/approve", new { rowVersion = 0 });

        r.Status.Should().Be(HttpStatusCode.BadRequest, r.Raw);
    }

    // ── Reject / cancel / revoke ─────────────────────────────────────────

    [Fact]
    public async Task Reject_RecordsReasonAndDoesNotRelaxTheGeofence()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        var r = await ReasonedAsync(id, "reject", "Outlet is close enough");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("status").GetString().Should().Be("Rejected");
        r.Data.GetProperty("reviewNote").GetString().Should().Be("Outlet is close enough");

        As(s.RepId, "SalesRep");
        (await OutletSyncAsync(s.RouteId)).GetProperty("geofenceEnforced").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Reject_WithoutReason_Returns400()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        (await ReasonedAsync(id, "reject", "")).Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_OwnPending_Works_ThenTheRepCanAskAgain()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.RepId, "SalesRep");

        var r = await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/cancel", new { rowVersion = 1u });

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("status").GetString().Should().Be("Cancelled");
        r.Data.GetProperty("cancelledAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        (await CreateAsync()).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancel_OtherRepsRequest_IsForbidden()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.OtherRepId, "SalesRep");

        var r = await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/cancel", new { rowVersion = 1u });

        r.Status.Should().Be(HttpStatusCode.Forbidden, r.Raw);
    }

    [Fact]
    public async Task Cancel_SupervisorRole_IsForbidden()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        (await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/cancel", new { rowVersion = 1u }))
            .Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_AlreadyApproved_Returns409()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        await ApproveAsync(id);
        As(s.RepId, "SalesRep");

        var r = await SendAsync(HttpMethod.Post, $"{BaseUrl}/{id}/cancel", new { rowVersion = 1u });

        r.Status.Should().Be(HttpStatusCode.Conflict, r.Raw);
        r.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    [Fact]
    public async Task Revoke_PendingRequest_Returns409()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");

        var r = await ReasonedAsync(id, "revoke", "changed my mind");

        r.Status.Should().Be(HttpStatusCode.Conflict, r.Raw);
        r.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    // ── Effect on outlet sync ────────────────────────────────────────────

    [Fact]
    public async Task OutletSync_BeforeApproval_IsEnforced()
    {
        var s = await SeedScenarioAsync();
        await RepCreatesAsync(s);

        var sync = await OutletSyncAsync(s.RouteId);

        sync.GetProperty("geofenceEnforced").GetBoolean().Should().BeTrue();
        sync.GetProperty("exemptionReason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task OutletSync_AfterApproval_RelaxesOnlyTheApprovedRoute_ThenRevokeRestoresIt()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        (await ApproveAsync(id)).Status.Should().Be(HttpStatusCode.OK);

        As(s.RepId, "SalesRep");
        var approved = await OutletSyncAsync(s.RouteId);
        approved.GetProperty("geofenceEnforced").GetBoolean().Should().BeFalse();
        approved.GetProperty("exemptionReason").GetString().Should().Be("RouteUnlock");
        approved.GetProperty("geofenceEnforcedFrom").GetDateTime().AsUtc()
            .Should().Be(SriLankaTime.StartOfDayUtc(SriLankaTime.Today.AddDays(1)));

        // The same rep on a different route is not relaxed.
        var elsewhere = await OutletSyncAsync(s.OtherRouteId);
        elsewhere.GetProperty("geofenceEnforced").GetBoolean().Should().BeTrue();
        elsewhere.GetProperty("exemptionReason").ValueKind.Should().Be(JsonValueKind.Null);

        // Another rep asking about the approved route is not relaxed either.
        As(s.OtherRepId, "SalesRep");
        (await OutletSyncAsync(s.RouteId)).GetProperty("geofenceEnforced").GetBoolean().Should().BeTrue();

        As(s.SupervisorId, "Supervisor");
        var revoke = await ReasonedAsync(id, "revoke", "Misused");
        revoke.Status.Should().Be(HttpStatusCode.OK, revoke.Raw);
        revoke.Data.GetProperty("status").GetString().Should().Be("Revoked");
        revoke.Data.GetProperty("revokeReason").GetString().Should().Be("Misused");

        As(s.RepId, "SalesRep");
        var afterRevoke = await OutletSyncAsync(s.RouteId);
        afterRevoke.GetProperty("geofenceEnforced").GetBoolean().Should().BeTrue();
        afterRevoke.GetProperty("exemptionReason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_WhileAdminExemptionIsLive_Returns422AlreadyExempt()
    {
        var s = await SeedScenarioAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserProximityExemptions.Add(new sfa_api.Features.UserProximityExemptions.Entities.UserProximityExemption
            {
                UserId = s.RepId, ValidFrom = DateTime.UtcNow.AddMinutes(-1), ValidTo = DateTime.UtcNow.AddDays(1),
                Reason = sfa_api.Features.UserProximityExemptions.Entities.ProximityExemptionReason.BadOutletCoordinates,
                GrantedByUserId = s.AdminId, IsActive = true
            });
            await db.SaveChangesAsync();
        }
        As(s.RepId, "SalesRep");

        var r = await CreateAsync();

        r.Status.Should().Be(HttpStatusCode.UnprocessableEntity, r.Raw);
        r.ErrorCode.Should().Be("ROUTE_UNLOCK_ALREADY_EXEMPT");
    }

    // ── Detail ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ShowsRequestedThenApprovedTimeline()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        await ApproveAsync(id, "fine");

        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        var events = r.Data.GetProperty("events").EnumerateArray().ToList();
        events.Select(e => e.GetProperty("action").GetString()).Should().Equal("Requested", "Approved");
        events[0].GetProperty("performedByUserId").GetInt32().Should().Be(s.RepId);
        events[0].GetProperty("performedByRole").GetString().Should().Be("SalesRep");
        events[1].GetProperty("performedByUserId").GetInt32().Should().Be(s.SupervisorId);
        events[1].GetProperty("fromStatus").GetString().Should().Be("Pending");
        events[1].GetProperty("toStatus").GetString().Should().Be("Approved");
        events[1].GetProperty("note").GetString().Should().Be("fine");
        r.Data.GetProperty("request").GetProperty("status").GetString().Should().Be("Approved");
        r.Data.GetProperty("bills").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Detail_AccessRules_OwnRepYes_OtherRepNo_OtherSupervisorNo()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);

        As(s.RepId, "SalesRep");
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}")).Status.Should().Be(HttpStatusCode.OK);

        As(s.OtherRepId, "SalesRep");
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}")).Status.Should().Be(HttpStatusCode.Forbidden);

        As(s.OtherSupervisorId, "Supervisor");
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}")).Status.Should().Be(HttpStatusCode.Forbidden);

        As(s.AdminId, "Admin");
        (await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}")).Status.Should().Be(HttpStatusCode.OK);
    }

    // ── Bill-create gate ─────────────────────────────────────────────────

    private async Task<Reply> PostBillAsync(int outletId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/billings")
        {
            Content = JsonContent.Create(new
            {
                outletId,
                billDiscountRate = 0m,
                notes = "route unlock bill",
                billingDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                latitude = FarLat,
                longitude = FarLng,
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

    [Fact]
    public async Task Bill_OutOfRangeBeforeApproval_IsRefused()
    {
        var s = await SeedScenarioAsync();
        As(s.RepId, "SalesRep");

        var r = await PostBillAsync(s.OutletId);

        r.Status.Should().Be(HttpStatusCode.UnprocessableEntity, r.Raw);
        r.ErrorCode.Should().Be("OUTLET_OUT_OF_RANGE");
    }

    [Fact]
    public async Task Bill_OutOfRangeOnApprovedRoute_IsAcceptedStampedAndListedOnDetail_ButOtherRouteStillRefused()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        (await ApproveAsync(id)).Status.Should().Be(HttpStatusCode.OK);

        As(s.RepId, "SalesRep");
        var ok = await PostBillAsync(s.OutletId);
        ok.Status.Should().Be(HttpStatusCode.Created, ok.Raw);
        ok.Data.GetProperty("proximityOverridden").GetBoolean().Should().BeTrue();
        var billingId = ok.Data.GetProperty("id").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bill = await db.Billings.AsNoTracking().FirstAsync(b => b.Id == billingId);
            bill.RouteUnlockRequestId.Should().Be(id);
            bill.ProximityExemptionId.Should().BeNull();
            bill.ProximityOverridden.Should().BeTrue();
        }

        // An outlet on a route the unlock does not cover is still gated.
        var refused = await PostBillAsync(s.OtherRouteOutletId);
        refused.Status.Should().Be(HttpStatusCode.UnprocessableEntity, refused.Raw);
        refused.ErrorCode.Should().Be("OUTLET_OUT_OF_RANGE");

        As(s.AdminId, "Admin");
        var detail = await SendAsync(HttpMethod.Get, $"{BaseUrl}/{id}");
        var bills = detail.Data.GetProperty("bills").EnumerateArray().ToList();
        bills.Should().ContainSingle();
        bills[0].GetProperty("billingId").GetInt32().Should().Be(billingId);
        bills[0].GetProperty("outletId").GetInt32().Should().Be(s.OutletId);
    }

    [Fact]
    public async Task Bill_AfterRevoke_IsRefusedAgain()
    {
        var s = await SeedScenarioAsync();
        var (id, _) = await RepCreatesAsync(s);
        As(s.SupervisorId, "Supervisor");
        await ApproveAsync(id);
        (await ReasonedAsync(id, "revoke", "Enough")).Status.Should().Be(HttpStatusCode.OK);

        As(s.RepId, "SalesRep");
        var r = await PostBillAsync(s.OutletId);

        r.Status.Should().Be(HttpStatusCode.UnprocessableEntity, r.Raw);
        r.ErrorCode.Should().Be("OUTLET_OUT_OF_RANGE");
    }
}

internal static class DateTimeTestExtensions
{
    public static DateTime AsUtc(this DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
