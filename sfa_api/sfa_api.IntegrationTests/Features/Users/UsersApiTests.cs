using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Users;

[Collection(SfaApiCollection.Name)]
public class UsersApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public UsersApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private object CreateUserPayload(
        string name = "Test User",
        string username = "testuser",
        string email = "testuser@example.com",
        string phone = "1234567890",
        string password = "Str0ng@Pass1",
        string role = "Manager",
        string? deviceId = null)
    {
        return new { name, username, email, phone, password, role, deviceId };
    }

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/users", CreateUserPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsers_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/users", CreateUserPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.DeleteAsync("/api/v1/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResetPassword_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/users/1/reset-password",
            new { newPassword = "NewStr0ng@Pass1" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateUser_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsync("/api/v1/users/1/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateUser_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsync("/api/v1/users/1/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // Update Authorization — non-admin can only update self
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_NonAdminUpdatingOtherUser_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken); // userId=200

        var response = await _client.PutAsJsonAsync("/api/v1/users/1", new
        {
            name = "Hacker",
            username = "hacker",
            email = "hack@evil.com",
            phone = "9999999999",
            role = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // Change Password — can only change own password
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ForDifferentUser_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken); // userId=200

        var response = await _client.PostAsJsonAsync("/api/v1/users/1/change-password", new
        {
            currentPassword = "Admin@1234",
            newPassword = "NewStr0ng@Pass1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/users — Admin happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsers_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/users — Create + GET by ID
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_AsAdmin_Returns201AndCanGetById()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateUserPayload(
            name: "Integration User",
            username: "integration_user",
            email: "integration@test.com",
            phone: "5551112222",
            role: "Manager");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", payload);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var userId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        userId.Should().BeGreaterThan(0);

        // Verify we can GET the created user
        var getResponse = await _client.GetAsync($"/api/v1/users/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("username").GetString().Should().Be("integration_user");
        getBody.GetProperty("data").GetProperty("role").GetString().Should().Be("Manager");
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_InvalidData_Returns400WithFieldErrors()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new
        {
            name = "",          // empty
            username = "ab",    // too short (min 3)
            email = "not-email",
            phone = "123",      // too short (min 10)
            password = "weak",  // no uppercase, no digit, no special
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/users", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        error.GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Object);
    }

    // ─────────────────────────────────────────────────
    // POST — Duplicate (409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_DuplicateUsername_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateUserPayload(
            name: "First User",
            username: "duplicate_test",
            email: "first@dup.com",
            phone: "7771110001",
            role: "Manager");

        var first = await _client.PostAsJsonAsync("/api/v1/users", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same username, different email/phone
        var duplicate = CreateUserPayload(
            name: "Second User",
            username: "duplicate_test",  // same username
            email: "second@dup.com",
            phone: "7771110002",
            role: "Manager");

        var second = await _client.PostAsJsonAsync("/api/v1/users", duplicate);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/users/{id} — Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/users/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/users/{id} — Update happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_AsAdmin_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        // First create a user to update
        var payload = CreateUserPayload(
            name: "Before Update",
            username: "update_target",
            email: "update@test.com",
            phone: "6661110001",
            role: "Manager");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var userId = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Update the user
        var updatePayload = new
        {
            name = "After Update",
            username = "update_target",
            email = "update@test.com",
            phone = "6661110001",
            role = "Admin"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/users/{userId}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("After Update");
        updateBody.GetProperty("data").GetProperty("role").GetString().Should().Be("Admin");
    }

    // ─────────────────────────────────────────────────
    // DELETE /api/v1/users/{id} — Soft delete
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_AsAdmin_Returns204()
    {
        SetToken(AuthHelper.AdminToken);

        // Create a user to delete
        var payload = CreateUserPayload(
            name: "To Delete",
            username: "delete_target",
            email: "delete@test.com",
            phone: "8881110001",
            role: "Manager");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var userId = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/v1/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/users/{id}/deactivate + activate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAndActivate_AsAdmin_TogglesIsActive()
    {
        SetToken(AuthHelper.AdminToken);

        // Create a user
        var payload = CreateUserPayload(
            name: "Toggle User",
            username: "toggle_user",
            email: "toggle@test.com",
            phone: "4441110001",
            role: "Manager");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var userId = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/users/{userId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getResponse = await _client.GetAsync($"/api/v1/users/{userId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/users/{userId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify activated
        var getResponse2 = await _client.GetAsync($"/api/v1/users/{userId}");
        var getBody2 = await getResponse2.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody2.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/users/{id}/reset-password
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_AsAdmin_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        // Create a user
        var payload = CreateUserPayload(
            name: "Reset PW User",
            username: "resetpw_user",
            email: "resetpw@test.com",
            phone: "3331110001",
            role: "Manager");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var userId = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Reset password
        var resetResponse = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/reset-password",
            new { newPassword = "NewStr0ng@Pass1" });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/users");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        // Verify envelope structure: { success, data, pagination, traceId }
        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/users/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        // Verify error envelope: { success: false, error: { code, message, ... } }
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
