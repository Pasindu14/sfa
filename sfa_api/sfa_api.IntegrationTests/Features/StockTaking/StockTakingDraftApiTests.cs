using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.StockTaking.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.StockTaking;

/// <summary>
/// Regression tests for POST /api/v1/stock-taking/portal/submissions (save draft).
/// Re-saving a draft used to re-insert the already-saved lines and fail on the
/// (SubmissionId, ProductId, StockType) unique index.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockTakingDraftApiTests(SfaWebApplicationFactory factory)
{
    private const string DraftUrl = "/api/v1/stock-taking/portal/submissions";

    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private record Envelope<T>(bool Success, T Data);
    private record ErrorEnvelope(bool Success, ErrorBody Error);
    private record ErrorBody(string Code, string Message);
    private record SubmissionResult(int Id, string Status, List<LineResult> Lines);
    private record LineResult(int Id, int ProductId, string ProductCode, string StockType, decimal CountedQuantity);

    private sealed record Seed(HttpClient Client, int PeriodId, int ProductA, int ProductB, int SubmissionOwnerDistributorId);

    private async Task<Seed> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = Guid.NewGuid().ToString("N")[..8];

        var distributor = new Distributor
        {
            Name = $"STD-{s}", Email = $"std-{s}@test.com", Phone = $"+94st{s}", IsActive = true,
        };
        var a = new Product { Code = $"STA-{s}", ItemDescription = "Count A", PiecesPerPack = 12, IsActive = true };
        var b = new Product { Code = $"STB-{s}", ItemDescription = "Count B", PiecesPerPack = 12, IsActive = true };
        // Periods are unique per (Month, Year) — use a far-future year unique to this test run.
        var period = new StockTakingPeriod { Month = Random.Shared.Next(1, 13), Year = Random.Shared.Next(3000, 9000) };
        db.AddRange(distributor, a, b, period);
        await db.SaveChangesAsync();

        var user = new User
        {
            Name = $"Count Distributor {s}", Username = $"countdist-{s}", Email = $"countdist-{s}@sfa.com",
            Phone = $"077{s}", PasswordHash = "placeholder", Role = UserRole.Distributor,
            DistributorId = distributor.Id, IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(user.Id, "Distributor"));

        return new Seed(client, period.Id, a.Id, b.Id, distributor.Id);
    }

    private static object Draft(int periodId, params (int ProductId, string StockType, decimal Counted)[] lines) => new
    {
        periodId,
        lines = lines.Select(l => new { productId = l.ProductId, stockType = l.StockType, countedQuantity = l.Counted }),
    };

    private async Task<SubmissionResult> SaveAsync(HttpClient client, object body)
    {
        var res = await client.PostAsJsonAsync(DraftUrl, body);
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<Envelope<SubmissionResult>>(
            await res.Content.ReadAsStringAsync(), _jsonOpts)!.Data;
    }

    [Fact]
    public async Task SaveDraft_Twice_WithSameProducts_UpdatesLinesInPlace()
    {
        var seed = await SeedAsync();

        var first = await SaveAsync(seed.Client, Draft(seed.PeriodId,
            (seed.ProductA, "Normal", 10m), (seed.ProductB, "Normal", 5m)));
        first.Lines.Should().HaveCount(2);

        // Same products again with new counts — previously a 409 unique-value error.
        var second = await SaveAsync(seed.Client, Draft(seed.PeriodId,
            (seed.ProductA, "Normal", 24m), (seed.ProductB, "Normal", 7m)));

        second.Id.Should().Be(first.Id);
        second.Status.Should().Be("Draft");
        second.Lines.Should().HaveCount(2);
        second.Lines.Single(l => l.ProductId == seed.ProductA).CountedQuantity.Should().Be(24m);
        second.Lines.Single(l => l.ProductId == seed.ProductB).CountedQuantity.Should().Be(7m);
        second.Lines.Should().OnlyContain(l => !string.IsNullOrEmpty(l.ProductCode));

        // Existing line rows are updated, not re-created.
        second.Lines.Select(l => l.Id).Should().BeEquivalentTo(first.Lines.Select(l => l.Id));
    }

    [Fact]
    public async Task SaveDraft_RemovesDroppedLines_AndAddsNewOnes()
    {
        var seed = await SeedAsync();

        await SaveAsync(seed.Client, Draft(seed.PeriodId,
            (seed.ProductA, "Normal", 10m), (seed.ProductB, "Normal", 5m)));

        // Drop B/Normal, keep A/Normal, add A/FreeIssue (same product, other pool).
        var result = await SaveAsync(seed.Client, Draft(seed.PeriodId,
            (seed.ProductA, "Normal", 11m), (seed.ProductA, "FreeIssue", 3m)));

        result.Lines.Should().HaveCount(2);
        result.Lines.Should().NotContain(l => l.ProductId == seed.ProductB);
        result.Lines.Should().ContainSingle(l => l.ProductId == seed.ProductA && l.StockType == "FreeIssue")
            .Which.CountedQuantity.Should().Be(3m);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.StockTakingLines.AsNoTracking()
            .Where(l => l.StockTakingSubmissionId == result.Id).ToListAsync();
        stored.Should().HaveCount(2);
    }

    [Fact]
    public async Task AdminAdjust_AfterSubmit_SetsStockToAdjustedQuantity()
    {
        var seed = await SeedAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.DistributorStocks.Add(new sfa_api.Features.Stock.Entities.DistributorStock
            {
                DistributorId = seed.SubmissionOwnerDistributorId, ProductId = seed.ProductA,
                StockType = StockType.Normal, QuantityOnHand = 5m, LastUpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var draft = await SaveAsync(seed.Client, Draft(seed.PeriodId, (seed.ProductA, "Normal", 1m)));
        var submit = await seed.Client.PostAsync($"{DraftUrl}/{seed.PeriodId}/submit", null);
        submit.StatusCode.Should().Be(HttpStatusCode.OK, await submit.Content.ReadAsStringAsync());

        var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(1, "Admin", "admin@sfa.com", "System Admin"));

        var lineId = draft.Lines.Single().Id;
        var res = await admin.PostAsJsonAsync($"/api/v1/stock-taking/lines/{lineId}/adjust", new { adjustedQuantity = 1m });
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await checkDb.DistributorStocks.AsNoTracking().SingleAsync(x =>
            x.DistributorId == seed.SubmissionOwnerDistributorId && x.ProductId == seed.ProductA);
        stock.QuantityOnHand.Should().Be(1m);
        var line = await checkDb.StockTakingLines.AsNoTracking().SingleAsync(l => l.Id == lineId);
        line.IsAdjusted.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDraft_DuplicateProductInRequest_Returns422()
    {
        var seed = await SeedAsync();

        var res = await seed.Client.PostAsJsonAsync(DraftUrl, Draft(seed.PeriodId,
            (seed.ProductA, "Normal", 10m), (seed.ProductA, "Normal", 4m)));

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = JsonSerializer.Deserialize<ErrorEnvelope>(await res.Content.ReadAsStringAsync(), _jsonOpts)!;
        error.Error.Code.Should().Be("STOCK_TAKING_DUPLICATE_LINE");
    }
}
