using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Reports;

/// <summary>
/// End-to-end coverage for GET /api/v1/reports/sales-summary.
/// <para>
/// The data path is NOT exercised here. Every measure in this report is a decimal SUM pushed into
/// SQL, and the SQLite test provider cannot translate SUM over decimal (it stores decimals as TEXT)
/// — the same wall BinCardApiTests and StockReconciliationApiTests hit. Note the failure is raised
/// while EF builds and executes the statement, so even an empty-result request throws; there is no
/// "assert it returns zero rows" workaround. The column math is covered by SalesSummaryServiceTests
/// and the proration by SalesSummaryProrationTests.
/// </para>
/// <para>
/// What IS covered here: routing, authorization, model binding of the groupBy enum, and validation
/// — none of which aggregate anything.
/// </para>
/// </summary>
[Collection(SfaApiCollection.Name)]
public class SalesSummaryApiTests
{
    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string Base = "/api/v1/reports/sales-summary";

    public SalesSummaryApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = ClientFor(AuthHelper.AdminToken);
    }

    /// <summary>
    /// Always build from the factory — a bare <c>new HttpClient()</c> would try the real network
    /// instead of the in-memory test server. Pass null for an unauthenticated client.
    /// </summary>
    private HttpClient ClientFor(string? token)
    {
        var client = _factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact(Skip = "SQLite test provider cannot translate SUM over decimal; column math covered by SalesSummaryServiceTests. Verified on PostgreSQL in production.")]
    public async Task GetSalesSummary_AggregatesBillsAndTargets()
    {
        var response = await _client.GetAsync($"{Base}?from=2026-04-01&to=2026-04-30&groupBy=SalesRep");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Validation ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns400_WhenToBeforeFrom()
    {
        var response = await _client.GetAsync($"{Base}?from=2026-04-30&to=2026-04-01&groupBy=SalesRep");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns400_WhenRangeExceedsTheCap()
    {
        var response = await _client.GetAsync($"{Base}?from=2025-01-01&to=2026-06-01&groupBy=SalesRep");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns400_WhenGroupByIsUnrecognised()
    {
        var response = await _client.GetAsync($"{Base}?from=2026-04-01&to=2026-04-30&groupBy=Nonsense");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns400_WhenFilterIdIsNotPositive()
    {
        var response = await _client.GetAsync($"{Base}?from=2026-04-01&to=2026-04-30&groupBy=SalesRep&salesRepId=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Authorization ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await ClientFor(null).GetAsync($"{Base}?from=2026-04-01&to=2026-04-30");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns403_ForSalesRep()
    {
        var response = await ClientFor(AuthHelper.SalesRepToken)
            .GetAsync($"{Base}?from=2026-04-01&to=2026-04-30&groupBy=SalesRep");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Returns403_ForSupervisor()
    {
        var response = await ClientFor(AuthHelper.SupervisorToken)
            .GetAsync($"{Base}?from=2026-04-01&to=2026-04-30&groupBy=SalesRep");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
