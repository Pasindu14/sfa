using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// End-to-end tests for GET /api/v1/stock/activity (admin stock ledger audit log).
/// Real ledger rows come from a stock transfer and a stock adjustment posted through the API,
/// plus hand-seeded rows for other users (one of them soft-deleted). Every query is scoped to a
/// product unique to the test, because the SQLite database is shared across the collection.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockActivityApiTests
{
    private const string BaseUrl = "/api/v1/stock/activity";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Sri Lanka business dates around "now", so the API-written rows fall inside the window.
    private static readonly DateOnly SlToday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));
    private static readonly string From = SlToday.AddDays(-1).ToString("yyyy-MM-dd");
    private static readonly string To   = SlToday.AddDays(1).ToString("yyyy-MM-dd");

    public StockActivityApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // User 1 is the seeded admin — TransactedBy is an FK to Users.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(1, "Admin", "admin@sfa.com", "System Admin"));
    }

    private record Envelope<T>(bool Success, T Data, PageMeta? Pagination);
    private record PageMeta(int Page, int PageSize, int Total, int TotalPages);
    private record ErrorEnvelope(bool Success, ErrorBody Error);
    private record ErrorBody(string Code, string Message);

    private record ActivityRow(
        int Id, DateTime TransactedAt, int TransactedById, string TransactedByName,
        int DistributorId, string DistributorName, int ProductId, string ProductCode, string ProductDescription,
        int PiecesPerPack, string StockType, string TransactionType, string Direction,
        decimal Quantity, decimal QuantityBefore, decimal QuantityAfter,
        string ReferenceType, int ReferenceId, string? ReferenceNumber, string? Notes);
    private record UserRow(int Id, string Name);
    private record IdResult(int Id, string? TransferNumber, string? AdjustmentNumber);

    private sealed record Seed(
        int SourceId, int TargetId, int ProductId, int RepUserId, string RepUserName,
        int DeletedUserId, string DeletedUserName, string TransferNumber, string AdjustmentNumber);

    /// <summary>
    /// Ledger rows on the test's product:
    ///   source  TransferOut 40 (admin, ST-…) · Sale Out 5 (rep) · Return In 2 (soft-deleted user)
    ///   target  TransferIn 40 (admin, ST-…) · Correction In 10 (admin, SA-…)
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        int sourceId, targetId, productId, repId, deletedId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Distributor Dist(string role, bool active) => new()
            {
                Name = $"LOG-{role}-{s}", Email = $"log-{role}-{s}@test.com", Phone = $"+94{role}{s}", IsActive = active,
            };
            User Usr(string role, bool deleted) => new()
            {
                Name = $"Log {role} {s}", Username = $"log-{role}-{s}", Email = $"log-{role}-{s}@user.com",
                Phone = $"+95{role}{s}", PasswordHash = "x", Role = UserRole.SalesRep, IsDeleted = deleted,
            };

            var source  = Dist("src", false);
            var target  = Dist("tgt", true);
            var product = new Product { Code = $"LOG-{s}", ItemDescription = "Log Cracker", PiecesPerPack = 24, IsActive = true };
            var rep     = Usr("rep", false);
            var gone    = Usr("gone", true);
            db.AddRange(source, target, product, rep, gone);
            await db.SaveChangesAsync();

            db.DistributorStocks.Add(new DistributorStock
            {
                DistributorId = source.Id, ProductId = product.Id, StockType = StockType.Normal,
                QuantityOnHand = 100m, LastUpdatedAt = DateTime.UtcNow,
            });

            StockTransaction Tx(int userId, StockTransactionType type, StockTransactionDirection dir, decimal qty) => new()
            {
                DistributorId = source.Id, ProductId = product.Id, StockType = StockType.Normal,
                TransactionType = type, Direction = dir, Quantity = qty, QuantityBefore = 0m, QuantityAfter = 0m,
                ReferenceType = "Billing", ReferenceId = 0, TransactedAt = DateTime.UtcNow.AddMinutes(-30),
                TransactedBy = userId,
            };
            db.StockTransactions.AddRange(
                Tx(rep.Id,  StockTransactionType.Sale,   StockTransactionDirection.Out, 5m),
                Tx(gone.Id, StockTransactionType.Return, StockTransactionDirection.In,  2m));
            await db.SaveChangesAsync();

            (sourceId, targetId, productId, repId, deletedId) = (source.Id, target.Id, product.Id, rep.Id, gone.Id);
        }

        var transfer = await PostAsync("/api/v1/stock-transfers", new
        {
            sourceDistributorId = sourceId, targetDistributorId = targetId,
            lines = new[] { new { productId, stockType = "Normal", quantity = 40m } },
        });
        var adjustment = await PostAsync("/api/v1/stock-adjustments", new
        {
            distributorId = targetId, reason = "CountCorrection",
            lines = new[] { new { productId, stockType = "Normal", expectedQuantity = 40m, newQuantity = 50m } },
        });

        return new Seed(sourceId, targetId, productId, repId, $"Log rep {s}", deletedId, $"Log gone {s}",
            transfer.TransferNumber!, adjustment.AdjustmentNumber!);
    }

    private async Task<IdResult> PostAsync(string url, object body)
    {
        var response = await _client.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, raw);
        return JsonSerializer.Deserialize<Envelope<IdResult>>(raw, _jsonOpts)!.Data;
    }

    private async Task<Envelope<List<ActivityRow>>> GetActivityAsync(string filters)
    {
        var response = await _client.GetAsync($"{BaseUrl}?from={From}&to={To}&{filters}");
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, raw);
        return JsonSerializer.Deserialize<Envelope<List<ActivityRow>>>(raw, _jsonOpts)!;
    }

    // ── Rows, names, references ────────────────────────────────────────────

    [Fact]
    public async Task GetActivity_ReturnsRowsAcrossDistributors_WithNamesAndReferenceNumbers()
    {
        var seed = await SeedAsync();

        var page = await GetActivityAsync($"productId={seed.ProductId}");

        page.Pagination!.Total.Should().Be(5);
        var rows = page.Data;
        rows.Should().HaveCount(5);
        rows.Select(r => r.DistributorId).Distinct().Should().BeEquivalentTo([seed.SourceId, seed.TargetId]);
        rows.Should().BeInDescendingOrder(r => r.TransactedAt);
        rows.Should().OnlyContain(r => r.ProductCode.StartsWith("LOG-") && r.PiecesPerPack == 24);

        var transferOut = rows.Single(r => r.TransactionType == "TransferOut");
        transferOut.TransactedById.Should().Be(1);
        transferOut.TransactedByName.Should().Be("System Admin");
        transferOut.DistributorName.Should().StartWith("LOG-src-");    // inactive distributor still named
        transferOut.Direction.Should().Be("Out");
        transferOut.ReferenceType.Should().Be("StockTransfer");
        transferOut.ReferenceNumber.Should().Be(seed.TransferNumber);
        transferOut.QuantityBefore.Should().Be(100m);
        transferOut.QuantityAfter.Should().Be(60m);

        rows.Single(r => r.TransactionType == "TransferIn").ReferenceNumber.Should().Be(seed.TransferNumber);

        var correction = rows.Single(r => r.TransactionType == "Correction");
        correction.ReferenceType.Should().Be("StockAdjustment");
        correction.ReferenceNumber.Should().Be(seed.AdjustmentNumber);
        correction.Quantity.Should().Be(10m);

        // Soft-deleted user's row is still listed, with their name.
        rows.Single(r => r.TransactedById == seed.DeletedUserId).TransactedByName.Should().Be(seed.DeletedUserName);
        // Billing reference with no matching bill resolves to null rather than failing.
        rows.Single(r => r.TransactionType == "Sale").ReferenceNumber.Should().BeNull();
    }

    // ── Filters ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivity_Filters_ByUser_Type_Distributor_Direction()
    {
        var seed = await SeedAsync();

        var byUser = (await GetActivityAsync($"productId={seed.ProductId}&userId={seed.RepUserId}")).Data;
        byUser.Should().ContainSingle().Which.TransactionType.Should().Be("Sale");
        byUser[0].TransactedByName.Should().Be(seed.RepUserName);

        var byType = (await GetActivityAsync($"productId={seed.ProductId}&transactionType=TransferIn")).Data;
        byType.Should().ContainSingle().Which.DistributorId.Should().Be(seed.TargetId);

        var byDistributor = (await GetActivityAsync($"productId={seed.ProductId}&distributorId={seed.SourceId}")).Data;
        byDistributor.Should().HaveCount(3).And.OnlyContain(r => r.DistributorId == seed.SourceId);

        var byDirection = (await GetActivityAsync($"productId={seed.ProductId}&direction=Out")).Data;
        byDirection.Select(r => r.TransactionType).Should().BeEquivalentTo(["TransferOut", "Sale"]);

        var combined = (await GetActivityAsync(
            $"productId={seed.ProductId}&distributorId={seed.TargetId}&direction=In&transactionType=Correction&userId=1")).Data;
        combined.Should().ContainSingle().Which.ReferenceNumber.Should().Be(seed.AdjustmentNumber);
    }

    [Fact]
    public async Task GetActivity_Paging_ReturnsRequestedSlice()
    {
        var seed = await SeedAsync();

        var first  = await GetActivityAsync($"productId={seed.ProductId}&page=1&pageSize=2");
        var third  = await GetActivityAsync($"productId={seed.ProductId}&page=3&pageSize=2");

        first.Pagination!.Total.Should().Be(5);
        first.Pagination.TotalPages.Should().Be(3);
        first.Data.Should().HaveCount(2);
        third.Data.Should().ContainSingle();
        first.Data.Select(r => r.Id).Should().NotIntersectWith(third.Data.Select(r => r.Id));
    }

    [Fact]
    public async Task GetActivity_OutsideWindow_ReturnsNothing()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"{BaseUrl}?from=2020-01-01&to=2020-01-31&productId={seed.ProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Envelope<List<ActivityRow>>>(_jsonOpts))!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsers_ListsLedgerUsers_IncludingSoftDeleted_OrderedByName()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"{BaseUrl}/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = (await response.Content.ReadFromJsonAsync<Envelope<List<UserRow>>>(_jsonOpts))!.Data;
        users.Should().Contain(u => u.Id == seed.RepUserId && u.Name == seed.RepUserName);
        users.Should().Contain(u => u.Id == seed.DeletedUserId);
        users.Should().Contain(u => u.Id == 1);
        users.Should().OnlyHaveUniqueItems(u => u.Id);
        users.Select(u => u.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    // ── Validation ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("from=2026-01-01&to=2026-06-01")]      // > 93 days
    [InlineData("from=2026-06-10&to=2026-06-01")]      // to before from
    [InlineData("to=2026-06-01")]                      // from missing
    [InlineData("from=2026-06-01&to=2026-06-02&pageSize=1001")]
    [InlineData("from=2026-06-01&to=2026-06-02&transactionType=Bogus")]
    [InlineData("from=2026-06-01&to=2026-06-02&direction=Up")]
    public async Task GetActivity_InvalidQuery_Returns400(string query)
    {
        var response = await _client.GetAsync($"{BaseUrl}?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error.Code.Should().Be("VALIDATION_FAILED");
    }

    // ── Auth ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("/users")]
    public async Task NonAdmin_Returns403(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.SalesRepToken);

        var response = await client.GetAsync($"{BaseUrl}{path}?from={From}&to={To}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/users")]
    public async Task NoToken_Returns401(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}{path}?from={From}&to={To}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
