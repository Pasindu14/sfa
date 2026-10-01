using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.IntegrationTests.Features.Billings;

/// <summary>
/// Duplicate-protection fixes on the Billings API:
///   • PATCH /{id}/cancel — a bill the distributor already REJECTED (stock already returned by the reject)
///     can still be cancelled by the rep, but its stock must NOT be reversed a second time.
///   • GET /by-client-id/{clientBillId} — the phone asks whether a bill whose sync response was lost
///     actually reached the server. Own bills only; anything else reads as 404 (no existence leak).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class BillingCancelAndLookupApiTests
{
    private const string BaseUrl = "/api/v1/billings";
    private const int StartingStock = 1_000;
    private const decimal BillQty = 7m;

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BillingCancelAndLookupApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Shared seed graph ───────────────────────────────────────────────────
    private static volatile bool _seeded;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static int _outletId, _distributorId;
    private static string _repToken = string.Empty;
    private static string _otherRepToken = string.Empty;
    private static string _distToken = string.Empty;

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await Gate.WaitAsync();
        try
        {
            if (_seeded) return;

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = Guid.NewGuid().ToString("N")[..8];

            var region = new Region { Name = $"CLRegion-{s}", IsActive = true };
            db.Regions.Add(region);
            await db.SaveChangesAsync();
            var area = new Area { Name = $"CLArea-{s}", RegionId = region.Id, IsActive = true };
            db.Areas.Add(area);
            await db.SaveChangesAsync();
            var territory = new Territory { Name = $"CLTerr-{s}", AreaId = area.Id, RegionId = region.Id, IsActive = true };
            db.Territories.Add(territory);
            await db.SaveChangesAsync();
            var division = new Division
            {
                Name = $"CLDiv-{s}", TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Divisions.Add(division);
            await db.SaveChangesAsync();
            var route = new RouteEntity
            {
                Name = $"CLRoute-{s}", PinColor = "#3366FF", DivisionId = division.Id, TerritoryId = territory.Id,
                AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Routes.Add(route);
            await db.SaveChangesAsync();
            var outlet = new Outlet
            {
                Name = $"CLOutlet-{s}", Address = "1 CL St", Tel = $"01{s}", NicNo = $"NIC{s}",
                RouteId = route.Id, DivisionId = division.Id, TerritoryId = territory.Id,
                AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Outlets.Add(outlet);
            var distributor = new Distributor
            {
                Name = $"CLDist-{s}", Address = "2 CL St", Phone = $"02{s}", Email = $"cldist-{s}@sfa.com",
                Alias = Math.Abs(Guid.NewGuid().GetHashCode()), Category = "A",
                TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Distributors.Add(distributor);
            await db.SaveChangesAsync();

            var phoneSeq = 0;
            User NewUser(string label, UserRole role, int? distributorId = null) => new()
            {
                Name = $"CL {label}", Username = $"cl{label}-{s}", Email = $"cl{label}-{s}@sfa.com",
                Phone = $"07{++phoneSeq}{s}", PasswordHash = "placeholder", Role = role,
                DistributorId = distributorId, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            var rep = NewUser("rep", UserRole.SalesRep);
            var otherRep = NewUser("otherrep", UserRole.SalesRep);
            var distUser = NewUser("dist", UserRole.Distributor, distributor.Id);
            db.Users.AddRange(rep, otherRep, distUser);
            await db.SaveChangesAsync();

            foreach (var r in new[] { rep, otherRep })
                db.UserGeoAssignments.Add(new UserGeoAssignment
                {
                    UserId = r.Id, DivisionId = division.Id, TerritoryId = territory.Id, AreaId = area.Id,
                    RegionId = region.Id, IsActive = true, EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            await db.SaveChangesAsync();

            _outletId = outlet.Id;
            _distributorId = distributor.Id;
            _repToken = AuthHelper.GenerateToken(rep.Id, "SalesRep");
            _otherRepToken = AuthHelper.GenerateToken(otherRep.Id, "SalesRep");
            _distToken = AuthHelper.GenerateToken(distUser.Id, "Distributor");
            _seeded = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>A fresh stocked product per test so stock assertions are isolated from other tests.</summary>
    private async Task<int> SeedProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product
        {
            Code = $"CL-{Guid.NewGuid().ToString("N")[..10]}", ItemDescription = "Cancel Product", IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.DistributorStocks.Add(new DistributorStock
        {
            DistributorId = _distributorId, ProductId = product.Id, StockType = StockType.Normal,
            QuantityOnHand = StartingStock, LastUpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return product.Id;
    }

    // ── HTTP / DB helpers ───────────────────────────────────────────────────

    private sealed record Reply(HttpStatusCode Status, JsonElement Root, string Raw)
    {
        public JsonElement Data => Root.GetProperty("data");
        public string? ErrorCode => Root.TryGetProperty("error", out var e) ? e.GetProperty("code").GetString() : null;
    }

    private async Task<Reply> SendAsync(HttpRequestMessage req)
    {
        var resp = await _client.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        return new Reply(resp.StatusCode, doc.RootElement.Clone(), raw);
    }

    private Task<Reply> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (body is not null) req.Content = JsonContent.Create(body);
        return SendAsync(req);
    }

    /// <summary>Creates a bill as the rep, using <paramref name="clientBillId"/> as the idempotency key.</summary>
    private async Task<int> CreateBillAsync(int productId, string token, string clientBillId)
    {
        SetToken(token);
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = JsonContent.Create(new
            {
                outletId = _outletId,
                billDiscountRate = 0m,
                billingDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                latitude = 6.9271,
                longitude = 79.8612,
                items = new object[]
                {
                    new { productId, quantity = BillQty, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
                }
            })
        };
        req.Headers.Add("X-Idempotency-Key", clientBillId);
        var r = await SendAsync(req);
        r.Status.Should().Be(HttpStatusCode.Created, r.Raw);
        return r.Data.GetProperty("id").GetInt32();
    }

    private Task<Reply> CancelAsync(int billingId, string token)
    {
        SetToken(token);
        return SendAsync(HttpMethod.Patch, $"{BaseUrl}/{billingId}/cancel");
    }

    private async Task<decimal> StockAsync(int productId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.DistributorStocks.AsNoTracking().SingleAsync(x =>
            x.DistributorId == _distributorId && x.ProductId == productId && x.StockType == StockType.Normal);
        return row.QuantityOnHand;
    }

    private async Task<int> ReversalCountAsync(int billingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.StockTransactions.AsNoTracking().CountAsync(t =>
            t.ReferenceType == "Billing" && t.ReferenceId == billingId
            && t.TransactionType == StockTransactionType.BillingReversal);
    }

    // ── Cancel: stock reversed exactly once ─────────────────────────────────

    [Fact]
    public async Task Cancel_PendingBill_ReversesStockExactlyOnce()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var billingId = await CreateBillAsync(productId, _repToken, Guid.NewGuid().ToString());
        (await StockAsync(productId)).Should().Be(StartingStock - BillQty);

        var r = await CancelAsync(billingId, _repToken);

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("repStatus").GetString().Should().Be("Cancelled");
        (await StockAsync(productId)).Should().Be(StartingStock);
        (await ReversalCountAsync(billingId)).Should().Be(1);
    }

    [Fact]
    public async Task Cancel_BillRejectedByDistributor_DoesNotReverseStockAgain()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var billingId = await CreateBillAsync(productId, _repToken, Guid.NewGuid().ToString());

        SetToken(_distToken);
        var reject = await SendAsync(HttpMethod.Patch, $"{BaseUrl}/{billingId}/reject", new { reason = "Out of stock" });
        reject.Status.Should().Be(HttpStatusCode.OK, reject.Raw);
        reject.Data.GetProperty("distributorStatus").GetString().Should().Be("Rejected");
        reject.Data.GetProperty("repStatus").GetString().Should().Be("Submitted");
        (await StockAsync(productId)).Should().Be(StartingStock, "the reject already returned the units");

        var cancel = await CancelAsync(billingId, _repToken);

        cancel.Status.Should().Be(HttpStatusCode.OK, cancel.Raw);
        cancel.Data.GetProperty("repStatus").GetString().Should().Be("Cancelled");
        cancel.Data.GetProperty("distributorStatus").GetString().Should().Be("Rejected");
        (await StockAsync(productId)).Should().Be(StartingStock, "the cancel must not credit the same units a second time");
        (await ReversalCountAsync(billingId)).Should().Be(1, "only the reject wrote a reversal");
    }

    [Fact]
    public async Task Cancel_Twice_SecondReturns422NotCancellable_AndStockIsNotReversedAgain()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var billingId = await CreateBillAsync(productId, _repToken, Guid.NewGuid().ToString());
        (await CancelAsync(billingId, _repToken)).Status.Should().Be(HttpStatusCode.OK);

        var again = await CancelAsync(billingId, _repToken);

        again.Status.Should().Be(HttpStatusCode.UnprocessableEntity, again.Raw);
        again.ErrorCode.Should().Be("BILLING_NOT_CANCELLABLE");
        (await StockAsync(productId)).Should().Be(StartingStock);
        (await ReversalCountAsync(billingId)).Should().Be(1);
    }

    [Fact]
    public async Task Cancel_RejectedThenCancelledTwice_SecondReturns422NotCancellable()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var billingId = await CreateBillAsync(productId, _repToken, Guid.NewGuid().ToString());
        SetToken(_distToken);
        (await SendAsync(HttpMethod.Patch, $"{BaseUrl}/{billingId}/reject", new { reason = "No" }))
            .Status.Should().Be(HttpStatusCode.OK);
        (await CancelAsync(billingId, _repToken)).Status.Should().Be(HttpStatusCode.OK);

        var again = await CancelAsync(billingId, _repToken);

        again.Status.Should().Be(HttpStatusCode.UnprocessableEntity, again.Raw);
        again.ErrorCode.Should().Be("BILLING_NOT_CANCELLABLE");
        (await StockAsync(productId)).Should().Be(StartingStock);
    }

    [Fact]
    public async Task Cancel_ApprovedBill_StillReversesStockExactlyOnce()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var billingId = await CreateBillAsync(productId, _repToken, Guid.NewGuid().ToString());
        SetToken(_distToken);
        (await SendAsync(HttpMethod.Patch, $"{BaseUrl}/{billingId}/approve")).Status.Should().Be(HttpStatusCode.OK);

        var r = await CancelAsync(billingId, _repToken);

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        (await StockAsync(productId)).Should().Be(StartingStock);
        (await ReversalCountAsync(billingId)).Should().Be(1);
    }

    // ── GET /by-client-id/{clientBillId} ────────────────────────────────────

    [Fact]
    public async Task GetByClientBillId_OwnBill_Returns200WithBillSummary()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var clientBillId = Guid.NewGuid().ToString();
        var billingId = await CreateBillAsync(productId, _repToken, clientBillId);

        SetToken(_repToken);
        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{clientBillId}");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Root.GetProperty("success").GetBoolean().Should().BeTrue();
        r.Data.GetProperty("id").GetInt32().Should().Be(billingId);
        r.Data.GetProperty("repStatus").GetString().Should().Be("Submitted");
        r.Data.GetProperty("distributorStatus").GetString().Should().Be("Pending");
        r.Data.GetProperty("billingNumber").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetByClientBillId_AfterCancel_ReflectsCancelledStatus()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var clientBillId = Guid.NewGuid().ToString();
        var billingId = await CreateBillAsync(productId, _repToken, clientBillId);
        (await CancelAsync(billingId, _repToken)).Status.Should().Be(HttpStatusCode.OK);

        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{clientBillId}");

        r.Status.Should().Be(HttpStatusCode.OK, r.Raw);
        r.Data.GetProperty("repStatus").GetString().Should().Be("Cancelled");
    }

    [Fact]
    public async Task GetByClientBillId_UnknownId_Returns404BillingNotFound()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{Guid.NewGuid()}");

        r.Status.Should().Be(HttpStatusCode.NotFound, r.Raw);
        r.ErrorCode.Should().Be("BILLING_NOT_FOUND");
    }

    [Fact]
    public async Task GetByClientBillId_AnotherRepsBill_Returns404NotForbidden()
    {
        await EnsureSeededAsync();
        var productId = await SeedProductAsync();
        var clientBillId = Guid.NewGuid().ToString();
        await CreateBillAsync(productId, _otherRepToken, clientBillId);

        SetToken(_repToken);
        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{clientBillId}");

        r.Status.Should().Be(HttpStatusCode.NotFound, r.Raw);
        r.ErrorCode.Should().Be("BILLING_NOT_FOUND");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Supervisor")]
    [InlineData("Distributor")]
    public async Task GetByClientBillId_NonSalesRep_IsForbidden(string role)
    {
        SetToken(AuthHelper.GenerateToken(9400, role));

        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{Guid.NewGuid()}");

        r.Status.Should().Be(HttpStatusCode.Forbidden, r.Raw);
    }

    [Fact]
    public async Task GetByClientBillId_Anonymous_IsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var r = await SendAsync(HttpMethod.Get, $"{BaseUrl}/by-client-id/{Guid.NewGuid()}");

        r.Status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
