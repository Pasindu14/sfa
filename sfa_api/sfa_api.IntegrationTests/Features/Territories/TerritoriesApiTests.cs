using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Territories;

[Collection(SfaApiCollection.Name)]
public class TerritoriesApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public TerritoriesApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a Region, then an Area within it via the API and returns the area id.
    /// Territories have a FK constraint to Areas, so every test that creates territory
    /// data must seed an area (and its parent region) first.
    /// </summary>
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

    private static object CreateTerritoryPayload(string name, int areaId)
        => new { name, areaId };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTerritories_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/territories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTerritoryById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/territories/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActiveTerritories_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/territories/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTerritory_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Test Territory", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTerritory_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/territories/1", CreateTerritoryPayload("Test Territory", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateTerritory_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/territories/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateTerritory_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/territories/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTerritory_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Test Territory", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTerritory_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Test Territory", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTerritory_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/territories/1", CreateTerritoryPayload("Test Territory", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateTerritory_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/territories/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateTerritory_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/territories/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/territories — any authenticated role
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTerritories_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllTerritories_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/territories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllTerritories_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/territories — status filter
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTerritories_StatusActive_ReturnsOnlyActiveTerritories()
    {
        var regionId = await CreateRegionAsync("Region For Territory Status Active Test");
        var areaId = await CreateAreaAsync("Area For Territory Status Active Test", regionId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Active Territory Status Test", areaId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Inactive Territory Status Test", areaId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/territories/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/territories?status=active&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var territories = body.GetProperty("data").GetProperty("territories");

        foreach (var territory in territories.EnumerateArray())
            territory.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = territories.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetAllTerritories_StatusInactive_ReturnsOnlyInactiveTerritories()
    {
        var regionId = await CreateRegionAsync("Region For Territory Status Inactive Test");
        var areaId = await CreateAreaAsync("Area For Territory Status Inactive Test", regionId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Active Territory For Inactive Filter", areaId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Inactive Territory For Inactive Filter", areaId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/territories/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/territories?status=inactive&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var territories = body.GetProperty("data").GetProperty("territories");

        foreach (var territory in territories.EnumerateArray())
            territory.GetProperty("isActive").GetBoolean().Should().BeFalse();

        var ids = territories.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(inactiveId);
        ids.Should().NotContain(activeId);
    }

    [Fact]
    public async Task GetAllTerritories_StatusOmitted_ReturnsAllTerritories()
    {
        var regionId = await CreateRegionAsync("Region For Territory Status Omitted Test");
        var areaId = await CreateAreaAsync("Area For Territory Status Omitted Test", regionId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Active Territory Status Omitted", areaId));
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Inactive Territory Status Omitted", areaId));
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/territories/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/territories?pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("territories")
            .EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(activeId);
        ids.Should().Contain(inactiveId);
    }

    [Fact]
    public async Task GetAllTerritories_FilteredByAreaId_ReturnsOnlyTerritoriesInThatArea()
    {
        var regionId = await CreateRegionAsync("Region For Territory Area Filter Test");
        var areaAId = await CreateAreaAsync("Area A For Territory Filter Test", regionId);
        var areaBId = await CreateAreaAsync("Area B For Territory Filter Test", regionId);

        var terrInAResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Territory In Area A Filter Test", areaAId));
        var terrInAId = (await terrInAResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var terrInBResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Territory In Area B Filter Test", areaBId));
        var terrInBId = (await terrInBResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/territories?areaId={areaAId}&pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("territories")
            .EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(terrInAId);
        ids.Should().NotContain(terrInBId);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/territories — Create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTerritory_AsAdmin_Returns201AndCanGetById()
    {
        var regionId = await CreateRegionAsync("Region For Create Territory Test");
        var areaId = await CreateAreaAsync("Area For Create Territory Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Western Territory", areaId));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var territoryId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        territoryId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/v1/territories/{territoryId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Territory");
        getBody.GetProperty("data").GetProperty("areaId").GetInt32().Should().Be(areaId);
    }

    [Fact]
    public async Task CreateTerritory_AsAdmin_SetsIsActiveTrue()
    {
        var regionId = await CreateRegionAsync("Region For Territory IsActive Test");
        var areaId = await CreateAreaAsync("Area For Territory IsActive Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Central Territory IsActive Test", areaId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateTerritory_AsAdmin_Returns201WithLocationHeader()
    {
        var regionId = await CreateRegionAsync("Region For Territory Location Header Test");
        var areaId = await CreateAreaAsync("Area For Territory Location Header Test", regionId);

        var response = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Southern Territory Location Test", areaId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/territories/");
    }

    [Fact]
    public async Task CreateTerritory_AsAdmin_ResponseIncludesAllFields()
    {
        var regionId = await CreateRegionAsync("Region For Territory All Fields Test");
        var areaId = await CreateAreaAsync("Area For Territory All Fields Test", regionId);

        var response = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Eastern Territory Fields Test", areaId));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Territory Fields Test");
        data.TryGetProperty("areaId", out _).Should().BeTrue();
        data.TryGetProperty("areaName", out _).Should().BeTrue();
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTerritory_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", areaId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/territories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateTerritory_NameExceedsMaxLength_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = new string('T', 101), areaId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/territories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateTerritory_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", areaId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/territories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task CreateTerritory_InvalidAreaId_Returns400WithAreaIdFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Valid Territory Name", areaId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/territories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("AreaId", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Business rule failures (404 / 409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTerritory_AreaNotFound_Returns404WithAreaNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Territory With Ghost Area", areaId = 999999 };
        var response = await _client.PostAsJsonAsync("/api/v1/territories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task CreateTerritory_DuplicateNameInSameArea_Returns409WithNameDuplicateCode()
    {
        var regionId = await CreateRegionAsync("Region For Duplicate Territory Test");
        var areaId = await CreateAreaAsync("Area For Duplicate Territory Test", regionId);

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Duplicate Territory Alpha", areaId));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Duplicate Territory Alpha", areaId));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/territories/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTerritoryById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("TERRITORY_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/territories/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTerritory_AsAdmin_Returns200WithUpdatedData()
    {
        var regionId = await CreateRegionAsync("Region For Update Territory Test");
        var areaId = await CreateAreaAsync("Area For Update Territory Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Before Update Territory", areaId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "After Update Territory", areaId, rowVersion = 1 };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/territories/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Territory");
    }

    [Fact]
    public async Task UpdateTerritory_NonExistent_Returns404()
    {
        var regionId = await CreateRegionAsync("Region For Update NonExistent Territory Test");
        var areaId = await CreateAreaAsync("Area For Update NonExistent Territory Test", regionId);

        var response = await _client.PutAsJsonAsync("/api/v1/territories/99999", new { name = "Ghost Territory", areaId, rowVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTerritory_InvalidData_Returns400()
    {
        var regionId = await CreateRegionAsync("Region For Update Invalid Territory Test");
        var areaId = await CreateAreaAsync("Area For Update Invalid Territory Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Valid Territory For Invalid Update", areaId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new { name = "", areaId };
        var response = await _client.PutAsJsonAsync($"/api/v1/territories/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateTerritory_DuplicateNameOfOtherRecordInSameArea_Returns409()
    {
        var regionId = await CreateRegionAsync("Region For Conflict Update Territory Test");
        var areaId = await CreateAreaAsync("Area For Conflict Update Territory Test", regionId);

        await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Conflict Territory A", areaId));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Conflict Territory B", areaId));
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Conflict Territory A", areaId, rowVersion = 1 };
        var response = await _client.PutAsJsonAsync($"/api/v1/territories/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateTerritory_SameNameAsOwnRecord_Returns200()
    {
        var regionId = await CreateRegionAsync("Region For Idempotent Territory Update Test");
        var areaId = await CreateAreaAsync("Area For Idempotent Territory Update Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Idempotent Territory", areaId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Idempotent Territory", areaId, rowVersion = 1 };
        var response = await _client.PutAsJsonAsync($"/api/v1/territories/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/territories/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        var regionId = await CreateRegionAsync("Region For Toggle Territory Test");
        var areaId = await CreateAreaAsync("Area For Toggle Territory Test", regionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Toggle Territory", areaId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/territories/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/territories/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/territories/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/territories/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateTerritory_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/territories/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateTerritory_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/territories/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/territories/active
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveTerritories_AsAdmin_Returns200WithSuccessEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveTerritories_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/territories/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveTerritories_DataIsArray()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetActiveTerritories_ReturnsOnlyActiveTerritories()
    {
        var regionId = await CreateRegionAsync("Region For Active Territories Only Test");
        var areaId = await CreateAreaAsync("Area For Active Territories Only Test", regionId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Active Territory For Active Filter Test", areaId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Inactive Territory For Active Filter Test", areaId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/territories/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/territories/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        foreach (var item in data.EnumerateArray())
            item.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = data.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetActiveTerritories_FilteredByAreaId_ReturnsOnlyActiveTerritoriesInThatArea()
    {
        var regionId = await CreateRegionAsync("Region For Active Territory Area Filter Test");
        var areaXId = await CreateAreaAsync("Area X For Active Territory Filter Test", regionId);
        var areaYId = await CreateAreaAsync("Area Y For Active Territory Filter Test", regionId);

        var terrXResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Territory X Active Area Filter Test", areaXId));
        var terrXId = (await terrXResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var terrYResp = await _client.PostAsJsonAsync("/api/v1/territories", CreateTerritoryPayload("Territory Y Active Area Filter Test", areaYId));
        var terrYId = (await terrYResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/territories/active?areaId={areaXId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data")
            .EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(terrXId);
        ids.Should().NotContain(terrYId);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/territories/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
