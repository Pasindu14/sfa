using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.ProximityExemptions;

/// <summary>
/// Covers /api/v1/proximity-exemptions — the admin control that lets one rep bill
/// outside the billing geofence for a bounded period.
///
/// The authorization tests matter most: this endpoint weakens a fraud control, so
/// "only an Admin can call it" has to be enforced by the API rather than by the web
/// app choosing not to draw the button.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class ProximityExemptionsApiTests
{
    private const string BaseUrl = "/api/v1/proximity-exemptions";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ProximityExemptionsApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// Seeds a user and returns its id. Admins are seeded too, because the grant
    /// row has an FK to its granter and SQLite enforces FKs.
    private async Task<int> SeedUserAsync(UserRole role, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var user = new User
        {
            Name = $"Exempt-{role}-{suffix}",
            Username = $"ex{suffix}",
            Email = $"ex-{suffix}@sfa.com",
            Phone = $"07{suffix}",
            PasswordHash = "x",
            Role = role,
            IsActive = isActive
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static object GrantBody(DateOnly? until = null, string? reason = null, string? notes = null)
        => new
        {
            validUntil = (until ?? SriLankaTime.Today.AddDays(3)).ToString("yyyy-MM-dd"),
            reason = reason ?? nameof(ProximityExemptionReason.BadOutletCoordinates),
            notes
        };

    private async Task<(HttpStatusCode Status, JsonElement Data, string Raw)> PostAsync(string url, object body)
    {
        var resp = await _client.PostAsync(url, JsonContent.Create(body));
        var raw = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return (resp.StatusCode, default, raw);

        using var doc = JsonDocument.Parse(raw);
        return (resp.StatusCode, doc.RootElement.GetProperty("data").Clone(), raw);
    }

    // ─────────────────────────────────────────────────
    // Authorization — the endpoint is the gate, not the UI
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("SalesRep")]
    [InlineData("Supervisor")]
    [InlineData("ASM")]
    [InlineData("Distributor")]
    public async Task Grant_NonAdmin_IsForbidden(string role)
    {
        // A rep granting themselves relief from the geofence would defeat the point.
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(9100, role));

        var resp = await _client.PostAsync($"{BaseUrl}/user/{repId}", JsonContent.Create(GrantBody()));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_NonAdmin_IsForbidden()
    {
        SetToken(AuthHelper.GenerateToken(9101, "Supervisor"));

        (await _client.GetAsync(BaseUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Grant_Anonymous_IsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var resp = await _client.PostAsync($"{BaseUrl}/user/1", JsonContent.Create(GrantBody()));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Grant
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Grant_ThenGetCurrent_ReturnsTheLiveGrant()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));
        var until = SriLankaTime.Today.AddDays(5);

        var (status, data, raw) = await PostAsync(
            $"{BaseUrl}/user/{repId}",
            GrantBody(until, nameof(ProximityExemptionReason.SharedCoordinateMarket), "market stalls share one pin"));

        status.Should().Be(HttpStatusCode.OK, raw);
        data.GetProperty("isCurrentlyEffective").GetBoolean().Should().BeTrue();
        // Echoed back as the inclusive business date the admin picked, not the
        // exclusive midnight boundary actually stored.
        data.GetProperty("validUntilDate").GetString().Should().Be(until.ToString("yyyy-MM-dd"));
        data.GetProperty("reason").GetString().Should().Be(nameof(ProximityExemptionReason.SharedCoordinateMarket));
        data.GetProperty("grantedByUserId").GetInt32().Should().Be(adminId);
        data.GetProperty("notes").GetString().Should().Be("market stalls share one pin");

        var currentResp = await _client.GetAsync($"{BaseUrl}/user/{repId}/current");
        var currentRaw = await currentResp.Content.ReadAsStringAsync();
        currentResp.StatusCode.Should().Be(HttpStatusCode.OK, currentRaw);
        using var doc = JsonDocument.Parse(currentRaw);
        doc.RootElement.GetProperty("data").GetProperty("isCurrentlyEffective").GetBoolean()
           .Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrent_NoGrant_ReturnsNullData()
    {
        // 200-with-null rather than 404: "this rep is not exempt" is a normal answer,
        // and the admin dialog should render the enforced state, not an error.
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var resp = await _client.GetAsync($"{BaseUrl}/user/{repId}/current");
        var raw = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK, raw);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Grant_BeyondTheMaximumDuration_IsRejected()
    {
        // The cap is the only thing stopping a temporary exemption from becoming a
        // permanent one that nobody remembers granting.
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync(
            $"{BaseUrl}/user/{repId}", GrantBody(SriLankaTime.Today.AddDays(365)));

        status.Should().Be(HttpStatusCode.BadRequest, raw);
        raw.Should().Contain("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Grant_APastDate_IsRejected()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync(
            $"{BaseUrl}/user/{repId}", GrantBody(SriLankaTime.Today.AddDays(-2)));

        status.Should().Be(HttpStatusCode.BadRequest, raw);
    }

    [Fact]
    public async Task Grant_UnknownReasonCode_IsRejected()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync(
            $"{BaseUrl}/user/{repId}", GrantBody(reason: "NoParticularReason"));

        status.Should().Be(HttpStatusCode.BadRequest, raw);
    }

    [Fact]
    public async Task Grant_ToANonSalesRep_IsRejected()
    {
        // The geofence only runs on rep billing, so this would be a control that
        // reads as real and does nothing at all.
        var adminId = await SeedUserAsync(UserRole.Admin);
        var supervisorId = await SeedUserAsync(UserRole.Supervisor);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync($"{BaseUrl}/user/{supervisorId}", GrantBody());

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("PROXIMITY_EXEMPTION_ROLE_INVALID");
    }

    [Fact]
    public async Task Grant_ToADeactivatedRep_IsRejected()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep, isActive: false);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody());

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("PROXIMITY_EXEMPTION_USER_INACTIVE");
    }

    [Fact]
    public async Task Grant_ToAnUnknownUser_IsNotFound()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync($"{BaseUrl}/user/999999", GrantBody());

        status.Should().Be(HttpStatusCode.NotFound, raw);
    }

    [Fact]
    public async Task Grant_Twice_SupersedesAndKeepsBothInHistory()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (_, first, _) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody(SriLankaTime.Today.AddDays(2)));
        var (status, second, raw) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody(SriLankaTime.Today.AddDays(9)));

        status.Should().Be(HttpStatusCode.OK, raw);
        second.GetProperty("id").GetInt32().Should().NotBe(first.GetProperty("id").GetInt32());

        // Exactly one live grant — two overlapping ones would make the resolver's
        // answer depend on ordering.
        var historyResp = await _client.GetAsync($"{BaseUrl}/user/{repId}");
        var historyRaw = await historyResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(historyRaw);
        var rows = doc.RootElement.GetProperty("data").EnumerateArray().ToList();

        rows.Should().HaveCount(2, "the superseded grant stays as history, it is not deleted");
        rows.Count(r => r.GetProperty("isCurrentlyEffective").GetBoolean()).Should().Be(1);
    }

    // ─────────────────────────────────────────────────
    // Revoke
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_EndsTheGrantImmediately()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (_, granted, _) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody());
        var id = granted.GetProperty("id").GetInt32();
        var rowVersion = granted.GetProperty("rowVersion").GetUInt32();

        var (status, revoked, raw) = await PostAsync($"{BaseUrl}/{id}/revoke", new { rowVersion });

        status.Should().Be(HttpStatusCode.OK, raw);
        revoked.GetProperty("isActive").GetBoolean().Should().BeFalse();
        revoked.GetProperty("isCurrentlyEffective").GetBoolean().Should().BeFalse();
        revoked.GetProperty("revokedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        revoked.GetProperty("revokedByUserId").GetInt32().Should().Be(adminId);

        // And the rep is immediately back under the geofence.
        var currentResp = await _client.GetAsync($"{BaseUrl}/user/{repId}/current");
        using var doc = JsonDocument.Parse(await currentResp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Revoke_Twice_IsRejected()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (_, granted, _) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody());
        var id = granted.GetProperty("id").GetInt32();
        var rowVersion = granted.GetProperty("rowVersion").GetUInt32();

        await PostAsync($"{BaseUrl}/{id}/revoke", new { rowVersion });
        var (status, _, raw) = await PostAsync($"{BaseUrl}/{id}/revoke", new { rowVersion });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("PROXIMITY_EXEMPTION_ALREADY_REVOKED");
    }

    [Fact]
    public async Task Revoke_WithoutARowVersion_IsRejected()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        var repId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (_, granted, _) = await PostAsync($"{BaseUrl}/user/{repId}", GrantBody());
        var id = granted.GetProperty("id").GetInt32();

        var (status, _, raw) = await PostAsync($"{BaseUrl}/{id}/revoke", new { rowVersion = 0 });

        status.Should().Be(HttpStatusCode.BadRequest, raw);
    }

    [Fact]
    public async Task Revoke_NonAdmin_IsForbidden()
    {
        SetToken(AuthHelper.GenerateToken(9102, "SalesRep"));

        var resp = await _client.PostAsync($"{BaseUrl}/1/revoke", JsonContent.Create(new { rowVersion = 1u }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revoke_UnknownExemption_IsNotFound()
    {
        var adminId = await SeedUserAsync(UserRole.Admin);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        var (status, _, raw) = await PostAsync($"{BaseUrl}/999999/revoke", new { rowVersion = 1u });

        status.Should().Be(HttpStatusCode.NotFound, raw);
    }

    // ─────────────────────────────────────────────────
    // Review list
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsOnlyCurrentlyEffectiveGrants()
    {
        // This is the audit list. A revoked or expired grant appearing here would
        // overstate how many reps are currently outside the geofence.
        var adminId = await SeedUserAsync(UserRole.Admin);
        var liveRepId = await SeedUserAsync(UserRole.SalesRep);
        var revokedRepId = await SeedUserAsync(UserRole.SalesRep);
        SetToken(AuthHelper.GenerateToken(adminId, "Admin"));

        await PostAsync($"{BaseUrl}/user/{liveRepId}", GrantBody());
        var (_, toRevoke, _) = await PostAsync($"{BaseUrl}/user/{revokedRepId}", GrantBody());
        await PostAsync(
            $"{BaseUrl}/{toRevoke.GetProperty("id").GetInt32()}/revoke",
            new { rowVersion = toRevoke.GetProperty("rowVersion").GetUInt32() });

        var resp = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=200");
        var raw = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.OK, raw);

        using var doc = JsonDocument.Parse(raw);
        var userIds = doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(r => r.GetProperty("userId").GetInt32()).ToList();

        userIds.Should().Contain(liveRepId);
        userIds.Should().NotContain(revokedRepId);
    }
}
