using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.GRNs;

[Collection(SfaApiCollection.Name)]
public class GrnsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private const string BaseUrl = "/api/v1/grns";

    public GrnsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Seeding helpers ────────────────────────────────────────────────────

    private static int _aliasCounter = 8000;
    private static int _productCodeCounter = 8500;
    private static int _geoCounter = 8000;

    private async Task<int> SeedRegionAsync()
    {
        SetToken(AuthHelper.AdminToken);
        var name = $"GRN Region {Interlocked.Increment(ref _geoCounter)}";
        var response = await _client.PostAsJsonAsync("/api/v1/regions", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"region seed failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> SeedAreaAsync(int regionId)
    {
        SetToken(AuthHelper.AdminToken);
        var name = $"GRN Area {Interlocked.Increment(ref _geoCounter)}";
        var response = await _client.PostAsJsonAsync("/api/v1/areas", new { name, regionId });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"area seed failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> SeedTerritoryAsync(int areaId)
    {
        SetToken(AuthHelper.AdminToken);
        var name = $"GRN Territory {Interlocked.Increment(ref _geoCounter)}";
        var response = await _client.PostAsJsonAsync("/api/v1/territories", new { name, areaId });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"territory seed failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> SeedDistributorAsync(string name = "GRN Test Distributor")
    {
        // Distributor requires TerritoryId — seed the full geographic hierarchy.
        var regionId    = await SeedRegionAsync();
        var areaId      = await SeedAreaAsync(regionId);
        var territoryId = await SeedTerritoryAsync(areaId);

        SetToken(AuthHelper.AdminToken);
        var alias = Interlocked.Increment(ref _aliasCounter);
        var payload = new
        {
            name,
            address      = "10 GRN Test Road",
            phone        = $"077{alias:D7}",
            email        = $"grndist{alias}@test.com",
            alias,
            territoryId,
            tradeDiscount = 0.0m,
            commission    = 0.0m,
            category      = "A"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/distributors", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"distributor seed failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<(int productId, string erpCode)> SeedProductAsync()
    {
        SetToken(AuthHelper.AdminToken);
        var code = $"GRN-{Interlocked.Increment(ref _productCodeCounter)}";
        var payload = new
        {
            code,
            itemDescription  = "GRN integration test product",
            printDescription = (string?)null,
            piecesPerPack    = 10,
            imageUrl         = (string?)null,
            remarks          = (string?)null
        };
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"product seed failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return (body.GetProperty("data").GetProperty("id").GetInt32(), code);
    }

    /// <summary>
    /// Seeds a SalesInvoice via the import endpoint using real distributor alias and product ERP code.
    /// Returns the invoice ID of the imported (and persisted) invoice.
    /// Uses userId=1 (seeded admin) because SalesInvoiceImportBatch.ImportedBy has a FK to Users.
    /// </summary>
    private async Task<int> SeedSalesInvoiceAsync(int distributorAlias, string erpCode)
    {
        // SalesInvoiceImportBatch.ImportedBy has a FK to Users — must use seeded admin (ID=1).
        var seededAdminToken = AuthHelper.GenerateToken(userId: 1, role: "Admin");
        SetToken(seededAdminToken);

        var vchBillNo = $"INT-GRN-{Guid.NewGuid():N}";
        var payload = new
        {
            fileName = "grn_test_import.xlsx",
            invoices = new[]
            {
                new
                {
                    vchBillNo,
                    busyOrderRequestNo = (string?)null,
                    sfaPoNumber        = (string?)null,
                    distributorAlias,
                    invoiceDate        = "2026-01-15",
                    invoiceType        = "Regular",
                    totalAmount        = 500.0m,
                    items = new[]
                    {
                        new
                        {
                            itemErpCode     = erpCode,
                            itemDescription = "GRN test item",
                            quantity        = 5m,
                            unit            = "CTN",
                            unitPrice       = 100m,
                            totalPrice      = 500m,
                            isFreeIssue     = false,
                            lineNumber      = 1
                        }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/sales-invoices/import", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"sales invoice seed (import) failed: {await response.Content.ReadAsStringAsync()}");

        // Retrieve the list and find the invoice we just seeded by VchBillNo prefix.
        // The GetList endpoint returns: { success: true, data: [...] } where data is a plain array.
        var getResponse = await _client.GetAsync($"/api/v1/sales-invoices?search={vchBillNo}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");
        data.ValueKind.Should().Be(JsonValueKind.Array, "list endpoint returns data as a JSON array");
        data.GetArrayLength().Should().BeGreaterThan(0,
            "the seeded invoice must appear in the list");
        return data[0].GetProperty("id").GetInt32();
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/grns — Authentication
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAdminToken_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/grns/{id}
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/grns — Authentication & Authorization
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ManagerToken_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_SalesRepToken_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/grns — Validation
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AdminToken_SalesInvoiceIdZero_Returns400()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/grns — Business logic
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AdminToken_NonExistentSalesInvoice_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = 999999 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Create_AdminToken_ValidPendingSalesInvoice_Returns201WithGrnNumber()
    {
        // Arrange: seed distributor + product, then import an invoice
        var distributor = await SeedDistributorAsync("GRN Create Distributor");
        var (_, erpCode) = await SeedProductAsync();

        // The import endpoint accepts alias (int), but the distributor alias was set to the counter value.
        // We retrieve it from the distributor we just created.
        SetToken(AuthHelper.AdminToken);
        var distResponse = await _client.GetAsync($"/api/v1/distributors/{distributor}");
        var distBody = await distResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var alias = distBody.GetProperty("data").GetProperty("alias").GetInt32();

        var invoiceId = await SeedSalesInvoiceAsync(alias, erpCode);

        // Act
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync(BaseUrl, new { salesInvoiceId = invoiceId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = body.GetProperty("data");
        data.GetProperty("salesInvoiceId").GetInt32().Should().Be(invoiceId);
        data.GetProperty("grnNumber").GetString().Should().MatchRegex(@"^GRN-\d{4}-\d{5}$");
        data.GetProperty("status").GetString().Should().Be("Pending");
    }

    // ─────────────────────────────────────────────────
    // PATCH /api/v1/grns/{id}/confirm — Authentication & Authorization
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var payload = new { receivedAt = DateTime.UtcNow };

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/confirm", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Confirm_ManagerToken_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var payload = new { receivedAt = DateTime.UtcNow };

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/confirm", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm_SalesRepToken_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var payload = new { receivedAt = DateTime.UtcNow };

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/confirm", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // PATCH /api/v1/grns/{id}/confirm — Validation
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_AdminToken_EmptyReceivedAt_Returns400()
    {
        SetToken(AuthHelper.AdminToken);
        // Send an object without receivedAt so it defaults to DateTime.MinValue (empty)
        var payload = new { notes = "missing receivedAt" };

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/confirm", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // PATCH /api/v1/grns/{id}/confirm — Business logic
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_AdminToken_NonExistentGrn_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { receivedAt = DateTime.UtcNow };

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/999999/confirm", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }
}
