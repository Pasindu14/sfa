using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.UserAssignments;

[Collection(SfaApiCollection.Name)]
public class UserAssignmentsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public UserAssignmentsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];

    private async Task<int> CreateSalesRepAsync(string prefix = "Rep")
    {
        SetToken(AuthHelper.AdminToken);
        var uid = Uid();
        var payload = new
        {
            name = $"{prefix} {uid}",
            username = $"{prefix.ToLower()}_{uid}",
            email = $"{prefix.ToLower()}_{uid}@test.com",
            phone = $"+94{Math.Abs(uid.GetHashCode() % 100000000):D8}",
            password = "Password1!",
            role = "SalesRep",
            deviceId = $"device_{uid}"
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/users", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding {prefix} user must succeed");
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateRegionAsync(string name)
    {
        SetToken(AuthHelper.AdminToken);
        var resp = await _client.PostAsJsonAsync("/api/v1/regions", new { name });
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding region '{name}' must succeed");
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateAreaAsync(string name, int regionId)
    {
        SetToken(AuthHelper.AdminToken);
        var resp = await _client.PostAsJsonAsync("/api/v1/areas", new { name, regionId });
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding area '{name}' must succeed");
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateTerritoryAsync(string name, int areaId)
    {
        SetToken(AuthHelper.AdminToken);
        var resp = await _client.PostAsJsonAsync("/api/v1/territories", new { name, areaId });
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding territory '{name}' must succeed");
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDivisionAsync(string name, int territoryId)
    {
        SetToken(AuthHelper.AdminToken);
        var resp = await _client.PostAsJsonAsync("/api/v1/divisions", new { name, territoryId });
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding division '{name}' must succeed");
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    /// <summary>Seeds the full geo hierarchy and returns a division ID.</summary>
    private async Task<int> SeedDivisionAsync()
    {
        var uid = Uid();
        var regionId = await CreateRegionAsync($"UA Region {uid}");
        var areaId = await CreateAreaAsync($"UA Area {uid}", regionId);
        var territoryId = await CreateTerritoryAsync($"UA Territory {uid}", areaId);
        return await CreateDivisionAsync($"UA Division {uid}", territoryId);
    }

    /// <summary>Seeds the full geo hierarchy and returns all IDs.</summary>
    private async Task<(int RegionId, int AreaId, int TerritoryId, int DivisionId)> SeedFullHierarchyAsync()
    {
        var uid = Uid();
        var regionId = await CreateRegionAsync($"UA Region {uid}");
        var areaId = await CreateAreaAsync($"UA Area {uid}", regionId);
        var territoryId = await CreateTerritoryAsync($"UA Territory {uid}", areaId);
        var divisionId = await CreateDivisionAsync($"UA Division {uid}", territoryId);
        return (regionId, areaId, territoryId, divisionId);
    }

    private static object AssignPayload(int userId, int managerId, int? divisionId = null,
        string effectiveFrom = "2026-03-26")
        => new { userId, reportsToUserId = managerId, divisionId, effectiveFrom };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/user-assignments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/user-assignments/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/user-assignments/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/v1/user-assignments/1",
            new { reportsToUserId = 2, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.DeleteAsync("/api/v1/user-assignments/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments", AssignPayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments", AssignPayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PutAsJsonAsync("/api/v1/user-assignments/1",
            new { reportsToUserId = 2, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.DeleteAsync("/api/v1/user-assignments/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // Stats endpoint
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_AsAdmin_Returns200WithStatFields()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/user-assignments/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = body.GetProperty("data");
        data.TryGetProperty("totalAssignments", out _).Should().BeTrue();
        data.TryGetProperty("activeAssignments", out _).Should().BeTrue();
        data.TryGetProperty("activeTerritories", out _).Should().BeTrue();
        data.TryGetProperty("assignmentsThisMonth", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET all — envelope structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/user-assignments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("userAssignments", out _).Should().BeTrue();
        data.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // CRUD happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithAllGeoIds_Returns201WithStoredIds()
    {
        var userId = await CreateSalesRepAsync("AsgCreate");
        var (regionId, areaId, territoryId, divisionId) = await SeedFullHierarchyAsync();

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            new { userId, regionId, areaId, territoryId, divisionId, effectiveFrom = "2026-03-26" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = body.GetProperty("data");
        data.GetProperty("userId").GetInt32().Should().Be(userId);
        data.GetProperty("divisionId").GetInt32().Should().Be(divisionId);
        data.GetProperty("territoryId").GetInt32().Should().Be(territoryId);
        data.GetProperty("areaId").GetInt32().Should().Be(areaId);
        data.GetProperty("regionId").GetInt32().Should().Be(regionId);
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithoutDivision_Returns201WithNullGeoFields()
    {
        var userId = await CreateSalesRepAsync("AsgNoDivRep");
        var managerId = await CreateSalesRepAsync("AsgNoDivMgr");

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, managerId, divisionId: null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");
        data.GetProperty("divisionId").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetProperty("territoryId").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetProperty("regionId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetById_AfterCreate_Returns200WithCorrectData()
    {
        var userId = await CreateSalesRepAsync("AsgGetById");
        var managerId = await CreateSalesRepAsync("AsgGetByIdMgr");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, managerId));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var getResp = await _client.GetAsync($"/api/v1/user-assignments/{id}");

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("data").GetProperty("userId").GetInt32().Should().Be(userId);
    }

    [Fact]
    public async Task Update_ValidPayload_Returns200WithUpdatedData()
    {
        var userId = await CreateSalesRepAsync("AsgUpdate");
        var (regionId, areaId, territoryId, divisionId) = await SeedFullHierarchyAsync();

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            new { userId, regionId, areaId, territoryId, divisionId, effectiveFrom = "2026-03-26" });
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/user-assignments/{id}",
            new { divisionId = (int?)null, effectiveFrom = "2026-04-01" });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("divisionId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("data").GetProperty("effectiveFrom").GetString().Should().Be("2026-04-01");
    }

    [Fact]
    public async Task Delete_ExistingAssignment_Returns204()
    {
        var userId = await CreateSalesRepAsync("AsgDelete");
        var managerId = await CreateSalesRepAsync("AsgDeleteMgr");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, managerId));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var deleteResp = await _client.DeleteAsync($"/api/v1/user-assignments/{id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeactivatesBothGeoAndRl()
    {
        var userId = await CreateSalesRepAsync("AsgDelBoth");
        var managerId = await CreateSalesRepAsync("AsgDelBothMgr");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, managerId));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.DeleteAsync($"/api/v1/user-assignments/{id}");

        // After delete, GET returns the record but IsActive must be false
        var getResp = await _client.GetAsync($"/api/v1/user-assignments/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // The reporting line for this user should also be inactive
        var rlResp = await _client.GetAsync(
            $"/api/v1/user-reporting-lines?isActive=true");
        var rlBody = await rlResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var rlItems = rlBody.GetProperty("data").GetProperty("userReportingLines").EnumerateArray().ToList();
        rlItems.Should().NotContain(el => el.GetProperty("userId").GetInt32() == userId);
    }

    // ─────────────────────────────────────────────────
    // Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/user-assignments/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync("/api/v1/user-assignments/99999",
            new { reportsToUserId = 1, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.DeleteAsync("/api/v1/user-assignments/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Validation (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_ZeroUserId_Returns400WithValidationError()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { userId = 0, reportsToUserId = 1, effectiveFrom = "2026-03-26" };

        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        body.GetProperty("error").GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task Create_MinimalPayload_Returns201()
    {
        var userId = await CreateSalesRepAsync("MinAsgRep");
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            new { userId, effectiveFrom = "2026-03-26" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ─────────────────────────────────────────────────
    // Business rules
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AdminUserAsSubordinate_Returns422()
    {
        var managerId = await CreateSalesRepAsync("AdminSubMgr");
        SetToken(AuthHelper.AdminToken);
        // userId=1 is the seeded admin user
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId: 1, managerId: managerId));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("USER_ROLE_NOT_ASSIGNABLE");
    }

    [Fact]
    public async Task Create_SecondAssignmentForSameUser_DeactivatesPreviousGeoAssignment()
    {
        var userId = await CreateSalesRepAsync("DupAsgRep");
        var mgr1 = await CreateSalesRepAsync("DupAsgMgr1");
        var mgr2 = await CreateSalesRepAsync("DupAsgMgr2");

        SetToken(AuthHelper.AdminToken);

        // First assignment
        var firstResp = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, mgr1));
        var firstId = (await firstResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Second assignment for same user
        await _client.PostAsJsonAsync("/api/v1/user-assignments",
            AssignPayload(userId, mgr2));

        // First geo assignment should be inactive
        var getFirst = await _client.GetAsync($"/api/v1/user-assignments/{firstId}");
        var firstBody = await getFirst.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        firstBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Create_NonExistentDivision_Returns404()
    {
        var userId = await CreateSalesRepAsync("BadDivRep");

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-assignments",
            new { userId, divisionId = 99999, effectiveFrom = "2026-03-26" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }
}
