using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Billings;

/// <summary>
/// GET /api/v1/billings/portal/dashboard-summary — distributor dashboard totals aggregated server-side.
/// Runs the non-Npgsql repository path (SQLite cannot SUM decimal); the grouping and filters are the
/// same, and the aggregation itself is unit-tested in DistributorBillingDashboardServiceTests.
/// Bills are seeded directly with FK enforcement briefly off (only the bill's own columns matter), in a
/// far-past date window so no other test's data can fall inside it, and removed afterwards.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class DistributorBillingDashboardApiTests(SfaWebApplicationFactory factory)
{
    private const string Url = "/api/v1/billings/portal/dashboard-summary";
    private readonly HttpClient _client = factory.CreateClient();
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private async Task<HttpResponseMessage> GetAsync(string token, string query)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, Url + query);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(req);
    }

    [Fact]
    public async Task Summary_AggregatesOwnBillsInRange_ExcludingCancelledFromRevenue()
    {
        const int distributorId = 991_001, otherDistributorId = 991_002;
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var d1 = new DateOnly(2001, 3, 1);
        var d3 = new DateOnly(2001, 3, 3);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var n = 0;
        Billing Bill(int dist, DateOnly date, DistributorBillingStatus ds, RepBillingStatus rs, decimal total, bool deleted = false) => new()
        {
            BillingNumber = $"DSH-{suffix}-{++n}", BillingDate = date, OutletId = 1, SalesRepId = 1,
            DistributorId = dist, DistributorStatus = ds, RepStatus = rs, TotalAmount = total,
            SubTotalAmount = total, IsDeleted = deleted,
        };

        var user = new User
        {
            Name = "Dash Distributor", Username = $"dash-{suffix}", Email = $"dash-{suffix}@sfa.com",
            Phone = $"071{suffix}", PasswordHash = "placeholder", Role = UserRole.Distributor,
            DistributorId = distributorId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        try
        {
            db.Users.Add(user);
            db.Billings.AddRange(
                Bill(distributorId, d1, DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 100m),
                Bill(distributorId, d1, DistributorBillingStatus.Approved, RepBillingStatus.Cancelled, 50m),   // approved-then-cancelled
                Bill(distributorId, d1, DistributorBillingStatus.Pending,  RepBillingStatus.Submitted, 30m),
                Bill(distributorId, d1, DistributorBillingStatus.Rejected, RepBillingStatus.Submitted, 20m),
                Bill(distributorId, d3, DistributorBillingStatus.Pending,  RepBillingStatus.Submitted, 10.5m),
                Bill(distributorId, d3, DistributorBillingStatus.Pending,  RepBillingStatus.Cancelled, 5m),
                Bill(distributorId, new DateOnly(2001, 3, 8), DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 999m), // out of range
                Bill(otherDistributorId, d1, DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 777m),                   // not mine
                Bill(distributorId, d1, DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 555m, deleted: true));       // deleted
            await db.SaveChangesAsync();
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        try
        {
            var token = AuthHelper.GenerateToken(user.Id, "Distributor");
            var response = await GetAsync(token, "?dateFrom=2001-03-01&dateTo=2001-03-07");
            var raw = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, raw);

            var data = JsonDocument.Parse(raw).RootElement.GetProperty("data");
            data.GetProperty("dateFrom").GetString().Should().Be("2001-03-01");
            data.GetProperty("dateTo").GetString().Should().Be("2001-03-07");
            data.GetProperty("totalRevenue").GetDecimal().Should().Be(140.5m);   // 100 + 30 + 10.5 (rejected 20 excluded)
            data.GetProperty("totalCount").GetInt32().Should().Be(6);
            data.GetProperty("approvedRevenue").GetDecimal().Should().Be(100m);
            data.GetProperty("approvedCount").GetInt32().Should().Be(2);
            data.GetProperty("pendingRevenue").GetDecimal().Should().Be(40.5m);
            data.GetProperty("pendingCount").GetInt32().Should().Be(3);

            var days = data.GetProperty("days").EnumerateArray().ToList();
            days.Should().HaveCount(7);
            days.Select(d => d.GetProperty("date").GetString()).Should().Equal(
                "2001-03-01", "2001-03-02", "2001-03-03", "2001-03-04", "2001-03-05", "2001-03-06", "2001-03-07");
            days[0].GetProperty("approvedRevenue").GetDecimal().Should().Be(100m);
            days[0].GetProperty("pendingRevenue").GetDecimal().Should().Be(30m);
            days[0].GetProperty("totalCount").GetInt32().Should().Be(4);
            days[1].GetProperty("totalCount").GetInt32().Should().Be(0);
            days[2].GetProperty("pendingRevenue").GetDecimal().Should().Be(10.5m);
            days[2].GetProperty("pendingCount").GetInt32().Should().Be(2);

            // Single day via one bound only.
            var single = await GetAsync(token, "?dateFrom=2001-03-03");
            var singleData = JsonDocument.Parse(await single.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            singleData.GetProperty("days").GetArrayLength().Should().Be(1);
            singleData.GetProperty("totalRevenue").GetDecimal().Should().Be(10.5m);

            (await GetAsync(token, "?dateFrom=2001-03-07&dateTo=2001-03-01")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await GetAsync(token, "?dateFrom=not-a-date")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            // Test-fixture cleanup so these synthetic rows cannot leak into other tests' list/count assertions.
            await db.Billings.IgnoreQueryFilters().Where(b => b.BillingNumber.StartsWith($"DSH-{suffix}-")).ExecuteDeleteAsync();
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Summary_NonDistributorRole_Returns403()
    {
        (await GetAsync(AuthHelper.AdminToken, "")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetAsync(AuthHelper.SalesRepToken, "")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Summary_Unauthenticated_Returns401()
    {
        (await _client.GetAsync(Url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
