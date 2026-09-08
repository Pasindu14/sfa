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
/// PATCH /api/v1/billings/{id}/adjust-items — the distributor reduces quantities on a pending bill
/// before approving it. Each reduction must:
///   • lower the parent Sale/FreeIssue line and reprice it on its own unit price + discount rate,
///   • append a DistributorReturn line for the returned quantity on the same pricing basis,
///   • credit the returned quantity back to the pool the parent line drew from,
///   • leave TotalAmount reduced exactly once (the return line is informational, never subtracted).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class BillingAdjustmentApiTests
{
    private const string BaseUrl = "/api/v1/billings";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BillingAdjustmentApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Shared seed graph ───────────────────────────────────────────────────
    private static volatile bool _seeded;
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static int _outletId;
    private static int _distributorId;
    private static int _saleProductId;        // Normal pool
    private static int _focCompanyProductId;  // FreeIssue pool
    private static string _repToken = string.Empty;
    private static string _distToken = string.Empty;
    private static string _otherDistToken = string.Empty;   // a distributor that does NOT own the bill

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

            var region = new Region { Name = $"AdjRegion-{suffix}", IsActive = true };
            db.Regions.Add(region);
            await db.SaveChangesAsync();

            var area = new Area { Name = $"AdjArea-{suffix}", RegionId = region.Id, IsActive = true };
            db.Areas.Add(area);
            await db.SaveChangesAsync();

            var territory = new Territory
            {
                Name = $"AdjTerr-{suffix}", AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            // Distributors are unique per territory, so the "not my bill" distributor needs its own.
            var otherTerritory = new Territory
            {
                Name = $"AdjTerr2-{suffix}", AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Territories.AddRange(territory, otherTerritory);
            await db.SaveChangesAsync();

            var division = new Division
            {
                Name = $"AdjDiv-{suffix}", TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Divisions.Add(division);
            await db.SaveChangesAsync();

            var route = new RouteEntity
            {
                Name = $"AdjRoute-{suffix}", PinColor = "#3366FF",
                DivisionId = division.Id, TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Routes.Add(route);
            await db.SaveChangesAsync();

            var outlet = new Outlet
            {
                Name = $"AdjOutlet-{suffix}", Address = "1 Adj St", Tel = $"01{suffix}",
                NicNo = $"NIC{suffix}", RouteId = route.Id, DivisionId = division.Id,
                TerritoryId = territory.Id, AreaId = area.Id, RegionId = region.Id, IsActive = true
            };
            db.Outlets.Add(outlet);
            await db.SaveChangesAsync();

            var distributor = new Distributor
            {
                Name = $"AdjDist-{suffix}", Address = "2 Dist St", Phone = $"02{suffix}",
                Email = $"adjdist-{suffix}@sfa.com", Alias = Math.Abs(Guid.NewGuid().GetHashCode()),
                Category = "A", TerritoryId = territory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            var otherDistributor = new Distributor
            {
                Name = $"AdjOther-{suffix}", Address = "3 Dist St", Phone = $"03{suffix}",
                Email = $"adjother-{suffix}@sfa.com", Alias = Math.Abs(Guid.NewGuid().GetHashCode()),
                Category = "A", TerritoryId = otherTerritory.Id, AreaId = area.Id,
                RegionId = region.Id, IsActive = true
            };
            db.Distributors.AddRange(distributor, otherDistributor);
            await db.SaveChangesAsync();

            // SQLite enforces FKs, so every user the JWT names must be a real row.
            var rep = new User
            {
                Name = "Adj Rep", Username = $"adjrep-{suffix}", Email = $"adjrep-{suffix}@sfa.com",
                Phone = $"07{suffix}", PasswordHash = "placeholder",
                Role = UserRole.SalesRep, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            var distUser = new User
            {
                Name = "Adj Distributor", Username = $"adjdb-{suffix}", Email = $"adjdb-{suffix}@sfa.com",
                Phone = $"08{suffix}", PasswordHash = "placeholder",
                Role = UserRole.Distributor, DistributorId = distributor.Id, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            var otherDistUser = new User
            {
                Name = "Other Distributor", Username = $"adjodb-{suffix}", Email = $"adjodb-{suffix}@sfa.com",
                Phone = $"09{suffix}", PasswordHash = "placeholder",
                Role = UserRole.Distributor, DistributorId = otherDistributor.Id, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            db.Users.AddRange(rep, distUser, otherDistUser);
            await db.SaveChangesAsync();

            var sale = new Product { Code = $"ADJS-{suffix}", ItemDescription = "Adj Sale Product", IsActive = true };
            var foc  = new Product { Code = $"ADJF-{suffix}", ItemDescription = "Adj FOC Product",  IsActive = true };
            db.Products.AddRange(sale, foc);
            await db.SaveChangesAsync();

            db.DistributorStocks.AddRange(
                new DistributorStock { DistributorId = distributor.Id, ProductId = sale.Id, StockType = StockType.Normal,    QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow },
                new DistributorStock { DistributorId = distributor.Id, ProductId = foc.Id,  StockType = StockType.FreeIssue, QuantityOnHand = 1_000_000m, LastUpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            db.UserGeoAssignments.Add(new UserGeoAssignment
            {
                UserId = rep.Id, DivisionId = division.Id, TerritoryId = territory.Id,
                AreaId = area.Id, RegionId = region.Id, IsActive = true,
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow)
            });
            await db.SaveChangesAsync();

            _outletId            = outlet.Id;
            _distributorId       = distributor.Id;
            _saleProductId       = sale.Id;
            _focCompanyProductId = foc.Id;
            _repToken            = AuthHelper.GenerateToken(rep.Id, "SalesRep");
            _distToken           = AuthHelper.GenerateToken(distUser.Id, "Distributor");
            _otherDistToken      = AuthHelper.GenerateToken(otherDistUser.Id, "Distributor");
            _seeded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string Today() => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    /// <summary>Creates a bill as the rep and returns its id plus its line ids by product.</summary>
    private async Task<(int BillingId, Dictionary<int, int> LineIdByProduct)> CreateBillAsync(object[] items, decimal billDiscountRate = 0m)
    {
        SetToken(_repToken);
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = JsonContent.Create(new
            {
                outletId = _outletId,
                billDiscountRate,
                billingDate = Today(),
                latitude = 6.9271,
                longitude = 79.8612,
                items
            })
        };
        req.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var resp = await _client.SendAsync(req);
        var raw  = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Created, raw);

        using var doc = JsonDocument.Parse(raw);
        var data = doc.RootElement.GetProperty("data");
        var billingId = data.GetProperty("id").GetInt32();
        var lineIds = data.GetProperty("items").EnumerateArray()
            .ToDictionary(i => i.GetProperty("productId").GetInt32(), i => i.GetProperty("id").GetInt32());
        return (billingId, lineIds);
    }

    private async Task<(HttpStatusCode Status, JsonElement Data, string Raw)> AdjustAsync(int billingId, object payload)
    {
        var resp = await _client.PatchAsync($"{BaseUrl}/{billingId}/adjust-items", JsonContent.Create(payload));
        var raw  = await resp.Content.ReadAsStringAsync();
        if (resp.StatusCode != HttpStatusCode.OK)
            return (resp.StatusCode, default, raw);

        using var doc = JsonDocument.Parse(raw);
        return (resp.StatusCode, doc.RootElement.GetProperty("data").Clone(), raw);
    }

    private async Task<decimal> GetStockAsync(int productId, StockType pool)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.DistributorStocks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.DistributorId == _distributorId
                                   && s.ProductId == productId
                                   && s.StockType == pool);
        return row?.QuantityOnHand ?? 0m;
    }

    // ─────────────────────────────────────────────────
    // The worked example: 10 → 7 on a discounted Sale line
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task AdjustItems_ReducingSaleLine_CarvesDistributorReturnAndReducesTotalOnlyOnce()
    {
        await EnsureSeededAsync();

        // 10 × 100.00 @ 10% → discount 100.00, line total 900.00
        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 10m, unitPrice = 100.00m, discountRate = 10m, billingItemType = 0 }
        ]);
        var saleLineId = lineIds[_saleProductId];
        var stockBefore = await GetStockAsync(_saleProductId, StockType.Normal);

        SetToken(_distToken);
        var (status, data, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = saleLineId, quantity = 7m } },
            note = "Outlet took 7 only"
        });

        status.Should().Be(HttpStatusCode.OK, raw);

        // Sale line: 7 × 100 @ 10% → discount 70, total 630
        var saleLine = data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetInt32() == saleLineId);
        saleLine.GetProperty("quantity").GetDecimal().Should().Be(7m);
        saleLine.GetProperty("discountAmount").GetDecimal().Should().Be(70m);
        saleLine.GetProperty("totalPrice").GetDecimal().Should().Be(630m);
        saleLine.GetProperty("originalQuantity").GetDecimal().Should().Be(10m);
        saleLine.GetProperty("source").GetString().Should().Be("SalesRep");

        // Return line: 3 × 100 @ the same 10% → discount 30, value 270
        var returnLine = data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("source").GetString() == "DistributorReturn");
        returnLine.GetProperty("productId").GetInt32().Should().Be(_saleProductId);
        returnLine.GetProperty("quantity").GetDecimal().Should().Be(3m);
        returnLine.GetProperty("unitPrice").GetDecimal().Should().Be(100m);
        returnLine.GetProperty("discountRate").GetDecimal().Should().Be(10m);
        returnLine.GetProperty("discountAmount").GetDecimal().Should().Be(30m);
        returnLine.GetProperty("totalPrice").GetDecimal().Should().Be(270m);
        returnLine.GetProperty("billingItemType").GetString().Should().Be("Return");
        returnLine.GetProperty("returnType").GetString().Should().Be("DistributorReturn");
        returnLine.GetProperty("sourceBillingItemId").GetInt32().Should().Be(saleLineId);

        // The reduction is applied ONCE: the sale line already dropped to 630, so the return line
        // must not also be subtracted. 360 here would mean the return was double-counted.
        data.GetProperty("subTotalAmount").GetDecimal().Should().Be(630m);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(630m, "the return line is informational, never deducted");
        data.GetProperty("returnValue").GetDecimal().Should().Be(0m, "DistributorReturn is not a market resell");
        data.GetProperty("distributorReturnValue").GetDecimal().Should().Be(270m);
        data.GetProperty("itemWiseTotalDiscount").GetDecimal().Should().Be(70m);

        // Stock: exactly the 3 returned units come back to the Normal pool.
        (await GetStockAsync(_saleProductId, StockType.Normal)).Should().Be(stockBefore + 3m);

        // Change trail
        data.GetProperty("adjustmentCount").GetInt32().Should().Be(1);
        data.GetProperty("lastAdjustedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        var adjustment = data.GetProperty("adjustments").EnumerateArray().Single();
        adjustment.GetProperty("note").GetString().Should().Be("Outlet took 7 only");
        adjustment.GetProperty("adjustedByName").GetString().Should().Be("Adj Distributor");
        adjustment.GetProperty("oldTotalAmount").GetDecimal().Should().Be(900m);
        adjustment.GetProperty("newTotalAmount").GetDecimal().Should().Be(630m);

        var adjLine = adjustment.GetProperty("lines").EnumerateArray().Single();
        adjLine.GetProperty("billingItemId").GetInt32().Should().Be(saleLineId);
        adjLine.GetProperty("oldQuantity").GetDecimal().Should().Be(10m);
        adjLine.GetProperty("newQuantity").GetDecimal().Should().Be(7m);
        adjLine.GetProperty("oldTotalPrice").GetDecimal().Should().Be(900m);
        adjLine.GetProperty("newTotalPrice").GetDecimal().Should().Be(630m);
        adjLine.GetProperty("returnedQuantity").GetDecimal().Should().Be(3m);
        adjLine.GetProperty("returnValue").GetDecimal().Should().Be(270m);
        adjLine.GetProperty("productCode").GetString().Should().NotBeEmpty();
    }

    [Fact]
    public async Task AdjustItems_ThenApprove_ApprovesOnTheAdjustedTotal()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 10m, unitPrice = 50m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (adjustStatus, _, adjustRaw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 4m } },
            note = (string?)null
        });
        adjustStatus.Should().Be(HttpStatusCode.OK, adjustRaw);

        var approve = await _client.PatchAsync($"{BaseUrl}/{billingId}/approve", null);
        var approveRaw = await approve.Content.ReadAsStringAsync();
        approve.StatusCode.Should().Be(HttpStatusCode.OK, approveRaw);

        using var doc = JsonDocument.Parse(approveRaw);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("distributorStatus").GetString().Should().Be("Approved");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(200m, "4 × 50 after the adjustment");
        data.GetProperty("adjustmentCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task AdjustItems_ThenReject_ReturnsStockToPreBillLevelExactlyOnce()
    {
        await EnsureSeededAsync();

        var stockBefore = await GetStockAsync(_saleProductId, StockType.Normal);

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 10m, unitPrice = 25m, discountRate = 0m, billingItemType = 0 }
        ]);
        (await GetStockAsync(_saleProductId, StockType.Normal)).Should().Be(stockBefore - 10m);

        SetToken(_distToken);
        var (adjustStatus, _, adjustRaw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 6m } },
            note = (string?)null
        });
        adjustStatus.Should().Be(HttpStatusCode.OK, adjustRaw);
        (await GetStockAsync(_saleProductId, StockType.Normal)).Should().Be(stockBefore - 6m, "4 units came back");

        var reject = await _client.PatchAsync($"{BaseUrl}/{billingId}/reject",
            JsonContent.Create(new { reason = "changed mind" }));
        reject.StatusCode.Should().Be(HttpStatusCode.OK, await reject.Content.ReadAsStringAsync());

        // The reversal must credit only the 6 still on the sale line. Crediting the DistributorReturn
        // line as well would put the pool 4 units above where it started.
        (await GetStockAsync(_saleProductId, StockType.Normal)).Should().Be(stockBefore,
            "the 4 returned units were already credited at adjustment time and must not be credited again");
    }

    [Fact]
    public async Task AdjustItems_CompanyFundedFreeIssue_CreditsTheFreeIssuePool()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId,       quantity = 2m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 },
            new { productId = _focCompanyProductId, quantity = 6m, unitPrice = 5m,  discountRate = 0m, billingItemType = 2, freeIssueSource = 0 }
        ]);

        var focBefore    = await GetStockAsync(_focCompanyProductId, StockType.FreeIssue);
        var normalBefore = await GetStockAsync(_focCompanyProductId, StockType.Normal);

        SetToken(_distToken);
        var (status, data, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_focCompanyProductId], quantity = 2m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.OK, raw);

        // Company-funded FOC came out of the FreeIssue pool, so it must go back there.
        (await GetStockAsync(_focCompanyProductId, StockType.FreeIssue)).Should().Be(focBefore + 4m);
        (await GetStockAsync(_focCompanyProductId, StockType.Normal)).Should().Be(normalBefore);

        // A FOC line never carries a discount, so the return is valued at the full unit price.
        var returnLine = data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("source").GetString() == "DistributorReturn");
        returnLine.GetProperty("quantity").GetDecimal().Should().Be(4m);
        returnLine.GetProperty("discountAmount").GetDecimal().Should().Be(0m);
        returnLine.GetProperty("totalPrice").GetDecimal().Should().Be(20m);
        returnLine.GetProperty("freeIssueSource").GetString().Should().Be("Company");

        // FreeIssue value follows the reduced line; the sale line alone drives the payable total.
        data.GetProperty("freeIssueValueCompany").GetDecimal().Should().Be(10m, "2 × 5 remain on the FOC line");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(20m, "the 2 × 10 sale line is untouched");
        data.GetProperty("distributorReturnValue").GetDecimal().Should().Be(20m);
    }

    [Fact]
    public async Task AdjustItems_TwiceOnSameLine_CarvesASecondReturnAndKeepsTheOriginalQuantity()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 10m, unitPrice = 100m, discountRate = 0m, billingItemType = 0 }
        ]);
        var saleLineId = lineIds[_saleProductId];

        SetToken(_distToken);
        var (firstStatus, _, firstRaw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = saleLineId, quantity = 7m } },
            note = (string?)null
        });
        firstStatus.Should().Be(HttpStatusCode.OK, firstRaw);

        var (status, data, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = saleLineId, quantity = 5m } },
            note = (string?)null
        });
        status.Should().Be(HttpStatusCode.OK, raw);

        var saleLine = data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetInt32() == saleLineId);
        saleLine.GetProperty("quantity").GetDecimal().Should().Be(5m);
        saleLine.GetProperty("originalQuantity").GetDecimal().Should().Be(10m,
            "OriginalQuantity records what the rep billed, not the previous round");

        var returnLines = data.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("source").GetString() == "DistributorReturn")
            .Select(i => i.GetProperty("quantity").GetDecimal())
            .OrderBy(q => q)
            .ToList();
        returnLines.Should().Equal(2m, 3m);

        data.GetProperty("totalAmount").GetDecimal().Should().Be(500m);
        data.GetProperty("distributorReturnValue").GetDecimal().Should().Be(500m, "300 + 200 returned");
        data.GetProperty("adjustmentCount").GetInt32().Should().Be(2);
        data.GetProperty("adjustments").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task AdjustItems_ZeroQuantity_IsAllowedWhenAnotherLineRemains()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId,       quantity = 4m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 },
            new { productId = _focCompanyProductId, quantity = 2m, unitPrice = 5m,  discountRate = 0m, billingItemType = 2, freeIssueSource = 0 }
        ]);

        SetToken(_distToken);
        var (status, data, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_focCompanyProductId], quantity = 0m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.OK, raw);

        var focLine = data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetInt32() == lineIds[_focCompanyProductId]);
        focLine.GetProperty("quantity").GetDecimal().Should().Be(0m, "a zeroed line stays on the bill, it is not deleted");
        focLine.GetProperty("totalPrice").GetDecimal().Should().Be(0m);

        data.GetProperty("items").GetArrayLength().Should().Be(3, "2 original lines + 1 return line — nothing was removed");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(40m);
    }

    // ─────────────────────────────────────────────────
    // Guards
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task AdjustItems_IncreasingQuantity_IsRejected()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 6m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_QTY_INCREASE_NOT_ALLOWED");
    }

    [Fact]
    public async Task AdjustItems_NoActualChange_IsRejected()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 5m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_NO_CHANGES");
    }

    [Fact]
    public async Task AdjustItems_ZeroingEveryLine_IsRejected()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 3m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 0m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_ALL_LINES_ZERO");
    }

    [Fact]
    public async Task AdjustItems_ItemFromAnotherBill_IsRejected()
    {
        await EnsureSeededAsync();

        var (firstBillId, _) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 3m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);
        var (_, otherLineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 3m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(firstBillId, new
        {
            items = new[] { new { billingItemId = otherLineIds[_saleProductId], quantity = 1m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_ITEM_NOT_ON_BILL");
    }

    [Fact]
    public async Task AdjustItems_OnAReturnLine_IsRejected()
    {
        await EnsureSeededAsync();

        // A MarketResell return line credited stock at creation; the distributor is reviewing what the
        // outlet took, not re-deciding what came back, so those lines are read-only.
        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 },
            new { productId = _focCompanyProductId, quantity = 1m, unitPrice = 4m, discountRate = 0m, billingItemType = 1, returnType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_focCompanyProductId], quantity = 0m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_LINE_NOT_ADJUSTABLE");
    }

    [Fact]
    public async Task AdjustItems_ByADifferentDistributor_IsForbidden()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_otherDistToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 1m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.Forbidden, raw);
    }

    [Fact]
    public async Task AdjustItems_AfterApproval_IsRejected()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var approve = await _client.PatchAsync($"{BaseUrl}/{billingId}/approve", null);
        approve.StatusCode.Should().Be(HttpStatusCode.OK, await approve.Content.ReadAsStringAsync());

        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 1m } },
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("BILLING_ALREADY_ACTIONED");
    }

    [Fact]
    public async Task AdjustItems_AsSalesRep_IsForbidden()
    {
        await EnsureSeededAsync();

        var (billingId, lineIds) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_repToken);   // the rep cannot adjust their own bill
        var resp = await _client.PatchAsync($"{BaseUrl}/{billingId}/adjust-items",
            JsonContent.Create(new
            {
                items = new[] { new { billingItemId = lineIds[_saleProductId], quantity = 1m } },
                note = (string?)null
            }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdjustItems_EmptyItemList_IsRejectedByTheValidator()
    {
        await EnsureSeededAsync();

        var (billingId, _) = await CreateBillAsync(
        [
            new { productId = _saleProductId, quantity = 5m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
        ]);

        SetToken(_distToken);
        var (status, _, raw) = await AdjustAsync(billingId, new
        {
            items = Array.Empty<object>(),
            note = (string?)null
        });

        status.Should().Be(HttpStatusCode.BadRequest, raw);
    }
}
