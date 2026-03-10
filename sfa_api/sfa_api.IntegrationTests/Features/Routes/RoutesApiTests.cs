using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Routes;

[Collection(SfaApiCollection.Name)]
public class RoutesApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RoutesApiTests(SfaWebApplicationFactory factory)
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

    private static object CreateRoutePayload(string name, int divisionId, string pinColor = "#FF5733", string? description = null)
        => new { name, divisionId, pinColor, description };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRoutes_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/routes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRouteById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/routes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRoute_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Test Route", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRoute_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/routes/1", CreateRoutePayload("Test Route", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRoute_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync("/api/v1/routes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoute_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Test Route", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRoute_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Test Route", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRoute_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/routes/1", CreateRoutePayload("Test Route", 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRoute_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.DeleteAsync("/api/v1/routes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRoute_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.DeleteAsync("/api/v1/routes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/routes
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRoutes_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/routes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllRoutes_AsSalesRep_Returns200()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/routes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllRoutes_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/routes?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRoutes_WithSearchParam_ReturnsFilteredResults()
    {
        var regionId = await CreateRegionAsync("Region For Route Search Test");
        var areaId = await CreateAreaAsync("Area For Route Search Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Route Search Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Route Search Test", territoryId);

        await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Searchable Route UniqueXYZ123", divisionId));
        await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Other Route Search Test", divisionId));

        var response = await _client.GetAsync("/api/v1/routes?search=UniqueXYZ123&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var routes = body.GetProperty("data").GetProperty("routes");
        routes.EnumerateArray().Should().Contain(r => r.GetProperty("name").GetString()!.Contains("UniqueXYZ123"));
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/routes — Create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoute_AsAdmin_Returns201AndCanGetById()
    {
        var regionId = await CreateRegionAsync("Region For Create Route Test");
        var areaId = await CreateAreaAsync("Area For Create Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Create Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Create Route Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Western Route", divisionId, "#FF5733", "A western route"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var routeId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        routeId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/v1/routes/{routeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Route");
        getBody.GetProperty("data").GetProperty("pinColor").GetString().Should().Be("#FF5733");
        getBody.GetProperty("data").GetProperty("divisionId").GetInt32().Should().Be(divisionId);
    }

    [Fact]
    public async Task CreateRoute_AsAdmin_Returns201WithLocationHeader()
    {
        var regionId = await CreateRegionAsync("Region For Route Location Header Test");
        var areaId = await CreateAreaAsync("Area For Route Location Header Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Route Location Header Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Route Location Header Test", territoryId);

        var response = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Southern Route Location Test", divisionId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/routes/");
    }

    [Fact]
    public async Task CreateRoute_AsAdmin_ResponseIncludesAllAncestorFields()
    {
        var regionId = await CreateRegionAsync("Region For Route All Fields Test");
        var areaId = await CreateAreaAsync("Area For Route All Fields Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Route All Fields Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Route All Fields Test", territoryId);

        var response = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Eastern Route Fields Test", divisionId));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Route Fields Test");
        data.TryGetProperty("pinColor", out _).Should().BeTrue();
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

        // Verify denormalized ancestor IDs are correctly resolved
        data.GetProperty("divisionId").GetInt32().Should().Be(divisionId);
        data.GetProperty("territoryId").GetInt32().Should().Be(territoryId);
        data.GetProperty("areaId").GetInt32().Should().Be(areaId);
        data.GetProperty("regionId").GetInt32().Should().Be(regionId);
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoute_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", pinColor = "#FF5733", divisionId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoute_EmptyPinColor_Returns400WithPinColorFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Valid Route Name", pinColor = "", divisionId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("PinColor", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoute_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "", pinColor = "", divisionId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task CreateRoute_InvalidDivisionId_Returns400WithDivisionIdFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Valid Route Name", pinColor = "#FF5733", divisionId = 0 };
        var response = await _client.PostAsJsonAsync("/api/v1/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("DivisionId", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Business rule failures (404 / 409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoute_DivisionNotFound_Returns404WithDivisionNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "Route With Ghost Division", pinColor = "#FF5733", divisionId = 999999 };
        var response = await _client.PostAsJsonAsync("/api/v1/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task CreateRoute_DuplicateNameInSameDivision_Returns409WithNameDuplicateCode()
    {
        var regionId = await CreateRegionAsync("Region For Duplicate Route Test");
        var areaId = await CreateAreaAsync("Area For Duplicate Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Duplicate Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Duplicate Route Test", territoryId);

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Duplicate Route Alpha", divisionId));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Duplicate Route Alpha", divisionId));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/routes/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRouteById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/routes/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("ROUTE_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/routes/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoute_AsAdmin_Returns200WithUpdatedData()
    {
        var regionId = await CreateRegionAsync("Region For Update Route Test");
        var areaId = await CreateAreaAsync("Area For Update Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update Route Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Before Update Route", divisionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "After Update Route", pinColor = "#0000FF", divisionId };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/routes/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Route");
        updateBody.GetProperty("data").GetProperty("pinColor").GetString().Should().Be("#0000FF");
    }

    [Fact]
    public async Task UpdateRoute_NonExistent_Returns404()
    {
        var regionId = await CreateRegionAsync("Region For Update NonExistent Route Test");
        var areaId = await CreateAreaAsync("Area For Update NonExistent Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update NonExistent Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update NonExistent Route Test", territoryId);

        var response = await _client.PutAsJsonAsync("/api/v1/routes/99999",
            new { name = "Ghost Route", pinColor = "#FF5733", divisionId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRoute_InvalidData_Returns400()
    {
        var regionId = await CreateRegionAsync("Region For Update Invalid Route Test");
        var areaId = await CreateAreaAsync("Area For Update Invalid Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Update Invalid Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Update Invalid Route Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Valid Route For Invalid Update", divisionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new { name = "", pinColor = "", divisionId };
        var response = await _client.PutAsJsonAsync($"/api/v1/routes/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateRoute_DuplicateNameOfOtherRecordInSameDivision_Returns409()
    {
        var regionId = await CreateRegionAsync("Region For Conflict Update Route Test");
        var areaId = await CreateAreaAsync("Area For Conflict Update Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Conflict Update Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Conflict Update Route Test", territoryId);

        await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Conflict Route A", divisionId));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/routes", CreateRoutePayload("Conflict Route B", divisionId));
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Conflict Route A", pinColor = "#FF5733", divisionId };
        var response = await _client.PutAsJsonAsync($"/api/v1/routes/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateRoute_SameNameAsOwnRecord_Returns200()
    {
        var regionId = await CreateRegionAsync("Region For Idempotent Route Update Test");
        var areaId = await CreateAreaAsync("Area For Idempotent Route Update Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Idempotent Route Update Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Idempotent Route Update Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Idempotent Route", divisionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "Idempotent Route", pinColor = "#FF5733", divisionId };
        var response = await _client.PutAsJsonAsync($"/api/v1/routes/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // DELETE /api/v1/routes/{id} — Soft delete
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRoute_AsAdmin_Returns204()
    {
        var regionId = await CreateRegionAsync("Region For Delete Route Test");
        var areaId = await CreateAreaAsync("Area For Delete Route Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Delete Route Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Delete Route Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Route To Delete", divisionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/routes/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteRoute_AsAdmin_SoftDeletesRoute_NotVisibleInList()
    {
        var regionId = await CreateRegionAsync("Region For Soft Delete Route Visibility Test");
        var areaId = await CreateAreaAsync("Area For Soft Delete Route Visibility Test", regionId);
        var territoryId = await CreateTerritoryAsync("Territory For Soft Delete Route Visibility Test", areaId);
        var divisionId = await CreateDivisionAsync("Division For Soft Delete Route Visibility Test", territoryId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routes",
            CreateRoutePayload("Soft Deleted Route UniqueABC987", divisionId));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        await _client.DeleteAsync($"/api/v1/routes/{id}");

        var listResponse = await _client.GetAsync("/api/v1/routes?search=UniqueABC987&pageSize=100");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var routes = listBody.GetProperty("data").GetProperty("routes");
        var ids = routes.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        ids.Should().NotContain(id);
    }

    [Fact]
    public async Task DeleteRoute_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.DeleteAsync("/api/v1/routes/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/routes");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/routes/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
