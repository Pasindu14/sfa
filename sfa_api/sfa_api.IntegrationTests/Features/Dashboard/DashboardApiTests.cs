using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Dashboard;

/// <summary>
/// End-to-end coverage for GET /api/v1/dashboard/{sales,activity,trend}.
/// <para>
/// The data path is NOT exercised here: the dashboard is built on the sales-summary report and a
/// per-day revenue SUM, both decimal SUMs the SQLite test provider cannot translate (see
/// SalesSummaryApiTests). The composition and every derived number is covered by
/// DashboardServiceTests. What IS covered here: routing, authorization and the future-date guard —
/// none of which aggregate anything.
/// </para>
/// </summary>
[Collection(SfaApiCollection.Name)]
public class DashboardApiTests(SfaWebApplicationFactory factory)
{
    private const string Base = "/api/v1/dashboard";

    private HttpClient ClientFor(string? token)
    {
        var client = factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await ClientFor(null).GetAsync($"{Base}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns403_ForSalesRep()
    {
        var response = await ClientFor(AuthHelper.SalesRepToken).GetAsync($"{Base}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Returns403_ForSupervisor()
    {
        var response = await ClientFor(AuthHelper.SupervisorToken).GetAsync($"{Base}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Returns422_ForFutureDate()
    {
        var response = await ClientFor(AuthHelper.AdminToken).GetAsync($"{Base}/activity?date=2099-01-01");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadAsStringAsync()).Should().Contain("DASHBOARD_FUTURE_DATE");
    }
}
