using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// End-to-end tests for the stock-reconciliation endpoints (review finding #4).
///
/// The on-demand RUN endpoint (GET /reconciliation) is NOT integration-tested here: it re-sums the
/// ledger with SUM over decimal, which the SQLite test provider cannot translate (same limitation the
/// BinCard data path hits). That math is covered exhaustively by the unit tests. These tests exercise
/// the SQLite-safe read path — persistence round-trip, controller wiring, auth and DTO mapping — which
/// also validates that the new EF entity configuration produces a working schema.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockReconciliationApiTests
{
    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public StockReconciliationApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.AdminToken);
    }

    private record Envelope<T>(bool Success, T Data);

    [Fact]
    public async Task GetLatest_ReturnsMostRecentPersistedRun_WithFlags()
    {
        var marker = $"manual:test-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.StockReconciliationRuns.Add(new StockReconciliationRun
            {
                RunAt = DateTime.UtcNow,
                TriggeredBy = marker,
                GroupsChecked = 3,
                DiscrepancyCount = 1,
                Flags = new List<StockReconciliationFlag>
                {
                    new()
                    {
                        DistributorId = 1234, ProductId = 5678, StockType = StockType.Normal,
                        Kind = StockDiscrepancyKind.LedgerSumVsBalance,
                        ExpectedQuantity = 70m, ActualQuantity = 88m, Delta = 18m
                    }
                }
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/stock/reconciliation/latest");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");
        data.GetProperty("triggeredBy").GetString().Should().Be(marker);   // sequential collection → newest run is ours
        data.GetProperty("groupsChecked").GetInt32().Should().Be(3);
        data.GetProperty("discrepancyCount").GetInt32().Should().Be(1);

        var flag = data.GetProperty("discrepancies").EnumerateArray().Single();
        flag.GetProperty("distributorId").GetInt32().Should().Be(1234);
        flag.GetProperty("kind").GetString().Should().Be(nameof(StockDiscrepancyKind.LedgerSumVsBalance));
        flag.GetProperty("delta").GetDecimal().Should().Be(18m);
        // No Distributor/Product seeded → enrichment falls back to the "#id" placeholder.
        flag.GetProperty("distributorName").GetString().Should().Be("#1234");
    }

    [Fact]
    public async Task GetLatest_AsNonAdmin_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.SalesRepToken);

        var response = await client.GetAsync("/api/v1/stock/reconciliation/latest");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // The on-demand RUN endpoint re-sums the ledger (SUM over decimal) which SQLite cannot translate.
    // Covered by StockReconciliationTests (math) + StockReconciliationServiceTests (orchestration).
    [Fact(Skip = "SQLite test provider cannot translate SUM over decimal; ledger re-sum covered by StockReconciliationTests. Verified on PostgreSQL in production.")]
    public Task Run_ComputesDiscrepanciesFromLedger() => Task.CompletedTask;
}
