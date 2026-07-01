using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Areas;

[Collection(SfaApiCollection.Name)]
public class AreasApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AreasApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a Region through the API and returns its id.
    /// Areas have a FK constraint to Regions, so every area test that needs
    /// to create data must seed a region first.
    /// </summary>
    private async Task<int> CreateRegionAsync(string name)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/regions", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"seeding region '{name}' must succeed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static object CreateAreaPayload(string name, int regionId)
        => new { name, regionId };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAreas_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/areas");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAreaById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/areas/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActiveAreas_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/areas/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateArea_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Test Area", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateArea_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/areas/1", CreateAreaPayload("Test Area", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateArea_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/areas/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateArea_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/areas/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArea_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Test Area", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateArea_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Test Area", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateArea_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/areas/1", CreateAreaPayload("Test Area", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateArea_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/areas/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateArea_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/areas/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/areas — any authenticated role
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAreas_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllAreas_AsSalesRep_Returns200()
    {
        // GET endpoints are [Authorize] — any authenticated role can access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/areas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllAreas_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAreas_WithSearchParam_Returns200()
    {
        // Arrange — seed a region and an area whose name contains the search term
        var regionId = await CreateRegionAsync("Region For Search Area Test");
        await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Searchable Eastern Area", regionId));

        // Act
        var response = await _client.GetAsync("/api/v1/areas?search=Searchable+Eastern");

        // Assert — pipeline accepts the param and returns a valid envelope
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();

        // All returned items (if any) must contain the search term in their name
        if (body.GetProperty("data").TryGetProperty("areas", out var areas) && areas.ValueKind == JsonValueKind.Array)
        {
            foreach (var area in areas.EnumerateArray())
                area.GetProperty("name").GetString()!.ToLower().Should().Contain("searchable eastern");
        }
    }

    [Fact]
    public async Task GetAllAreas_SearchByRegionName_ReturnsAreasUnderThatRegion()
    {
        var matchRegionId = await CreateRegionAsync("Region RegSearch UniqueRR1");
        var otherRegionId = await CreateRegionAsync("Region Plain For RegSearch");

        var underMatchResp = await _client.PostAsJsonAsync("/api/v1/areas",
            CreateAreaPayload("Area Under Matching Region", matchRegionId));
        var underMatchId = (await underMatchResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
        var underOtherResp = await _client.PostAsJsonAsync("/api/v1/areas",
            CreateAreaPayload("Area Under Other Region", otherRegionId));
        var underOtherId = (await underOtherResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Search matches the parent region's name, not the area's own name.
        var response = await _client.GetAsync("/api/v1/areas?search=UniqueRR1&pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("areas").EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(underMatchId);
        ids.Should().NotContain(underOtherId);
    }

    [Fact]
    public async Task GetAllAreas_SearchByCode_ReturnsAreaWithThatId()
    {
        var regionId = await CreateRegionAsync("Region For Area CodeSearch");
        var resp = await _client.PostAsJsonAsync("/api/v1/areas",
            CreateAreaPayload("Area CodeSearch Target", regionId));
        var targetId = (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/areas?search={targetId}&pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("areas").EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(targetId);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/areas — status filter
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAreas_StatusActive_ReturnsOnlyActiveAreas()
    {
        // Seed a region, then create one active and one inactive area
        var regionId = await CreateRegionAsync("Status Filter Region Active Test");

        var activeResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Active Area Status Test", regionId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Inactive Area Status Test", regionId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/areas/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/areas?status=active&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var areas = body.GetProperty("data").GetProperty("areas");

        // Every returned area must be active
        foreach (var area in areas.EnumerateArray())
            area.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = areas.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetAllAreas_StatusInactive_ReturnsOnlyInactiveAreas()
    {
        var regionId = await CreateRegionAsync("Status Filter Region Inactive Test");

        var activeResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Active Area For Inactive Filter", regionId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Inactive Area For Inactive Filter", regionId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/areas/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/areas?status=inactive&pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var areas = body.GetProperty("data").GetProperty("areas");

        // Every returned area must be inactive
        foreach (var area in areas.EnumerateArray())
            area.GetProperty("isActive").GetBoolean().Should().BeFalse();

        var ids = areas.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(inactiveId);
        ids.Should().NotContain(activeId);
    }

    [Fact]
    public async Task GetAllAreas_StatusOmitted_ReturnsAllAreas()
    {
        // When ?status is not provided the controller passes null isActive — all records returned
        var regionId = await CreateRegionAsync("Status Omitted Region Test");

        var activeResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Active Area Status Omitted", regionId));
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Inactive Area Status Omitted", regionId));
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/areas/{inactiveId}/deactivate", null);

        // Use a large pageSize so both seed areas are guaranteed to appear regardless of
        // how many records other tests have inserted into the shared DB.
        var response = await _client.GetAsync("/api/v1/areas?pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("areas")
            .EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(activeId);
        ids.Should().Contain(inactiveId);
    }

    [Fact]
    public async Task GetAllAreas_FilteredByRegionId_ReturnsOnlyAreasInThatRegion()
    {
        var regionAId = await CreateRegionAsync("Region A For Filter Test Areas");
        var regionBId = await CreateRegionAsync("Region B For Filter Test Areas");

        var areaInAResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Area In Region A Filter Test", regionAId));
        var areaInAId = (await areaInAResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var areaInBResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Area In Region B Filter Test", regionBId));
        var areaInBId = (await areaInBResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/areas?regionId={regionAId}&pageSize=1000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data").GetProperty("areas")
            .EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(areaInAId);
        ids.Should().NotContain(areaInBId);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/areas — Create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArea_AsAdmin_Returns201AndCanGetById()
    {
        var regionId = await CreateRegionAsync("Region For Create Area Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Western Area", regionId));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var areaId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        areaId.Should().BeGreaterThan(0);

        // Verify GET by ID returns the same area
        var getResponse = await _client.GetAsync($"/api/v1/areas/{areaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Area");
        getBody.GetProperty("data").GetProperty("regionId").GetInt32().Should().Be(regionId);
    }

    [Fact]
    public async Task CreateArea_AsAdmin_SetsIsActiveTrue()
    {
        var regionId = await CreateRegionAsync("Region For IsActive Area Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Central Area IsActive Test", regionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateArea_AsAdmin_Returns201WithLocationHeader()
    {
        var regionId = await CreateRegionAsync("Region For Location Header Area Test");

        var response = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Southern Area Location Test", regionId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/areas/");
    }

    [Fact]
    public async Task CreateArea_AsAdmin_ResponseIncludesAllFields()
    {
        var regionId = await CreateRegionAsync("Region For All Fields Area Test");

        var response = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Eastern Area Fields Test", regionId));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Area Fields Test");
        data.TryGetProperty("regionId", out _).Should().BeTrue();
        data.TryGetProperty("regionName", out _).Should().BeTrue();
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArea_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", regionId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/areas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateArea_NameExceedsMaxLength_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = new string('A', 101), regionId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/areas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateArea_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", regionId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/areas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task CreateArea_InvalidRegionId_Returns400WithRegionIdFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Valid Area Name", regionId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/areas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("RegionId", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Business rule failures (404 / 409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArea_RegionNotFound_Returns404WithRegionNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        // Use a RegionId that does not exist in the test DB
        var payload = new { name = "Area With Ghost Region", regionId = 999999 };
        var response = await _client.PostAsJsonAsync("/api/v1/areas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("REGION_NOT_FOUND");
    }

    [Fact]
    public async Task CreateArea_DuplicateNameInSameRegion_Returns409WithNameDuplicateCode()
    {
        var regionId = await CreateRegionAsync("Region For Duplicate Area Test");

        // First creation succeeds
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Duplicate Area Alpha", regionId));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same name + same region again
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Duplicate Area Alpha", regionId));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/areas/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAreaById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("AREA_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/areas/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArea_AsAdmin_Returns200WithUpdatedData()
    {
        var regionId = await CreateRegionAsync("Region For Update Area Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Before Update Area", regionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();
        var rowVersion = createBody.GetProperty("data").GetProperty("rowVersion").GetUInt32();

        var updatePayload = new { name = "After Update Area", regionId, rowVersion };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/areas/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Area");
    }

    [Fact]
    public async Task UpdateArea_NonExistent_Returns404()
    {
        var regionId = await CreateRegionAsync("Region For Update NonExistent Area Test");

        var response = await _client.PutAsJsonAsync("/api/v1/areas/99999", new { name = "Ghost Area", regionId, rowVersion = 1u });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateArea_InvalidData_Returns400()
    {
        var regionId = await CreateRegionAsync("Region For Update Invalid Area Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Valid Area For Invalid Update", regionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new { name = "", regionId };
        var response = await _client.PutAsJsonAsync($"/api/v1/areas/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateArea_DuplicateNameOfOtherRecordInSameRegion_Returns409()
    {
        var regionId = await CreateRegionAsync("Region For Conflict Update Area Test");

        await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Conflict Area A", regionId));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Conflict Area B", regionId));
        var secondData = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts)).GetProperty("data");
        var secondId = secondData.GetProperty("id").GetInt32();
        var secondRowVersion = secondData.GetProperty("rowVersion").GetUInt32();

        // Try to rename second area to the first area's name in the same region
        var updatePayload = new { name = "Conflict Area A", regionId, rowVersion = secondRowVersion };
        var response = await _client.PutAsJsonAsync($"/api/v1/areas/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateArea_SameNameAsOwnRecord_Returns200()
    {
        // Updating an area with its own name and same region should not be treated as duplicate
        var regionId = await CreateRegionAsync("Region For Idempotent Area Update Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Idempotent Area", regionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();
        var rowVersion = createBody.GetProperty("data").GetProperty("rowVersion").GetUInt32();

        var updatePayload = new { name = "Idempotent Area", regionId, rowVersion };
        var response = await _client.PutAsJsonAsync($"/api/v1/areas/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/areas/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        var regionId = await CreateRegionAsync("Region For Toggle Area Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Toggle Area", regionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/areas/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/areas/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/areas/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/areas/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateArea_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/areas/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateArea_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/areas/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/areas/active
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveAreas_AsAdmin_Returns200WithSuccessEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAreas_AsSalesRep_Returns200()
    {
        // [Authorize] with no role restriction — any authenticated user may access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/areas/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveAreas_DataIsArray()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        // data must be an array (flat list, no pagination wrapper)
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetActiveAreas_ReturnsOnlyActiveAreas()
    {
        var regionId = await CreateRegionAsync("Region For Active Areas Only Test");

        // Create one active area
        var activeResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Active Area For Active Filter Test", regionId));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Create a second area then deactivate it
        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Inactive Area For Active Filter Test", regionId));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/areas/{inactiveId}/deactivate", null);

        var response = await _client.GetAsync("/api/v1/areas/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        // All returned areas must have isActive == true
        foreach (var item in data.EnumerateArray())
            item.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = data.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetActiveAreas_FilteredByRegionId_ReturnsOnlyActiveAreasInThatRegion()
    {
        var regionXId = await CreateRegionAsync("Region X For Active Region Filter Test");
        var regionYId = await CreateRegionAsync("Region Y For Active Region Filter Test");

        // Area in Region X — active
        var areaXResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Area X Active Region Filter Test", regionXId));
        var areaXId = (await areaXResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Area in Region Y — active (must NOT appear when filtering by Region X)
        var areaYResp = await _client.PostAsJsonAsync("/api/v1/areas", CreateAreaPayload("Area Y Active Region Filter Test", regionYId));
        var areaYId = (await areaYResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/areas/active?regionId={regionXId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = body.GetProperty("data")
            .EnumerateArray()
            .Select(a => a.GetProperty("id").GetInt32())
            .ToList();

        ids.Should().Contain(areaXId);
        ids.Should().NotContain(areaYId);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/areas/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
