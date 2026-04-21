using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Fleets;

[Collection(SfaApiCollection.Name)]
public class FleetsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public FleetsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static object CreateFleetPayload(string name = "Test Fleet")
        => new { name };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFleets_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/fleets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFleetById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/fleets/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllActiveFleets_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/fleets/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFleet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateFleet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/fleets/1", CreateFleetPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateFleet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/fleets/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateFleet_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/v1/fleets/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only for write operations
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateFleet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateFleet_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateFleet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/fleets/1", CreateFleetPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateFleet_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/fleets/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateFleet_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/fleets/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/fleets — paginated list
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllFleets_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllFleets_AsSalesRep_Returns200()
    {
        // GET endpoints are [Authorize] — any authenticated role can access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/fleets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFleets_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllFleets_WithSearchParam_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        // Arrange — seed a fleet with a unique searchable name
        await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Searchable Northern Fleet"));

        // Act
        var response = await _client.GetAsync("/api/v1/fleets?search=Searchable+Northern");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        if (data.TryGetProperty("fleets", out var fleets) && fleets.ValueKind == JsonValueKind.Array)
        {
            foreach (var fleet in fleets.EnumerateArray())
                fleet.GetProperty("name").GetString()!.ToLower().Should().Contain("searchable northern");
        }
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/fleets — Create + GET by ID
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateFleet_AsAdmin_Returns201AndCanGetById()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Western Fleet"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var fleetId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        fleetId.Should().BeGreaterThan(0);

        // Verify GET by ID returns the same fleet
        var getResponse = await _client.GetAsync($"/api/v1/fleets/{fleetId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Western Fleet");
    }

    [Fact]
    public async Task CreateFleet_AsAdmin_SetsIsActiveTrue()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Central Fleet"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateFleet_AsAdmin_Returns201WithLocationHeader()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Southern Fleet"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/fleets/");
    }

    [Fact]
    public async Task CreateFleet_AsAdmin_ResponseIncludesAllFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Eastern Fleet"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.TryGetProperty("id", out _).Should().BeTrue();
        data.GetProperty("name").GetString().Should().Be("Eastern Fleet");
        data.TryGetProperty("isActive", out _).Should().BeTrue();
        data.TryGetProperty("createdAt", out _).Should().BeTrue();
        data.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateFleet_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateFleetPayload("");

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateFleet_NameExceedsMaxLength_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateFleetPayload(new string('F', 101));

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateFleet_InvalidData_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/fleets", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // POST — Duplicate conflict (409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateFleet_DuplicateName_Returns409WithNameDuplicateCode()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateFleetPayload("Duplicate Fleet Alpha");

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/fleets", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same name again
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/fleets", payload);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/fleets/{id} — Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFleetById_NonExistent_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("FLEET_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/fleets/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateFleet_AsAdmin_Returns200WithUpdatedData()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Before Update Fleet"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new { name = "After Update Fleet" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/fleets/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Fleet");
    }

    [Fact]
    public async Task UpdateFleet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PutAsJsonAsync("/api/v1/fleets/99999", new { name = "Ghost Fleet" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFleet_InvalidData_Returns400()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Valid Fleet Before Update"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/v1/fleets/{id}", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateFleet_DuplicateNameOfOtherRecord_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Conflict Fleet A"));
        var secondResp = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Conflict Fleet B"));
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/v1/fleets/{secondId}", new { name = "Conflict Fleet A" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateFleet_SameNameAsOwnRecord_Returns200()
    {
        // Updating with own name must not be treated as a duplicate
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Idempotent Fleet"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var response = await _client.PutAsJsonAsync($"/api/v1/fleets/{id}", new { name = "Idempotent Fleet" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/fleets/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        SetToken(AuthHelper.AdminToken);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Toggle Fleet"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/fleets/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/fleets/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/fleets/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/fleets/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateFleet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/fleets/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateFleet_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/fleets/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/fleets/all — lightweight active list
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveFleets_AsAdmin_Returns200WithSuccessEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllActiveFleets_AsSalesRep_Returns200()
    {
        // [Authorize] with no role restriction — any authenticated user may access
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/fleets/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllActiveFleets_DataIsArray()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets/all");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        // data must be a flat array (no pagination wrapper)
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllActiveFleets_ReturnsOnlyActiveFleets()
    {
        SetToken(AuthHelper.AdminToken);

        // Create one active fleet
        var activeResp = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Active Fleet For Filter Test"));
        activeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var activeId = (await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Create a second fleet then deactivate it
        var inactiveResp = await _client.PostAsJsonAsync("/api/v1/fleets", CreateFleetPayload("Inactive Fleet For Filter Test"));
        inactiveResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inactiveId = (await inactiveResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.PostAsync($"/api/v1/fleets/{inactiveId}/deactivate", null);

        // Act
        var response = await _client.GetAsync("/api/v1/fleets/all");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        // All returned fleets must have isActive == true
        foreach (var item in data.EnumerateArray())
            item.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var ids = data.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToList();
        ids.Should().Contain(activeId);
        ids.Should().NotContain(inactiveId);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/fleets/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
