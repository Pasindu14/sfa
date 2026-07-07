using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;
using sfa_api.Features.Billings.Services;
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
/// Verifies the discount-total snapshot columns added to Billing:
///   ItemWiseTotalDiscount — Σ DiscountAmount for Sale lines only
///   TotalDiscount         — ItemWiseTotalDiscount + BillDiscountAmount
/// Exercised end-to-end through POST /api/v1/billings → real BillingService.CreateAsync.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class BillingsApiTests
{
    private const string BaseUrl = "/api/v1/billings";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BillingsApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Shared seed graph (seeded once for the whole collection) ────────────
    // SalesRepToken is userId=200, so the geo assignment must be keyed on 200.
    private static volatile bool _seeded;
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static int _outletId;
    private static int _productAId;   // Sale
    private static int _productBId;   // Sale
    private static int _productCId;   // FreeIssue (company-funded)
    private static int _productDId;   // Return (MarketResell)
    private static string _repToken = string.Empty;   // JWT minted for the seeded rep (FKs enforced under SQLite)
    private static int _repId;                         // seeded rep id — for direct service-level calls

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await _gate.WaitAsync();
        try
        {
            if (_seeded) return;

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var suffix = Guid.NewGuid().ToString("N")[..8];

            var region = new Region { Name = $"BillRegion-{suffix}", IsActive = true };
            db.Regions.Add(region);
            await db.SaveChangesAsync();

            var area = new Area { Name = $"BillArea-{suffix}", RegionId = region.Id, IsActive = true };
            db.Areas.Add(area);
            await db.SaveChangesAsync();

            var territory = new Territory
            {
                Name = $"BillTerr-{suffix}", AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Territories.Add(territory);
            await db.SaveChangesAsync();

            var division = new Division
            {
                Name = $"BillDiv-{suffix}", TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Divisions.Add(division);
            await db.SaveChangesAsync();

            var route = new RouteEntity
            {
                Name = $"BillRoute-{suffix}", PinColor = "#3366FF",
                DivisionId = division.Id, TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Routes.Add(route);
            await db.SaveChangesAsync();

            var outlet = new Outlet
            {
                Name = $"BillOutlet-{suffix}", Address = "1 Test St", Tel = $"01{suffix}",
                NicNo = $"NIC{suffix}", RouteId = route.Id, DivisionId = division.Id,
                TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Outlets.Add(outlet);
            await db.SaveChangesAsync();

            var distributor = new Distributor
            {
                Name = $"BillDist-{suffix}", Address = "2 Dist St", Phone = $"02{suffix}",
                Email = $"dist-{suffix}@sfa.com", Alias = Math.Abs(Guid.NewGuid().GetHashCode()),
                Category = "A", TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Distributors.Add(distributor);
            await db.SaveChangesAsync();

            // SQLite enforces FKs, so the rep referenced by the JWT must be a real row.
            // Seed the user, let SQLite assign the id, then mint the token for that id.
            var rep = new User
            {
                Name = "Test Rep", Username = $"rep-{suffix}", Email = $"rep-{suffix}@sfa.com",
                Phone = $"07{suffix}", PasswordHash = "placeholder",
                Role = UserRole.SalesRep, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(rep);
            await db.SaveChangesAsync();

            var pA = new Product { Code = $"PA-{suffix}", ItemDescription = "Sale Product A", IsActive = true };
            var pB = new Product { Code = $"PB-{suffix}", ItemDescription = "Sale Product B", IsActive = true };
            var pC = new Product { Code = $"PC-{suffix}", ItemDescription = "FreeIssue Product C", IsActive = true };
            var pD = new Product { Code = $"PD-{suffix}", ItemDescription = "Return Product D", IsActive = true };
            db.Products.AddRange(pA, pB, pC, pD);
            await db.SaveChangesAsync();

            db.DistributorStocks.AddRange(
                new DistributorStock { DistributorId = distributor.Id, ProductId = pA.Id, StockType = StockType.Normal,    QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow },
                new DistributorStock { DistributorId = distributor.Id, ProductId = pB.Id, StockType = StockType.Normal,    QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow },
                new DistributorStock { DistributorId = distributor.Id, ProductId = pC.Id, StockType = StockType.FreeIssue, QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow },
                new DistributorStock { DistributorId = distributor.Id, ProductId = pD.Id, StockType = StockType.Normal,    QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            db.UserGeoAssignments.Add(new UserGeoAssignment
            {
                UserId = rep.Id, DivisionId = division.Id, TerritoryId = territory.Id,
                AreaId = area.Id, RegionId = region.Id, IsActive = true,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow)
            });
            await db.SaveChangesAsync();

            _outletId   = outlet.Id;
            _productAId = pA.Id;
            _productBId = pB.Id;
            _productCId = pC.Id;
            _productDId = pD.Id;
            _repToken   = AuthHelper.GenerateToken(rep.Id, "SalesRep");
            _repId      = rep.Id;
            _seeded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string Today() => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    private async Task<(HttpStatusCode Status, JsonElement Data, string Raw)> PostBillingAsync(object payload)
    {
        // POST /billings requires an X-Idempotency-Key (the client-generated bill id);
        // a fresh key per call mirrors a real client creating distinct bills.
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var resp = await _client.SendAsync(req);
        var raw  = await resp.Content.ReadAsStringAsync();
        if (resp.StatusCode != HttpStatusCode.Created)
            return (resp.StatusCode, default, raw);

        using var doc = JsonDocument.Parse(raw);
        // Clone so the element stays valid after the JsonDocument is disposed.
        var data = doc.RootElement.GetProperty("data").Clone();
        return (resp.StatusCode, data, raw);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync — discount totals
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBilling_WithMixedLineTypes_SumsSaleLineDiscountsOnly()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        // Sale A: 3 × 9.99, 10%   → discount 2.997→3.00, line 26.97
        // Sale B: 7 × 4.55, 12.5% → discount 3.98125→3.98, line 27.87
        // FreeIssue C: 2 × 5.00 (company) → discount 0, excluded
        // Return D (MarketResell): 1 × 20.00, 5% → discount 1.00, EXCLUDED from item-wise total
        // subTotal           = 26.97 + 27.87 = 54.84
        // itemWiseTotalDisc  = 3.00 + 3.98   = 6.98   (Sale lines only)
        // billDiscount (10%) = round(5.484)  = 5.48
        // totalDiscount      = 6.98 + 5.48   = 12.46
        // returnValue        = 19.00
        // totalAmount        = 54.84 − 5.48 − 19.00 = 30.36
        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 10m,
            notes = "itemwise discount test",
            billingDate = Today(),
            items = new object[]
            {
                new { productId = _productAId, quantity = 3m,  unitPrice = 9.99m,  discountRate = 10m,   billingItemType = 0 },
                new { productId = _productBId, quantity = 7m,  unitPrice = 4.55m,  discountRate = 12.5m, billingItemType = 0 },
                new { productId = _productCId, quantity = 2m,  unitPrice = 5.00m,  discountRate = 0m,    billingItemType = 2, freeIssueSource = 0 },
                new { productId = _productDId, quantity = 1m,  unitPrice = 20.00m, discountRate = 5m,    billingItemType = 1, returnType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);

        data.GetProperty("subTotalAmount").GetDecimal().Should().Be(54.84m);
        data.GetProperty("billDiscountAmount").GetDecimal().Should().Be(5.48m);
        data.GetProperty("returnValue").GetDecimal().Should().Be(19m);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(30.36m);

        // The new fields under test
        data.GetProperty("itemWiseTotalDiscount").GetDecimal().Should().Be(6.98m,
            "only Sale-line discounts (3.00 + 3.98) count — the Return line's 1.00 and the FreeIssue line's 0 are excluded");
        data.GetProperty("totalDiscount").GetDecimal().Should().Be(12.46m,
            "TotalDiscount = ItemWiseTotalDiscount (6.98) + BillDiscountAmount (5.48)");
    }

    [Fact]
    public async Task CreateBilling_NoDiscounts_YieldsZeroDiscountTotals()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 100m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);

        data.GetProperty("subTotalAmount").GetDecimal().Should().Be(100m);
        data.GetProperty("billDiscountAmount").GetDecimal().Should().Be(0m);
        data.GetProperty("itemWiseTotalDiscount").GetDecimal().Should().Be(0m);
        data.GetProperty("totalDiscount").GetDecimal().Should().Be(0m);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task CreateBilling_WithoutIdempotencyKeyHeader_Returns400()
    {
        // The X-Idempotency-Key header is mandatory: without it the duplicate-bill /
        // double-stock-deduction guard cannot engage, so the request must be rejected
        // up front rather than silently creating an unguarded bill.
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 100m, discountRate = 0m, billingItemType = 0 }
            }
        };

        // Deliberately POST without the header (bypasses the PostBillingAsync helper).
        var resp = await _client.PostAsJsonAsync(BaseUrl, payload);
        var raw  = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);
        raw.Should().Contain("X-Idempotency-Key");
    }

    [Fact]
    public async Task CreateBilling_ReturnsPlusBillDiscountExceedSubtotal_Rejected()
    {
        // finding #4: a full-value market return PLUS a bill-level discount drives the grand total
        // negative. The request passes the validator's return<=sale check (1000 <= 1000), so the
        // service-level guard must catch it — otherwise negative revenue is persisted.
        await EnsureSeededAsync();
        SetToken(_repToken);

        // Sale 10×100 = 1000 subtotal; MarketResell return 10×100 = 1000; 10% bill discount = 100.
        // totalAmount = 1000 − 100 − 1000 = −100.
        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 10m,
            billingDate = Today(),
            items = new object[]
            {
                new { productId = _productAId, quantity = 10m, unitPrice = 100m, discountRate = 0m, billingItemType = 0 },
                new { productId = _productBId, quantity = 10m, unitPrice = 100m, discountRate = 0m, billingItemType = 1, returnType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILL_TOTAL_NEGATIVE");
    }

    // ─────────────────────────────────────────────────
    // CreateAsync — idempotency / duplicate-request backstop (audit finding #2)
    // Calls the service directly to bypass the HTTP IdempotencyMiddleware cache, so this
    // exercises the DB-level backstop (clientBillId fast-path + unique index) in isolation —
    // it fails if that backstop is removed, even though the middleware would mask it over HTTP.
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBilling_SameClientBillId_ReturnsSameBill_AndCreatesOnlyOne()
    {
        await EnsureSeededAsync();

        var clientBillId = Guid.NewGuid().ToString();
        var request = new CreateBillingRequest(
            OutletId: _outletId,
            BillDiscountRate: 0m,
            Notes: "idempotency backstop test",
            Items: new List<CreateBillingItemRequest>
            {
                new(_productAId, Quantity: 1m, UnitPrice: 50m, BillingItemType: BillingItemType.Sale)
            },
            BillingDate: DateOnly.FromDateTime(DateTime.UtcNow));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillingService>();

        // First create persists the bill carrying the client bill id.
        var first = await service.CreateAsync(request, _repId, clientBillId);
        // A retry with the SAME client bill id must return the same bill, not create a second.
        var second = await service.CreateAsync(request, _repId, clientBillId);

        second.Id.Should().Be(first.Id,
            "a retry carrying the same clientBillId must be idempotent, not create a duplicate bill");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Billings.Count(b => b.ClientBillId == clientBillId)
          .Should().Be(1, "the clientBillId fast-path + unique index must prevent a duplicate bill");
    }

    [Fact]
    public async Task CreateBilling_DifferentClientBillIds_CreateDistinctBills()
    {
        await EnsureSeededAsync();

        var request = new CreateBillingRequest(
            OutletId: _outletId,
            BillDiscountRate: 0m,
            Notes: "distinct-key control",
            Items: new List<CreateBillingItemRequest>
            {
                new(_productAId, Quantity: 1m, UnitPrice: 25m, BillingItemType: BillingItemType.Sale)
            },
            BillingDate: DateOnly.FromDateTime(DateTime.UtcNow));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var a = await service.CreateAsync(request, _repId, Guid.NewGuid().ToString());
        var b = await service.CreateAsync(request, _repId, Guid.NewGuid().ToString());

        b.Id.Should().NotBe(a.Id,
            "different clientBillIds are genuinely different submissions and must each create a bill");
    }
}
