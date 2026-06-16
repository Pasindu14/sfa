using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Regions;

[Collection(SfaApiCollection.Name)]
public class RegionsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RegionsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static object CreateRegionPayload(string name = "Test Region")
        => new { name };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRegions_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/regions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRegionById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/regions/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRegion_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRegion_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/regions/1", CreateRegionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateRegion_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/regions/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateRegion_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/regions/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRegion_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRegion_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRegion_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/regions/1", CreateRegionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateRegion_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/regions/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateRegion_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/regions/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/regions — any authenticated role
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRegions_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllRegions_AsSalesRep_Returns200()
    {
        // GET endpoints are [Authorize] — any authenticated role can access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/regions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllRegions_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRegions_WithSearchParam_Returns200()
    {
        // Arrange — create a region whose name contains the search term so we have at least one match
        SetToken(AuthHelper.AdminToken);
        await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Searchable North Region"));

        // Act
        var response = await _client.GetAsync("/api/v1/regions?search=Searchable+North");

        // Assert — pipeline accepts the param and returns a valid envelope
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();

        // All returned items (if any) must contain the search term in their name
        var data = body.GetProperty("data");
        if (data.TryGetProperty("regions", out var regions) && regions.ValueKind == JsonValueKind.Array)
        {
            foreach (var region in regions.EnumerateArray())
                region.GetProperty("name").GetString()!.ToLower().Should().Contain("searchable north");
        }
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/regions — Create + GET by ID
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRegion_AsAdmin_Returns201AndCanGetById()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateRegionPayload("Western Province");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", payload);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var regionId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        regionId.Should().BeGreaterThan(0);

        // Verify GET by ID returns the same region
        var getResponse = await _client.GetAsync($"/api/v1/regions/{regionId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Province");
    }

    [Fact]
    public async Task CreateRegion_AsAdmin_SetsIsActiveTrue()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Central Province"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateRegion_AsAdmin_Returns201WithLocationHeader()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Southern Province"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/regions/");
    }

    [Fact]
    public async Task CreateRegion_AsAdmin_ResponseIncludesAllFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Eastern Province"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Province");
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRegion_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateRegionPayload("");

        var response = await _client.PostAsJsonAsync("/api/v1/regions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRegion_NameExceedsMaxLength_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateRegionPayload(new string('A', 101));

        var response = await _client.PostAsJsonAsync("/api/v1/regions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRegion_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new { name = "" };

        var response = await _client.PostAsJsonAsync("/api/v1/regions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // POST — Duplicate conflict (409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRegion_DuplicateName_Returns409WithNameDuplicateCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateRegionPayload("Duplicate Region Alpha");

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/regions", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same name again
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/regions", payload);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/regions/{id} — Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRegionById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("REGION_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/regions/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRegion_AsAdmin_Returns200WithUpdatedData()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Before Update Region"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "After Update Region", rowVersion = 1 };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/regions/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Region");
    }

    [Fact]
    public async Task UpdateRegion_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PutAsJsonAsync("/api/v1/regions/99999", new { name = "Ghost Region", rowVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRegion_InvalidData_Returns400()
    {
        SetToken(AuthHelper.AdminToken);

        // First create a region to get a valid ID
        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Valid Before Update Region"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new { name = "" };
        var response = await _client.PutAsJsonAsync($"/api/v1/regions/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateRegion_DuplicateNameOfOtherRecord_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Conflict Region A"));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Conflict Region B"));
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Try to rename second region to the first region's name
        var updatePayload = new { name = "Conflict Region A", rowVersion = 1 };
        var response = await _client.PutAsJsonAsync($"/api/v1/regions/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateRegion_SameNameAsOwnRecord_Returns200()
    {
        // Updating a region with its own name should not be treated as a duplicate
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Idempotent Region"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Re-send the same name
        var updatePayload = new { name = "Idempotent Region", rowVersion = 1 };
        var response = await _client.PutAsJsonAsync($"/api/v1/regions/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/regions/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload("Toggle Region"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/regions/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/regions/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/regions/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/regions/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateRegion_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/regions/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateRegion_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/regions/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/regions/active
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveRegions_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/regions/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActiveRegions_AsAdmin_Returns200WithSuccessEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveRegions_AsSalesRep_Returns200()
    {
        // [Authorize] with no role restriction — any authenticated user may access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/regions/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveRegions_ReturnsOnlyActiveRegions()
    {
        SetToken(AuthHelper.AdminToken);

        // Create one active region
        var activePayload = CreateRegionPayload("Active Region For Filter Test");
        var activeResp = await _client.PostAsJsonAsync("/api/v1/regions", activePayload);
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Create a second region then deactivate it
        var inactivePayload = CreateRegionPayload("Inactive Region For Filter Test");
        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/regions", inactivePayload);
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/regions/{inactiveId}/deactivate", null);

        // Act
        var response = await _client.GetAsync("/api/v1/regions/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        // All returned regions must have isActive == true
        foreach (var item in data.EnumerateArray())
            item.GetProperty("isActive").GetBoolean().Should().BeTrue();

        // The active region must be present
        var ids = data.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);

        // The deactivated region must not be present
        ids.Should().NotContain(inactiveId);
    }

    [Fact]
    public async Task GetActiveRegions_ResultsAreOrderedAlphabeticallyByName()
    {
        SetToken(AuthHelper.AdminToken);

        // Create three active regions with names that have a known alphabetical order
        var names = new[] { "Zebra Region Sort Test", "Alpha Region Sort Test", "Mango Region Sort Test" };
        foreach (var name in names)
            await _client.PostAsJsonAsync("/api/v1/regions", CreateRegionPayload(name));

        var response = await _client.GetAsync("/api/v1/regions/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var returnedNames = body.GetProperty("data")
            .EnumerateArray()
            .Select(i => i.GetProperty("name").GetString()!)
            .ToList();

        // Extract only the names we seeded for this test to avoid interference from other tests
        var filteredNames = returnedNames
            .Where(n => names.Contains(n))
            .ToList();

        filteredNames.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetActiveRegions_DataIsArray()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions/active");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        // data must be an array (flat list, no pagination wrapper)
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/regions/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
