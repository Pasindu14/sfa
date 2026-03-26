using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.UserReportingLines;

[Collection(SfaApiCollection.Name)]
public class UserReportingLinesApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public UserReportingLinesApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Creates a SalesRep user and returns the new user ID.</summary>
    private async Task<int> CreateSalesRepAsync(string namePrefix = "Rep")
    {
        SetToken(AuthHelper.AdminToken);
        var uid = Uid();
        var payload = new
        {
            name = $"{namePrefix} {uid}",
            username = $"rep_{uid}",
            email = $"rep_{uid}@test.com",
            phone = $"+94{Math.Abs(uid.GetHashCode() % 100000000):D8}",
            password = "Password1!",
            role = "SalesRep",
            deviceId = $"device_{uid}"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/users", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "user creation must succeed for test seeding");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static object CreateLinePayload(int userId, int managerId, string effectiveFrom = "2026-03-26")
        => new { userId, reportsToUserId = managerId, effectiveFrom };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/user-reporting-lines");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/user-reporting-lines/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/v1/user-reporting-lines/1",
            new { reportsToUserId = 2, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.DeleteAsync("/api/v1/user-reporting-lines/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(1, 2));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PutAsJsonAsync("/api/v1/user-reporting-lines/1",
            new { reportsToUserId = 2, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.DeleteAsync("/api/v1/user-reporting-lines/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET all — envelope structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/user-reporting-lines");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // CRUD happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidPayload_Returns201WithCorrectFields()
    {
        var userId = await CreateSalesRepAsync("CreateRL");
        var managerId = await CreateSalesRepAsync("ManagerRL");

        SetToken(AuthHelper.AdminToken);
        var payload = CreateLinePayload(userId, managerId, "2026-03-26");
        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = body.GetProperty("data");
        data.GetProperty("userId").GetInt32().Should().Be(userId);
        data.GetProperty("reportsToUserId").GetInt32().Should().Be(managerId);
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetById_AfterCreate_Returns200WithCorrectData()
    {
        var userId = await CreateSalesRepAsync("GetByIdRL");
        var managerId = await CreateSalesRepAsync("MgrGetByIdRL");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, managerId));
        var createBody = await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var getResp = await _client.GetAsync($"/api/v1/user-reporting-lines/{id}");

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("data").GetProperty("userId").GetInt32().Should().Be(userId);
    }

    [Fact]
    public async Task Update_ValidPayload_Returns200WithUpdatedManager()
    {
        var userId = await CreateSalesRepAsync("UpdateRL");
        var managerId1 = await CreateSalesRepAsync("Mgr1RL");
        var managerId2 = await CreateSalesRepAsync("Mgr2RL");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, managerId1));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/user-reporting-lines/{id}",
            new { reportsToUserId = managerId2, effectiveFrom = "2026-04-01" });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("reportsToUserId").GetInt32().Should().Be(managerId2);
    }

    [Fact]
    public async Task Delete_ExistingLine_Returns204()
    {
        var userId = await CreateSalesRepAsync("DeleteRL");
        var managerId = await CreateSalesRepAsync("MgrDeleteRL");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, managerId));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var deleteResp = await _client.DeleteAsync($"/api/v1/user-reporting-lines/{id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Activate_DeactivatedLine_Returns204()
    {
        var userId = await CreateSalesRepAsync("ActivateRL");
        var managerId = await CreateSalesRepAsync("MgrActivateRL");

        SetToken(AuthHelper.AdminToken);
        var createResp = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, managerId));
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        await _client.DeleteAsync($"/api/v1/user-reporting-lines/{id}");
        var activateResp = await _client.PostAsync($"/api/v1/user-reporting-lines/{id}/activate", null);

        activateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─────────────────────────────────────────────────
    // Subordinates endpoint
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSubordinates_DirectOnly_Returns200WithDirectReports()
    {
        var repId = await CreateSalesRepAsync("SubRep");
        var mgrId = await CreateSalesRepAsync("SubMgr");

        SetToken(AuthHelper.AdminToken);
        await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(repId, mgrId));

        var resp = await _client.GetAsync($"/api/v1/user-reporting-lines/{mgrId}/subordinates?depth=1");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = body.GetProperty("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.EnumerateArray().Should().Contain(el => el.GetProperty("userId").GetInt32() == repId);
    }

    // ─────────────────────────────────────────────────
    // Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/user-reporting-lines/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task Update_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync("/api/v1/user-reporting-lines/99999",
            new { reportsToUserId = 1, effectiveFrom = "2026-03-26" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.DeleteAsync("/api/v1/user-reporting-lines/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Validation (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_ZeroUserId_Returns400WithFieldErrors()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { userId = 0, reportsToUserId = 1, effectiveFrom = "2026-03-26" };

        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Create_SelfReport_Returns400()
    {
        var userId = await CreateSalesRepAsync("SelfRepRL");
        SetToken(AuthHelper.AdminToken);
        var payload = new { userId, reportsToUserId = userId, effectiveFrom = "2026-03-26" };

        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────
    // Business rules
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AdminUserAsSubordinate_Returns422()
    {
        // The seeded admin user (id=1) is Admin role — cannot be assigned a reporting line
        var managerId = await CreateSalesRepAsync("MgrAdminRL");
        SetToken(AuthHelper.AdminToken);
        var payload = CreateLinePayload(userId: 1, managerId: managerId);

        var response = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("USER_ROLE_NOT_ASSIGNABLE");
    }

    [Fact]
    public async Task Create_SecondAssignmentForSameUser_AutoDeactivatesPreviousLine()
    {
        var userId = await CreateSalesRepAsync("DupRL");
        var mgr1 = await CreateSalesRepAsync("DupMgr1");
        var mgr2 = await CreateSalesRepAsync("DupMgr2");

        SetToken(AuthHelper.AdminToken);

        // Create first reporting line
        var first = await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, mgr1));
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Create second — should deactivate first
        await _client.PostAsJsonAsync("/api/v1/user-reporting-lines",
            CreateLinePayload(userId, mgr2));

        // First line should now be inactive
        var getFirst = await _client.GetAsync($"/api/v1/user-reporting-lines/{firstId}");
        var firstBody = await getFirst.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        firstBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();
    }
}
