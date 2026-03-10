using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Divisions;

[Collection(SfaApiCollection.Name)]
public class DivisionsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public DivisionsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates the full ancestor chain (Region -> Area -> Territory) and returns the territory id.
    /// Every Division test that creates data must seed a territory first.
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

    private async Task<int> CreateTerritoryAsync(string name, int areaId)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/territories", new { name, areaId });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding territory '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static object CreateDivisionPayload(string name, int territoryId)
        => new { name, territoryId };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDivisions_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/divisions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDivisionById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/divisions/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActiveDivisions_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/divisions/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDivision_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Test Division", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDivision_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/divisions/1", CreateDivisionPayload("Test Division", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateDivision_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/divisions/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateDivision_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/divisions/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDivision_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Test Division", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDivision_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Test Division", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDivision_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/divisions/1", CreateDivisionPayload("Test Division", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateDivision_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/divisions/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateDivision_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/divisions/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/divisions — any authenticated role
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDivisions_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllDivisions_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/divisions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllDivisions_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/divisions — status filter
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDivisions_StatusActive_ReturnsOnlyActiveDivisions()
    {
        var regionId = await CreateRegionAsync("Region For Division Status Active Test");
        var areaId = await CreateAreaAsync("Area For Division Status Active Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division Status Active Test", areaId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Active Division Status Test", territoryId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Inactive Division Status Test", territoryId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/divisions/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/divisions?status=active&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var divisions = body.GetProperty("data").GetProperty("divisions");

        foreach (var division in divisions.EnumerateArray())
            division.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = divisions.EnumerateArray().Select(d => d.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetAllDivisions_StatusInactive_ReturnsOnlyInactiveDivisions()
    {
        var regionId = await CreateRegionAsync("Region For Division Status Inactive Test");
        var areaId = await CreateAreaAsync("Area For Division Status Inactive Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division Status Inactive Test", areaId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Active Division For Inactive Filter", territoryId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Inactive Division For Inactive Filter", territoryId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/divisions/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/divisions?status=inactive&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var divisions = body.GetProperty("data").GetProperty("divisions");

        foreach (var division in divisions.EnumerateArray())
            division.GetProperty("isActive").GetBoolean().Should().BeFalse();

        var ids = divisions.EnumerateArray().Select(d => d.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(inactiveId);
        ids.Should().NotContain(activeId);
    }

    [Fact]
    public async Task GetAllDivisions_StatusOmitted_ReturnsAllDivisions()
    {
        var regionId = await CreateRegionAsync("Region For Division Status Omitted Test");
        var areaId = await CreateAreaAsync("Area For Division Status Omitted Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division Status Omitted Test", areaId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Active Division Status Omitted", territoryId));
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Inactive Division Status Omitted", territoryId));
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/divisions/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/divisions?pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("divisions")
            .EnumerateArray()
            .Select(d => d.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(activeId);
        ids.Should().Contain(inactiveId);
    }

    [Fact]
    public async Task GetAllDivisions_FilteredByTerritoryId_ReturnsOnlyDivisionsInThatTerritory()
    {
        var regionId = await CreateRegionAsync("Region For Division Territory Filter Test");
        var areaId = await CreateAreaAsync("Area For Division Territory Filter Test", regionId);
        var territoryAId = await CreateTerritoryAsync("Territory A For Division Filter Test", areaId);
        var territoryBId = await CreateTerritoryAsync("Territory B For Division Filter Test", areaId);

        var divInAResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Division In Territory A Filter Test", territoryAId));
        var divInAId = (await divInAResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var divInBResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Division In Territory B Filter Test", territoryBId));
        var divInBId = (await divInBResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/divisions?territoryId={territoryAId}&pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("divisions")
            .EnumerateArray()
            .Select(d => d.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(divInAId);
        ids.Should().NotContain(divInBId);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/divisions — Create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDivision_AsAdmin_Returns201AndCanGetById()
    {
        var regionId = await CreateRegionAsync("Region For Create Division Test");
        var areaId = await CreateAreaAsync("Area For Create Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Create Division Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Western Division", territoryId));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var divisionId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        divisionId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/v1/divisions/{divisionId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Division");
        getBody.GetProperty("data").GetProperty("territoryId").GetInt32().Should().Be(territoryId);
    }

    [Fact]
    public async Task CreateDivision_AsAdmin_SetsIsActiveTrue()
    {
        var regionId = await CreateRegionAsync("Region For Division IsActive Test");
        var areaId = await CreateAreaAsync("Area For Division IsActive Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division IsActive Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Central Division IsActive Test", territoryId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateDivision_AsAdmin_Returns201WithLocationHeader()
    {
        var regionId = await CreateRegionAsync("Region For Division Location Header Test");
        var areaId = await CreateAreaAsync("Area For Division Location Header Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division Location Header Test", areaId);

        var response = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Southern Division Location Test", territoryId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/divisions/");
    }

    [Fact]
    public async Task CreateDivision_AsAdmin_ResponseIncludesAllAncestorFields()
    {
        var regionId = await CreateRegionAsync("Region For Division All Fields Test");
        var areaId = await CreateAreaAsync("Area For Division All Fields Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Division All Fields Test", areaId);

        var response = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Eastern Division Fields Test", territoryId));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Division Fields Test");
        data.TryGetProperty("territoryId", out _).Should().BeTrue();
        data.TryGetProperty("territoryName", out _).Should().BeTrue();
        data.TryGetProperty("areaId", out _).Should().BeTrue();
        data.TryGetProperty("areaName", out _).Should().BeTrue();
        data.TryGetProperty("regionId", out _).Should().BeTrue();
        data.TryGetProperty("regionName", out _).Should().BeTrue();
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
        // Verify denormalized ancestor IDs are correctly resolved
        data.GetProperty("areaId").GetInt32().Should().Be(areaId);
        data.GetProperty("regionId").GetInt32().Should().Be(regionId);
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDivision_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", territoryId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateDivision_NameExceedsMaxLength_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = new string('D', 101), territoryId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateDivision_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", territoryId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task CreateDivision_InvalidTerritoryId_Returns400WithTerritoryIdFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Valid Division Name", territoryId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("TerritoryId", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Business rule failures (404 / 409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDivision_TerritoryNotFound_Returns404WithTerritoryNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Division With Ghost Territory", territoryId = 999999 };
        var response = await _client.PostAsJsonAsync("/api/v1/divisions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task CreateDivision_DuplicateNameInSameTerritory_Returns409WithNameDuplicateCode()
    {
        var regionId = await CreateRegionAsync("Region For Duplicate Division Test");
        var areaId = await CreateAreaAsync("Area For Duplicate Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Duplicate Division Test", areaId);

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Duplicate Division Alpha", territoryId));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Duplicate Division Alpha", territoryId));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/divisions/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDivisionById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("DIVISION_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/divisions/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDivision_AsAdmin_Returns200WithUpdatedData()
    {
        var regionId = await CreateRegionAsync("Region For Update Division Test");
        var areaId = await CreateAreaAsync("Area For Update Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Division Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Before Update Division", territoryId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "After Update Division", territoryId };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/divisions/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Division");
    }

    [Fact]
    public async Task UpdateDivision_NonExistent_Returns404()
    {
        var regionId = await CreateRegionAsync("Region For Update NonExistent Division Test");
        var areaId = await CreateAreaAsync("Area For Update NonExistent Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update NonExistent Division Test", areaId);

        var response = await _client.PutAsJsonAsync("/api/v1/divisions/99999", new { name = "Ghost Division", territoryId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDivision_InvalidData_Returns400()
    {
        var regionId = await CreateRegionAsync("Region For Update Invalid Division Test");
        var areaId = await CreateAreaAsync("Area For Update Invalid Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Invalid Division Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Valid Division For Invalid Update", territoryId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new { name = "", territoryId };
        var response = await _client.PutAsJsonAsync($"/api/v1/divisions/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateDivision_DuplicateNameOfOtherRecordInSameTerritory_Returns409()
    {
        var regionId = await CreateRegionAsync("Region For Conflict Update Division Test");
        var areaId = await CreateAreaAsync("Area For Conflict Update Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Conflict Update Division Test", areaId);

        await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Conflict Division A", territoryId));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Conflict Division B", territoryId));
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Conflict Division A", territoryId };
        var response = await _client.PutAsJsonAsync($"/api/v1/divisions/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateDivision_SameNameAsOwnRecord_Returns200()
    {
        var regionId = await CreateRegionAsync("Region For Idempotent Division Update Test");
        var areaId = await CreateAreaAsync("Area For Idempotent Division Update Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Idempotent Division Update Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Idempotent Division", territoryId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Idempotent Division", territoryId };
        var response = await _client.PutAsJsonAsync($"/api/v1/divisions/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/divisions/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        var regionId = await CreateRegionAsync("Region For Toggle Division Test");
        var areaId = await CreateAreaAsync("Area For Toggle Division Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Toggle Division Test", areaId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Toggle Division", territoryId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/divisions/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/divisions/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/divisions/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/divisions/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateDivision_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/divisions/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateDivision_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/divisions/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/divisions/active
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveDivisions_AsAdmin_Returns200WithSuccessEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveDivisions_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/divisions/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveDivisions_DataIsArray()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetActiveDivisions_ReturnsOnlyActiveDivisions()
    {
        var regionId = await CreateRegionAsync("Region For Active Divisions Only Test");
        var areaId = await CreateAreaAsync("Area For Active Divisions Only Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Active Divisions Only Test", areaId);

        var activeResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Active Division For Active Filter Test", territoryId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Inactive Division For Active Filter Test", territoryId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/divisions/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/divisions/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        foreach (var item in data.EnumerateArray())
            item.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = data.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetActiveDivisions_FilteredByTerritoryId_ReturnsOnlyActiveDivisionsInThatTerritory()
    {
        var regionId = await CreateRegionAsync("Region For Active Division Territory Filter Test");
        var areaId = await CreateAreaAsync("Area For Active Division Territory Filter Test", regionId);
        var territoryXId = await CreateTerritoryAsync("Territory X For Active Division Filter Test", areaId);
        var territoryYId = await CreateTerritoryAsync("Territory Y For Active Division Filter Test", areaId);

        var divXResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Division X Active Territory Filter Test", territoryXId));
        var divXId = (await divXResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var divYResp = await _client.PostAsJsonAsync("/api/v1/divisions", CreateDivisionPayload("Division Y Active Territory Filter Test", territoryYId));
        var divYId = (await divYResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/divisions/active?territoryId={territoryXId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data")
            .EnumerateArray()
            .Select(d => d.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(divXId);
        ids.Should().NotContain(divYId);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/divisions/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
