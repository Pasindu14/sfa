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
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserProximityExemptions.Entities;
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

    /// <summary>
    /// Creates an outlet on the seeded route carrying real coordinates. The shared
    /// seed outlet is deliberately at (0,0) — GeoMath's "no coordinate" sentinel,
    /// which skips the proximity gate — so any test that needs the gate to actually
    /// run has to bring its own outlet rather than mutate the shared one.
    /// </summary>
    private async Task<int> CreateOutletWithCoordinatesAsync(double lat, double lng)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = db.Outlets.First(o => o.Id == _outletId);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var outlet = new Outlet
        {
            Name = $"GeoOutlet-{suffix}", Address = "3 Geo St", Tel = $"03{suffix}",
            NicNo = $"NICG{suffix}", RouteId = seed.RouteId, DivisionId = seed.DivisionId,
            TerritoryId = seed.TerritoryId, AreaId = seed.AreaId, RegionId = seed.RegionId,
            Latitude = lat, Longitude = lng, IsActive = true
        };
        db.Outlets.Add(outlet);
        await db.SaveChangesAsync();
        return outlet.Id;
    }

    /// Inserts a live exemption straight into the table. The grant endpoint is
    /// covered by ProximityExemptionsApiTests; here we only need the effect.
    private async Task<int> GrantExemptionAsync(int userId, ProximityExemptionReason reason)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var grant = new UserProximityExemption
        {
            UserId = userId,
            ValidFrom = now.AddMinutes(-1),
            ValidTo = now.AddDays(1),
            Reason = reason,
            GrantedByUserId = userId,   // FK only needs to resolve under SQLite
            IsActive = true
        };
        db.UserProximityExemptions.Add(grant);
        await db.SaveChangesAsync();
        return grant.Id;
    }

    private async Task RevokeAllExemptionsAsync(int userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var row in db.UserProximityExemptions.Where(x => x.UserId == userId).ToList())
            row.IsActive = false;
        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync — proximity exemptions
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBilling_FarFromOutlet_WithoutExemption_IsRefused()
    {
        // Baseline for the test below: with the geofence armed, a rep ~300 km away
        // cannot bill. If this ever passes, the exemption feature is meaningless
        // because nothing was being enforced in the first place.
        await EnsureSeededAsync();
        await RevokeAllExemptionsAsync(_repId);
        SetToken(_repToken);
        var outletId = await CreateOutletWithCoordinatesAsync(6.9271, 79.8612);

        var payload = new
        {
            outletId,
            billDiscountRate = 0m,
            notes = "far away, no exemption",
            billingDate = Today(),
            latitude = 9.6615,      // Jaffna — far outside any sane radius
            longitude = 80.0255,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.UnprocessableEntity, raw);
        raw.Should().Contain("OUTLET_OUT_OF_RANGE");
    }

    [Fact]
    public async Task CreateBilling_FarFromOutlet_WithLiveExemption_IsAcceptedAndStamped()
    {
        // The whole point of the feature. It must also leave evidence: an accepted
        // out-of-range bill that is not flagged is invisible to any later review,
        // which is exactly the hole the exemption would otherwise open.
        await EnsureSeededAsync();
        SetToken(_repToken);
        var outletId = await CreateOutletWithCoordinatesAsync(6.9271, 79.8612);
        var exemptionId = await GrantExemptionAsync(_repId, ProximityExemptionReason.SharedCoordinateMarket);

        try
        {
            var payload = new
            {
                outletId,
                billDiscountRate = 0m,
                notes = "far away, exempt",
                billingDate = Today(),
                latitude = 9.6615,
                longitude = 80.0255,
                items = new object[]
                {
                    new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
                }
            };

            var (status, data, raw) = await PostBillingAsync(payload);

            status.Should().Be(HttpStatusCode.Created, raw);
            data.GetProperty("proximityOverridden").GetBoolean().Should().BeTrue(
                "an out-of-range bill let through by an exemption must be reportable");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = db.Billings.OrderByDescending(b => b.Id).First();
            saved.ProximityOverridden.Should().BeTrue();
            saved.ProximityExemptionId.Should().Be(exemptionId,
                "the report has to be able to name the grant and its reason");
            saved.DistanceFromOutletMeters.Should().BeGreaterThan(100_000,
                "the real distance is recorded even when the gate is relaxed");
        }
        finally
        {
            // The rep is shared across this collection — leaving the grant live
            // would silently disarm the geofence for every later test.
            await RevokeAllExemptionsAsync(_repId);
        }
    }

    [Fact]
    public async Task CreateBilling_NearOutlet_WithLiveExemption_IsNotFlaggedAsOverridden()
    {
        // An exempt rep standing at the shop is doing nothing unusual. Flagging it
        // would bury the genuine exceptions in noise.
        await EnsureSeededAsync();
        SetToken(_repToken);
        var outletId = await CreateOutletWithCoordinatesAsync(6.9271, 79.8612);
        await GrantExemptionAsync(_repId, ProximityExemptionReason.DeviceGpsFault);

        try
        {
            var payload = new
            {
                outletId,
                billDiscountRate = 0m,
                notes = "on the doorstep, exempt",
                billingDate = Today(),
                latitude = 6.9272,      // ~10 m away
                longitude = 79.8613,
                items = new object[]
                {
                    new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
                }
            };

            var (status, data, raw) = await PostBillingAsync(payload);

            status.Should().Be(HttpStatusCode.Created, raw);
            data.GetProperty("proximityOverridden").GetBoolean().Should().BeFalse();
        }
        finally
        {
            await RevokeAllExemptionsAsync(_repId);
        }
    }

    [Fact]
    public async Task CreateBilling_RecordsGpsAccuracyWhenSent()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = "accuracy stamp",
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            gpsAccuracyMeters = 12.5,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Billings.OrderByDescending(b => b.Id).First()
          .GpsAccuracyMeters.Should().Be(12.5);
    }

    [Fact]
    public async Task CreateBilling_WithoutGpsAccuracy_StillSucceeds()
    {
        // Older app builds do not send it. Refusing the bill over a missing
        // diagnostic field would strand every rep who has not updated.
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = "no accuracy field",
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
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
            latitude = 6.9271,
            longitude = 79.8612,
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
    public async Task CreateBilling_DamageAndExpireReturns_AreCreditedLikeGoodReturns()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        // Sale 10 × 50 = 500; returns: MarketResell 40 + Damage 30 + Expire 20 = 90 → total 410.
        // Must equal the mobile cart's "Net Total" (sales − all outlet returns).
        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 10m, unitPrice = 50m, discountRate = 0m, billingItemType = 0 },
                new { productId = _productBId, quantity = 2m,  unitPrice = 20m, discountRate = 0m, billingItemType = 1, returnType = 0 },
                new { productId = _productCId, quantity = 3m,  unitPrice = 10m, discountRate = 0m, billingItemType = 1, returnType = 1 },
                new { productId = _productDId, quantity = 1m,  unitPrice = 20m, discountRate = 0m, billingItemType = 1, returnType = 2,
                      expireDate = Today() }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("subTotalAmount").GetDecimal().Should().Be(500m);
        data.GetProperty("returnValue").GetDecimal().Should().Be(90m);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(410m);
    }

    [Fact]
    public async Task CreateBilling_TotalsRoundOnceFromUnroundedLines_MatchingTheMobileCart()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        // BIL-2026-00002, the client's reference order. Line discounts 21.216 + 19.575 are summed
        // unrounded (40.791) — rounding each line first (21.22 + 19.58) would give 3734.93.
        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 6m,  unitPrice = 110.50m, discountRate = 3.2m, billingItemType = 0 },
                new { productId = _productBId, quantity = 5m,  unitPrice = 87.00m,  discountRate = 4.5m, billingItemType = 0 },
                new { productId = _productAId, quantity = 8m,  unitPrice = 172.00m, discountRate = 0m,   billingItemType = 0 },  // C holds FreeIssue stock only
                new { productId = _productDId, quantity = 20m, unitPrice = 182.70m, discountRate = 0m,   billingItemType = 0 },
                new { productId = _productAId, quantity = 5m,  unitPrice = 125.11m, discountRate = 0m,   billingItemType = 1, returnType = 1 },
                new { productId = _productBId, quantity = 3m,  unitPrice = 312.28m, discountRate = 0m,   billingItemType = 1, returnType = 2,
                      expireDate = Today() },
                new { productId = _productCId, quantity = 4m,  unitPrice = 197.47m, discountRate = 0m,   billingItemType = 1, returnType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("subTotalAmount").GetDecimal().Should().Be(6087.21m);
        data.GetProperty("itemWiseTotalDiscount").GetDecimal().Should().Be(40.79m);
        data.GetProperty("returnValue").GetDecimal().Should().Be(2352.27m);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(3734.94m);
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
            latitude = 6.9271,
            longitude = 79.8612,
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
            latitude = 6.9271,
            longitude = 79.8612,
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
            latitude = 6.9271,
            longitude = 79.8612,
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
            BillingDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Latitude: 6.9271,
            Longitude: 79.8612);

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

    // ─────────────────────────────────────────────────
    // Mandatory rep location
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateBilling_WithoutCoordinates_IsRejected()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = "no location",
            billingDate = Today(),
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.BadRequest, raw);
        raw.Should().Contain("Latitude is required",
            "omitting coordinates would otherwise skip the proximity gate entirely");
    }

    [Fact]
    public async Task CreateBilling_WithZeroZeroCoordinates_IsRejected()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = "null island",
            billingDate = Today(),
            latitude = 0.0,
            longitude = 0.0,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.BadRequest, raw);
        raw.Should().Contain("(0, 0)",
            "(0,0) is GeoMath's no-coordinate sentinel and must not be usable to opt out of the geofence");
    }

    [Fact]
    public async Task CreateBilling_OutletWithoutCoordinates_SkipsProximityButStillRequiresRepLocation()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        // The seeded outlet has no coordinates (0,0). The rep position below is
        // ~10,000 km from it, yet the bill must succeed: proximity is skipped for
        // placeholder outlets, while the rep's own location is still recorded.
        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = "placeholder outlet",
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("latitude").GetDouble().Should().Be(6.9271);

        // No distance is computable against a 0,0 outlet, so it stays null.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = db.Billings.OrderByDescending(b => b.Id).First();
        saved.DistanceFromOutletMeters.Should().BeNull();
        saved.Latitude.Should().Be(6.9271);
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
            BillingDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Latitude: 6.9271,
            Longitude: 79.8612);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillingService>();

        var a = await service.CreateAsync(request, _repId, Guid.NewGuid().ToString());
        var b = await service.CreateAsync(request, _repId, Guid.NewGuid().ToString());

        b.Id.Should().NotBe(a.Id,
            "different clientBillIds are genuinely different submissions and must each create a bill");
    }

    [Fact]
    public async Task CreateBilling_WithRemarks_PersistsAndReturnsThem()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        // SFA-120: the rep records anything special about the outlet when they
        // close the bill. Multi-line text must survive the round trip intact.
        const string remarks = "Shop closed early.\nOwner asked to deliver the balance next visit.";

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = remarks,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("notes").GetString().Should().Be(remarks);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Billings.OrderByDescending(b => b.Id).First().Notes.Should().Be(remarks);
    }

    [Fact]
    public async Task CreateBilling_WithOverlongRemarks_Returns400()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            notes = new string('x', 1001),
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.BadRequest, raw);
        raw.Should().Contain("Notes must not exceed 1000 characters",
            "the 1000-char cap is enforced on the column, so without a validator rule an over-long remark would surface as a 500");
    }

    // ─────────────────────────────────────────────────
    // CreateAsync — pricing structures (per-line snapshot)
    // ─────────────────────────────────────────────────

    private async Task<int> CreateStructureAsync(string prefix, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = new PricingStructure { Name = $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}", IsActive = isActive };
        db.PricingStructures.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    /// The collection shares one database: reuse whichever default another test left, or create one.
    private async Task<int> EnsureDefaultStructureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = db.PricingStructures.FirstOrDefault(s => s.IsDefault && !s.IsDeleted);
        if (existing is not null) return existing.Id;
        var s = new PricingStructure { Name = $"BillDefault-{Guid.NewGuid():N}"[..30], IsActive = true, IsDefault = true };
        db.PricingStructures.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    [Fact]
    public async Task CreateBilling_MixedStructures_PersistsPerLineStructureBasisAndListPrice()
    {
        // A rep may switch structure mid-bill: each line keeps the structure and the prices it was
        // actually billed at, and the header records the structure selected at submit.
        await EnsureSeededAsync();
        SetToken(_repToken);
        var standard = await CreateStructureAsync("Std");
        var promo    = await CreateStructureAsync("Promo");

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            pricingStructureId = promo,
            items = new object[]
            {
                new { productId = _productAId, quantity = 2m,  unitPrice = 50m,  discountRate = 0m, billingItemType = 0,
                      pricingStructureId = standard, priceBasis = "Pack", listUnitPrice = 50m },
                new { productId = _productBId, quantity = 12m, unitPrice = 45m,  discountRate = 0m, billingItemType = 0,
                      pricingStructureId = promo,    priceBasis = "Case", listUnitPrice = 540m },
                new { productId = _productDId, quantity = 1m,  unitPrice = 10m,  discountRate = 0m, billingItemType = 1,
                      returnType = 0, priceBasis = "Manual" },   // no line structure → inherits the header's
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("pricingStructureId").GetInt32().Should().Be(promo);
        data.GetProperty("pricingStructureName").GetString().Should().StartWith("Promo-");

        var lines = data.GetProperty("items").EnumerateArray().OrderBy(i => i.GetProperty("lineNumber").GetInt32()).ToList();
        lines[0].GetProperty("pricingStructureId").GetInt32().Should().Be(standard);
        lines[0].GetProperty("priceBasis").GetString().Should().Be("Pack");
        lines[0].GetProperty("pricingStructureName").GetString().Should().StartWith("Std-");
        lines[1].GetProperty("pricingStructureId").GetInt32().Should().Be(promo);
        lines[1].GetProperty("priceBasis").GetString().Should().Be("Case");
        lines[1].GetProperty("listUnitPrice").GetDecimal().Should().Be(540m);
        lines[1].GetProperty("unitPrice").GetDecimal().Should().Be(45m, "the server keeps the per-pack price the phone sent");
        lines[2].GetProperty("pricingStructureId").GetInt32().Should().Be(promo);
        lines[2].GetProperty("priceBasis").GetString().Should().Be("Manual");
        lines[2].GetProperty("listUnitPrice").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CreateBilling_LegacyPayloadWithoutStructure_StampsDefaultStructure()
    {
        // Older app builds send no structure; they priced from the legacy product fields, which the
        // server now fills from the default structure — so the default is the accurate record.
        await EnsureSeededAsync();
        SetToken(_repToken);
        var defaultId = await EnsureDefaultStructureAsync();

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0 }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("pricingStructureId").GetInt32().Should().Be(defaultId);
        var line = data.GetProperty("items")[0];
        line.GetProperty("pricingStructureId").GetInt32().Should().Be(defaultId);
        line.GetProperty("priceBasis").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CreateBilling_InactiveStructure_IsStillAccepted()
    {
        // An offline bill can reach the server after its structure was deactivated; it is still a
        // true record of what was charged and must not be bounced.
        await EnsureSeededAsync();
        SetToken(_repToken);
        var retired = await CreateStructureAsync("Retired", isActive: false);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            pricingStructureId = retired,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0,
                      priceBasis = "Pack", listUnitPrice = 10m }
            }
        };

        var (status, data, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.Created, raw);
        data.GetProperty("items")[0].GetProperty("pricingStructureId").GetInt32().Should().Be(retired);
    }

    [Fact]
    public async Task CreateBilling_UnknownStructure_Returns404()
    {
        await EnsureSeededAsync();
        SetToken(_repToken);

        var payload = new
        {
            outletId = _outletId,
            billDiscountRate = 0m,
            billingDate = Today(),
            latitude = 6.9271,
            longitude = 79.8612,
            items = new object[]
            {
                new { productId = _productAId, quantity = 1m, unitPrice = 10m, discountRate = 0m, billingItemType = 0,
                      pricingStructureId = 987654 }
            }
        };

        var (status, _, raw) = await PostBillingAsync(payload);

        status.Should().Be(HttpStatusCode.NotFound, raw);
        raw.Should().Contain("PRICINGSTRUCTURE_NOT_FOUND");
    }
}
