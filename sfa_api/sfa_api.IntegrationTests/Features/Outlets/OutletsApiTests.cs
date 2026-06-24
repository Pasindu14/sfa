using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Outlets;

[Collection(SfaApiCollection.Name)]
public class OutletsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OutletsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Seed helpers ──────────────────────────────────────────────────────

    private async Task<int> CreateRegionAsync(string name)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/regions", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding region '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateAreaAsync(string name, int regionId)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/areas", new { name, regionId });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding area '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateTerritoryAsync(string name, int areaId)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/territories", new { name, areaId });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding territory '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDivisionAsync(string name, int territoryId)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", new { name, territoryId });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding division '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateRouteAsync(string name, int divisionId)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/routes",
            new { name, divisionId, pinColor = "#FF5733" });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding route '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateOutletAsync(string name, int routeId, string nicNo, string outletType = "Medium", string outletCategory = "Wholesale")
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", new
        {
            name, address = "123 Test Street", tel = "0771234567", nicNo,
            latitude = 6.9271, longitude = 79.8612,
            outletType, outletCategory,
            provinceCode = 1, districtCode = 11, routeId
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding outlet '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<uint> GetOutletRowVersionAsync(int id)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync($"/api/v1/outlets/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"reading rowVersion for outlet {id} must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("rowVersion").GetUInt32();
    }

    private static object CreateOutletPayload(string name, int routeId, string nicNo, string outletType = "Medium", string outletCategory = "Wholesale")
        => new
        {
            name, address = "123 Test Street", tel = "0771234567", nicNo,
            latitude = 6.9271, longitude = 79.8612,
            outletType, outletCategory,
            provinceCode = 1, districtCode = 11, routeId
        };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOutlets_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/outlets");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOutletById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/outlets/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOutlet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", CreateOutletPayload("Test", 1, "111111111V"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOutlet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/v1/outlets/1", CreateOutletPayload("Test", 1, "111111111V"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteOutlet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.DeleteAsync("/api/v1/outlets/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateOutlet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/1/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateOutlet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/1/deactivate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write ops
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOutlet_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", CreateOutletPayload("Test", 1, "111111111V"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOutlet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PutAsJsonAsync("/api/v1/outlets/1", CreateOutletPayload("Test", 1, "111111111V"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteOutlet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.DeleteAsync("/api/v1/outlets/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateOutlet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/1/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateOutlet_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/1/deactivate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/outlets
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllOutlets_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/outlets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllOutlets_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.GetAsync("/api/v1/outlets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllOutlets_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/outlets?page=1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllOutlets_WithStatusActive_ReturnsOnlyActiveOutlets()
    {
        var regionId = await CreateRegionAsync("Region For Outlet Status Active Test");
        var areaId = await CreateAreaAsync("Area For Outlet Status Active Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Outlet Status Active Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Outlet Status Active Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Outlet Status Active Test", divisionId);

        var activeId = await CreateOutletAsync("Active Outlet StatusTest UniqueOT1", routeId, "901001001V");
        var inactiveId = await CreateOutletAsync("Inactive Outlet StatusTest UniqueOT1", routeId, "901001002V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{inactiveId}/deactivate", new { });

        var response = await _client.GetAsync("/api/v1/outlets?status=active&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var outlets = body.GetProperty("data").GetProperty("outlets").EnumerateArray().ToList();
        outlets.Should().Contain(o => o.GetProperty("id").GetInt32() == activeId);
        outlets.Should().NotContain(o => o.GetProperty("id").GetInt32() == inactiveId);
    }

    [Fact]
    public async Task GetAllOutlets_WithStatusInactive_ReturnsOnlyInactiveOutlets()
    {
        var regionId = await CreateRegionAsync("Region For Outlet Status Inactive Test");
        var areaId = await CreateAreaAsync("Area For Outlet Status Inactive Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Outlet Status Inactive Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Outlet Status Inactive Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Outlet Status Inactive Test", divisionId);

        var activeId = await CreateOutletAsync("Active Outlet InactiveFilter UniqueOT2", routeId, "902001001V");
        var inactiveId = await CreateOutletAsync("Inactive Outlet InactiveFilter UniqueOT2", routeId, "902001002V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{inactiveId}/deactivate", new { });

        var response = await _client.GetAsync("/api/v1/outlets?status=inactive&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var outlets = body.GetProperty("data").GetProperty("outlets").EnumerateArray().ToList();
        outlets.Should().Contain(o => o.GetProperty("id").GetInt32() == inactiveId);
        outlets.Should().NotContain(o => o.GetProperty("id").GetInt32() == activeId);
    }

    [Fact]
    public async Task GetAllOutlets_WithSearchParam_ReturnsFilteredResults()
    {
        var regionId = await CreateRegionAsync("Region For Outlet Search Test");
        var areaId = await CreateAreaAsync("Area For Outlet Search Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Outlet Search Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Outlet Search Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Outlet Search Test", divisionId);

        await CreateOutletAsync("Searchable Outlet UniqueXYZ789", routeId, "903001001V");
        await CreateOutletAsync("Other Outlet Search Test", routeId, "903001002V");

        var response = await _client.GetAsync("/api/v1/outlets?search=UniqueXYZ789&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var outlets = body.GetProperty("data").GetProperty("outlets");
        outlets.EnumerateArray().Should().Contain(o => o.GetProperty("name").GetString()!.Contains("UniqueXYZ789"));
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/outlets/active
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveOutlets_Returns200WithOnlyActiveOutlets()
    {
        var regionId = await CreateRegionAsync("Region For Active Outlets Endpoint Test");
        var areaId = await CreateAreaAsync("Area For Active Outlets Endpoint Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Active Outlets Endpoint Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Active Outlets Endpoint Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Active Outlets Endpoint Test", divisionId);

        var activeId = await CreateOutletAsync("Active For Active Endpoint UniqueOT3", routeId, "904001001V");
        var inactiveId = await CreateOutletAsync("Inactive For Active Endpoint UniqueOT3", routeId, "904001002V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{inactiveId}/deactivate", new { });

        var response = await _client.GetAsync("/api/v1/outlets/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var outlets = body.GetProperty("data").EnumerateArray().ToList();
        outlets.Should().Contain(o => o.GetProperty("id").GetInt32() == activeId);
        outlets.Should().NotContain(o => o.GetProperty("id").GetInt32() == inactiveId);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/outlets — Create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOutlet_AsAdmin_Returns201AndCanGetById()
    {
        var regionId = await CreateRegionAsync("Region For Create Outlet Test");
        var areaId = await CreateAreaAsync("Area For Create Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Create Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Create Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Create Outlet Test", divisionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/outlets", new
        {
            name = "Sunrise Pharmacy",
            address = "123 Main Street",
            tel = "0771234567",
            nicNo = "905001001V",
            email = "sunrise@pharmacy.lk",
            creditLimit = 10000,
            latitude = 6.9271,
            longitude = 79.8612,
            outletType = "Large",
            outletCategory = "Wholesale",
            billingPriceType = "DealerPrice",
            provinceCode = 1,
            districtCode = 11,
            routeId
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var outletId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        outletId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/v1/outlets/{outletId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Sunrise Pharmacy");
        getBody.GetProperty("data").GetProperty("address").GetString().Should().Be("123 Main Street");
        getBody.GetProperty("data").GetProperty("nicNo").GetString().Should().Be("905001001V");
    }

    [Fact]
    public async Task CreateOutlet_AsAdmin_SetsIsActiveTrue()
    {
        var regionId = await CreateRegionAsync("Region For Create Outlet IsActive Test");
        var areaId = await CreateAreaAsync("Area For Create Outlet IsActive Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Create Outlet IsActive Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Create Outlet IsActive Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Create Outlet IsActive Test", divisionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/outlets",
            CreateOutletPayload("IsActive Outlet Test", routeId, "906001001V"));

        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateOutlet_AsAdmin_Returns201WithLocationHeader()
    {
        var regionId = await CreateRegionAsync("Region For Outlet Location Header Test");
        var areaId = await CreateAreaAsync("Area For Outlet Location Header Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Outlet Location Header Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Outlet Location Header Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Outlet Location Header Test", divisionId);

        var response = await _client.PostAsJsonAsync("/api/v1/outlets",
            CreateOutletPayload("Outlet Location Header Test", routeId, "907001001V"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/outlets/");
    }

    [Fact]
    public async Task CreateOutlet_AsAdmin_ResponseIncludesAllAncestorFields()
    {
        var regionId = await CreateRegionAsync("Region For Outlet All Fields Test");
        var areaId = await CreateAreaAsync("Area For Outlet All Fields Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Outlet All Fields Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Outlet All Fields Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Outlet All Fields Test", divisionId);

        var response = await _client.PostAsJsonAsync("/api/v1/outlets",
            CreateOutletPayload("Outlet All Fields Test", routeId, "908001001V"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.TryGetProperty("name", out _).Should().BeTrue();
        data.TryGetProperty("address", out _).Should().BeTrue();
        data.TryGetProperty("nicNo", out _).Should().BeTrue();
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("routeId", out _).Should().BeTrue();
        data.TryGetProperty("routeName", out _).Should().BeTrue();
        data.TryGetProperty("divisionId", out _).Should().BeTrue();
        data.TryGetProperty("divisionName", out _).Should().BeTrue();
        data.TryGetProperty("territoryId", out _).Should().BeTrue();
        data.TryGetProperty("territoryName", out _).Should().BeTrue();
        data.TryGetProperty("areaId", out _).Should().BeTrue();
        data.TryGetProperty("areaName", out _).Should().BeTrue();
        data.TryGetProperty("regionId", out _).Should().BeTrue();
        data.TryGetProperty("regionName", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();

        data.GetProperty("routeId").GetInt32().Should().Be(routeId);
        data.GetProperty("divisionId").GetInt32().Should().Be(divisionId);
        data.GetProperty("territoryId").GetInt32().Should().Be(territoryId);
        data.GetProperty("areaId").GetInt32().Should().Be(areaId);
        data.GetProperty("regionId").GetInt32().Should().Be(regionId);
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOutlet_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "", address = "123 Street", tel = "0771234567", nicNo = "100001001V", latitude = 6.9, longitude = 79.8, outletType = "Medium", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11, routeId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateOutlet_InvalidOutletType_Returns400WithOutletTypeFieldError()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "Valid Name", address = "123 Street", tel = "0771234567", nicNo = "100001002V", latitude = 6.9, longitude = 79.8, outletType = "Giant", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11, routeId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("OutletType", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateOutlet_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "", address = "", tel = "", nicNo = "", outletType = "", outletCategory = "", provinceCode = 0, districtCode = 0, routeId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task CreateOutlet_InvalidLatitude_Returns400WithLatitudeFieldError()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "Valid Name", address = "123 Street", tel = "0771234567", nicNo = "100001003V", latitude = 200.0, longitude = 79.8, outletType = "Medium", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11, routeId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Latitude", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Business rule failures (404 / 409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOutlet_RouteNotFound_Returns404WithRouteNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = CreateOutletPayload("Ghost Route Outlet", 999999, "909001001V");
        var response = await _client.PostAsJsonAsync("/api/v1/outlets", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task CreateOutlet_DuplicateNicNo_Returns409WithNicNoDuplicateCode()
    {
        var regionId = await CreateRegionAsync("Region For Duplicate Outlet NicNo Test");
        var areaId = await CreateAreaAsync("Area For Duplicate Outlet NicNo Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Duplicate Outlet NicNo Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Duplicate Outlet NicNo Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Duplicate Outlet NicNo Test", divisionId);

        await CreateOutletAsync("First Outlet NicNo Dup", routeId, "910001001V");

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/outlets",
            CreateOutletPayload("Second Outlet NicNo Dup", routeId, "910001001V"));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NICNO_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/outlets/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOutletById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/outlets/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("OUTLET_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/outlets/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOutlet_AsAdmin_Returns200WithUpdatedData()
    {
        var regionId = await CreateRegionAsync("Region For Update Outlet Test");
        var areaId = await CreateAreaAsync("Area For Update Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Update Outlet Test", divisionId);

        var id = await CreateOutletAsync("Before Update Outlet", routeId, "911001001V");
        var rowVersion = await GetOutletRowVersionAsync(id);

        var updatePayload = new
        {
            name = "After Update Outlet", address = "999 Updated Street", tel = "0779999999",
            nicNo = "911001001V", creditLimit = 5000, latitude = 7.5, longitude = 80.5,
            outletType = "Large", outletCategory = "SMMT", provinceCode = 2, districtCode = 22, routeId,
            rowVersion
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/outlets/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Outlet");
        updateBody.GetProperty("data").GetProperty("outletType").GetString().Should().Be("Large");
        updateBody.GetProperty("data").GetProperty("outletCategory").GetString().Should().Be("SMMT");
    }

    // NOTE: a stale-rowVersion → 409 test is intentionally NOT included here. Integration tests run
    // on SQLite, where TestAppDbContext disables the xmin concurrency token (IsConcurrencyToken(false)),
    // so a stale update returns 200, not 409. The xmin concurrency path is exercised against real
    // PostgreSQL; the round-trip plumbing is covered by the validator unit tests. (review finding #9)

    [Fact]
    public async Task UpdateOutlet_WithNewRoute_ReDenormalizesAncestorIds()
    {
        var regionAId = await CreateRegionAsync("Region A For Route Change Outlet Test");
        var areaAId = await CreateAreaAsync("Area A For Route Change Outlet Test", regionAId);
        var territoryAId = await CreateTerritoryAsync("Territory A For Route Change Outlet Test", areaAId);
        var divisionAId = await CreateDivisionAsync("Division A For Route Change Outlet Test", territoryAId);
        var routeAId = await CreateRouteAsync("Route A For Route Change Outlet Test", divisionAId);

        var regionBId = await CreateRegionAsync("Region B For Route Change Outlet Test");
        var areaBId = await CreateAreaAsync("Area B For Route Change Outlet Test", regionBId);
        var territoryBId = await CreateTerritoryAsync("Territory B For Route Change Outlet Test", areaBId);
        var divisionBId = await CreateDivisionAsync("Division B For Route Change Outlet Test", territoryBId);
        var routeBId = await CreateRouteAsync("Route B For Route Change Outlet Test", divisionBId);

        var id = await CreateOutletAsync("Outlet Before Route Change", routeAId, "912001001V");
        var rowVersion = await GetOutletRowVersionAsync(id);

        var updatePayload = new
        {
            name = "Outlet After Route Change", address = "456 Street", tel = "0771111111",
            nicNo = "912001001V", latitude = 6.0, longitude = 80.0,
            outletType = "Small", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11,
            routeId = routeBId,
            rowVersion
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/outlets/{id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");
        data.GetProperty("routeId").GetInt32().Should().Be(routeBId);
        data.GetProperty("divisionId").GetInt32().Should().Be(divisionBId);
        data.GetProperty("territoryId").GetInt32().Should().Be(territoryBId);
        data.GetProperty("areaId").GetInt32().Should().Be(areaBId);
        data.GetProperty("regionId").GetInt32().Should().Be(regionBId);
    }

    [Fact]
    public async Task UpdateOutlet_DoesNotModifyIsActive()
    {
        var regionId = await CreateRegionAsync("Region For Update IsActive Outlet Test");
        var areaId = await CreateAreaAsync("Area For Update IsActive Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update IsActive Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update IsActive Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Update IsActive Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet Before IsActive Update", routeId, "913001001V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/deactivate", new { });
        var rowVersion = await GetOutletRowVersionAsync(id);

        var updatePayload = new
        {
            name = "Outlet After IsActive Update", address = "123 Street", tel = "0771234567",
            nicNo = "913001001V", latitude = 6.9, longitude = 79.8,
            outletType = "Medium", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11, routeId,
            rowVersion
        };
        await _client.PutAsJsonAsync($"/api/v1/outlets/{id}", updatePayload);

        var getResponse = await _client.GetAsync($"/api/v1/outlets/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOutlet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "Ghost", address = "123 Street", tel = "0771234567", nicNo = "999001001V", latitude = 6.9, longitude = 79.8, outletType = "Medium", outletCategory = "Wholesale", provinceCode = 1, districtCode = 11, routeId = 1, rowVersion = 1u };
        var response = await _client.PutAsJsonAsync("/api/v1/outlets/99999", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOutlet_InvalidData_Returns400()
    {
        var regionId = await CreateRegionAsync("Region For Update Invalid Outlet Test");
        var areaId = await CreateAreaAsync("Area For Update Invalid Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Invalid Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update Invalid Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Update Invalid Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet For Invalid Update", routeId, "914001001V");

        var response = await _client.PutAsJsonAsync($"/api/v1/outlets/{id}",
            new { name = "", address = "", tel = "", nicNo = "", outletType = "", outletCategory = "", provinceCode = 0, districtCode = 0, routeId = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // DELETE /api/v1/outlets/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteOutlet_AsAdmin_Returns204()
    {
        var regionId = await CreateRegionAsync("Region For Delete Outlet Test");
        var areaId = await CreateAreaAsync("Area For Delete Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Delete Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Delete Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Delete Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet To Delete", routeId, "915001001V");

        var response = await _client.DeleteAsync($"/api/v1/outlets/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteOutlet_SoftDelete_OutletNotReturnedInList()
    {
        var regionId = await CreateRegionAsync("Region For Soft Delete Outlet Test");
        var areaId = await CreateAreaAsync("Area For Soft Delete Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Soft Delete Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Soft Delete Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Soft Delete Outlet Test", divisionId);

        var id = await CreateOutletAsync("Soft Delete Outlet UniqueOT999", routeId, "916001001V");
        await _client.DeleteAsync($"/api/v1/outlets/{id}");

        var listResponse = await _client.GetAsync("/api/v1/outlets?pageSize=200&status=active");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = listBody.GetProperty("data").GetProperty("outlets").EnumerateArray()
            .Select(o => o.GetProperty("id").GetInt32()).ToList();
        ids.Should().NotContain(id);
    }

    [Fact]
    public async Task DeleteOutlet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.DeleteAsync("/api/v1/outlets/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/outlets/{id}/activate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateOutlet_AsAdmin_Returns204()
    {
        var regionId = await CreateRegionAsync("Region For Activate Outlet Test");
        var areaId = await CreateAreaAsync("Area For Activate Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Activate Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Activate Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Activate Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet To Activate", routeId, "917001001V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/deactivate", new { });

        var response = await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/activate", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ActivateOutlet_SetsIsActiveTrue_VerifiedByGetById()
    {
        var regionId = await CreateRegionAsync("Region For Activate Verify Outlet Test");
        var areaId = await CreateAreaAsync("Area For Activate Verify Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Activate Verify Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Activate Verify Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Activate Verify Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet Activate Verify", routeId, "918001001V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/deactivate", new { });
        await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/activate", new { });

        var getResponse = await _client.GetAsync($"/api/v1/outlets/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateOutlet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/99999/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/outlets/{id}/deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateOutlet_AsAdmin_Returns204()
    {
        var regionId = await CreateRegionAsync("Region For Deactivate Outlet Test");
        var areaId = await CreateAreaAsync("Area For Deactivate Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Deactivate Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Deactivate Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Deactivate Outlet Test", divisionId);

        var id = await CreateOutletAsync("Outlet To Deactivate", routeId, "919001001V");

        var response = await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/deactivate", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivateOutlet_SetsIsActiveFalse_StillVisibleWithInactiveFilter()
    {
        var regionId = await CreateRegionAsync("Region For Deactivate Visibility Outlet Test");
        var areaId = await CreateAreaAsync("Area For Deactivate Visibility Outlet Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Deactivate Visibility Outlet Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Deactivate Visibility Outlet Test", territoryId);
        var routeId = await CreateRouteAsync("Route For Deactivate Visibility Outlet Test", divisionId);

        var id = await CreateOutletAsync("Deactivated Outlet UniqueOT888", routeId, "920001001V");
        await _client.PostAsJsonAsync($"/api/v1/outlets/{id}/deactivate", new { });

        var getResponse = await _client.GetAsync($"/api/v1/outlets/{id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        var listResponse = await _client.GetAsync("/api/v1/outlets?status=inactive&pageSize=100");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = listBody.GetProperty("data").GetProperty("outlets").EnumerateArray()
            .Select(o => o.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(id);
    }

    [Fact]
    public async Task DeactivateOutlet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/outlets/99999/deactivate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/outlets");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/outlets/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
