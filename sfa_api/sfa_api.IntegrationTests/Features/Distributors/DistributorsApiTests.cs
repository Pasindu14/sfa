using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Distributors;

[Collection(SfaApiCollection.Name)]
public class DistributorsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public DistributorsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static object CreateDistributorPayload(
        string name = "Test Distributor",
        string address = "10 Test Road, Colombo",
        string phone = "0771234567",
        string email = "testdist@example.com",
        int alias = 999,
        decimal tradeDiscount = 10.00m,
        decimal commission = 5.00m,
        string? remark = null,
        string? vatRegNo = null,
        double? latitude = null,
        double? longitude = null)
    {
        return new
        {
            name,
            address,
            phone,
            email,
            alias,
            tradeDiscount,
            commission,
            remark,
            vatRegNo,
            latitude,
            longitude
        };
    }

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDistributors_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/distributors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDistributorById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/distributors/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDistributor_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", CreateDistributorPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDistributor_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync("/api/v1/distributors/1", CreateDistributorPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDistributor_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync("/api/v1/distributors/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — only Admin role is allowed
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDistributors_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/distributors");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllDistributors_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.GetAsync("/api/v1/distributors");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDistributor_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", CreateDistributorPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDistributor_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", CreateDistributorPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDistributor_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PutAsJsonAsync("/api/v1/distributors/1", CreateDistributorPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteDistributor_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.DeleteAsync("/api/v1/distributors/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateDistributor_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/distributors/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateDistributor_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/distributors/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/distributors — Admin happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDistributors_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/distributors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllDistributors_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/distributors?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/distributors — Create + GET by ID
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDistributor_AsAdmin_Returns201AndCanGetById()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(
            name: "Gamma Distributors",
            address: "7 Industrial Zone, Gampaha",
            phone: "0331234567",
            email: "gamma@distributors.com",
            alias: 501,
            tradeDiscount: 8.50m,
            commission: 3.00m,
            vatRegNo: "VAT5678901");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", payload);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var distributorId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        distributorId.Should().BeGreaterThan(0);

        // Verify GET by ID returns the same distributor
        var getResponse = await _client.GetAsync($"/api/v1/distributors/{distributorId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be("Gamma Distributors");
        getBody.GetProperty("data").GetProperty("email").GetString().Should().Be("gamma@distributors.com");
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateDistributor_AsAdmin_SetsIsActiveByDefault()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(
            name: "Delta Distributors",
            address: "15 Port Road, Colombo 01",
            phone: "0111234567",
            email: "delta@distributors.com",
            alias: 502);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateDistributor_AsAdmin_ResponseIncludesAllFields()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(
            name: "Epsilon Distributors",
            address: "20 Harbor Road, Colombo 15",
            phone: "0112345678",
            email: "epsilon@distributors.com",
            alias: 503,
            tradeDiscount: 15.00m,
            commission: 7.50m,
            remark: "Premium partner",
            vatRegNo: "VAT9988776",
            latitude: 6.9271,
            longitude: 79.8612);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", payload);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.GetProperty("name").GetString().Should().Be("Epsilon Distributors");
        data.GetProperty("address").GetString().Should().Be("20 Harbor Road, Colombo 15");
        data.GetProperty("alias").GetInt32().Should().Be(503);
        data.GetProperty("tradeDiscount").GetDecimal().Should().Be(15.00m);
        data.GetProperty("commission").GetDecimal().Should().Be(7.50m);
        data.GetProperty("remark").GetString().Should().Be("Premium partner");
        data.GetProperty("vatRegNo").GetString().Should().Be("VAT9988776");
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDistributor_InvalidData_Returns400WithFieldErrors()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new
        {
            name = "",             // required
            address = "",          // required
            phone = "123",         // too short
            email = "not-email",   // invalid format
            alias = 0,             // must be > 0
            tradeDiscount = -1m,   // cannot be negative
            commission = 101m      // cannot exceed 100
        };

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        error.GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task CreateDistributor_EmptyName_Returns400WithNameFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(name: "");

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateDistributor_InvalidEmail_Returns400WithEmailFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(email: "bad-email");

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("Email", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Duplicate conflicts (409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDistributor_DuplicateEmail_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        var first = CreateDistributorPayload(
            name: "First Dup Email Dist",
            address: "1 First Street, Colombo",
            phone: "0771110001",
            email: "duptest_email@example.com",
            alias: 601);

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/distributors", first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same email, different phone
        var second = CreateDistributorPayload(
            name: "Second Dup Email Dist",
            address: "2 Second Street, Colombo",
            phone: "0771110002",
            email: "duptest_email@example.com",  // duplicate
            alias: 602);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/distributors", second);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("EMAIL_DUPLICATE");
    }

    [Fact]
    public async Task CreateDistributor_DuplicatePhone_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        var first = CreateDistributorPayload(
            name: "First Dup Phone Dist",
            address: "1 Main Ave, Colombo",
            phone: "0771120001",
            email: "duptest_phone1@example.com",
            alias: 701);

        await _client.PostAsJsonAsync("/api/v1/distributors", first);

        // Same phone, different email
        var second = CreateDistributorPayload(
            name: "Second Dup Phone Dist",
            address: "2 Main Ave, Colombo",
            phone: "0771120001",               // duplicate
            email: "duptest_phone2@example.com",
            alias: 702);

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("PHONE_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/distributors/{id} — Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDistributorById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/distributors/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/distributors/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDistributor_AsAdmin_Returns200WithUpdatedData()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateDistributorPayload(
            name: "Before Update Dist",
            address: "5 Old Street, Colombo",
            phone: "0771230001",
            email: "beforeupdate@example.com",
            alias: 801);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = new
        {
            name = "After Update Dist",
            address = "99 New Boulevard, Galle",
            phone = "0771230001",
            email = "afterupdate@example.com",
            alias = 802,
            tradeDiscount = 20.00m,
            commission = 10.00m,
            remark = "Updated partner",
            vatRegNo = (string?)null,
            latitude = (double?)null,
            longitude = (double?)null
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/distributors/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update Dist");
        updateBody.GetProperty("data").GetProperty("address").GetString().Should().Be("99 New Boulevard, Galle");
        updateBody.GetProperty("data").GetProperty("tradeDiscount").GetDecimal().Should().Be(20.00m);
    }

    [Fact]
    public async Task UpdateDistributor_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new
        {
            name = "Ghost Dist",
            address = "Nowhere Street",
            phone = "0770000001",
            email = "ghost@example.com",
            alias = 1,
            tradeDiscount = 0m,
            commission = 0m,
            remark = (string?)null,
            vatRegNo = (string?)null,
            latitude = (double?)null,
            longitude = (double?)null
        };

        var response = await _client.PutAsJsonAsync("/api/v1/distributors/99999", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDistributor_InvalidData_Returns400()
    {
        SetToken(AuthHelper.AdminToken);

        // First create a distributor so we have a valid ID to update
        var createPayload = CreateDistributorPayload(
            name: "Valid Before Update",
            address: "3 Commerce Lane, Matara",
            phone: "0411234567",
            email: "validbeforeupdate@example.com",
            alias: 851);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new
        {
            name = "",            // required
            address = "OK",
            phone = "0411234567",
            email = "bad-email",  // invalid format
            alias = 1,
            tradeDiscount = 0m,
            commission = 0m
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/distributors/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateDistributor_DuplicateEmailOfOtherRecord_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        // Create two distributors
        var first = CreateDistributorPayload(
            name: "Conflict Dist A",
            address: "1 Street A",
            phone: "0771310001",
            email: "conflict_a@example.com",
            alias: 901);

        var second = CreateDistributorPayload(
            name: "Conflict Dist B",
            address: "2 Street B",
            phone: "0771310002",
            email: "conflict_b@example.com",
            alias: 902);

        var firstResp = await _client.PostAsJsonAsync("/api/v1/distributors", first);
        var secondResp = await _client.PostAsJsonAsync("/api/v1/distributors", second);

        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Try to update second distributor to use first distributor's email
        var updatePayload = new
        {
            name = "Conflict Dist B Updated",
            address = "2 Street B",
            phone = "0771310002",
            email = "conflict_a@example.com",   // taken by first distributor
            alias = 902,
            tradeDiscount = 0m,
            commission = 0m,
            remark = (string?)null,
            vatRegNo = (string?)null,
            latitude = (double?)null,
            longitude = (double?)null
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/distributors/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─────────────────────────────────────────────────
    // DELETE /api/v1/distributors/{id} — Soft delete
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDistributor_AsAdmin_Returns204()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateDistributorPayload(
            name: "Dist To Delete",
            address: "Disposal Road, Colombo",
            phone: "0771400001",
            email: "todelete@example.com",
            alias: 1001);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/distributors/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteDistributor_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.DeleteAsync("/api/v1/distributors/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/distributors/{id}/activate + deactivate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(
            name: "Toggle Active Dist",
            address: "Toggle Street, Negombo",
            phone: "0311234567",
            email: "toggleactive@example.com",
            alias: 1101);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/distributors", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/distributors/{id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getAfterDeactivate = await _client.GetAsync($"/api/v1/distributors/{id}");
        var deactivatedBody = await getAfterDeactivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        deactivatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/distributors/{id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getAfterActivate = await _client.GetAsync($"/api/v1/distributors/{id}");
        var activatedBody = await getAfterActivate.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        activatedBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ActivateDistributor_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/distributors/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateDistributor_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/distributors/99999/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/distributors");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/distributors/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // Created response Location header
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateDistributor_Returns201WithLocationHeader()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateDistributorPayload(
            name: "Location Header Dist",
            address: "Header Lane, Colombo 03",
            phone: "0111990001",
            email: "location_header@example.com",
            alias: 1201);

        var response = await _client.PostAsJsonAsync("/api/v1/distributors", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/distributors/");
    }
}
