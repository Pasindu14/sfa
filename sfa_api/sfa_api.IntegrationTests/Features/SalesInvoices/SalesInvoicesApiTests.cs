using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.SalesInvoices;

[Collection(SfaApiCollection.Name)]
public class SalesInvoicesApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private const string BaseUrl   = "/api/v1/sales-invoices";
    private const string ImportUrl = "/api/v1/sales-invoices/import";

    public SalesInvoicesApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Minimal valid import payload ───────────────────────────────────────

    private static object MinimalImportPayload(
        string fileName     = "test_import.xlsx",
        int distributorAlias = 9999,
        string vchBillNo    = "INT-TEST-001",
        string erpCode      = "ERP-INT-001") => new
    {
        fileName,
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
                totalAmount        = 1000.0m,
                items = new[]
                {
                    new
                    {
                        itemErpCode     = erpCode,
                        itemDescription = "Integration test product",
                        quantity        = 10m,
                        unit            = "CTN",
                        unitPrice       = 100m,
                        totalPrice      = 1000m,
                        isFreeIssue     = false,
                        lineNumber      = 1
                    }
                }
            }
        }
    };

    // ─────────────────────────────────────────────────
    // GET /api/v1/sales-invoices — Authentication
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

    [Fact]
    public async Task GetAll_WithStatusFilter_Returns200()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync($"{BaseUrl}?status=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/sales-invoices/{id}
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
    // POST /api/v1/sales-invoices/import — Authentication & Authorization
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Import_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(ImportUrl, MinimalImportPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Import_ManagerToken_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.PostAsJsonAsync(ImportUrl, MinimalImportPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Import_SalesRepToken_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync(ImportUrl, MinimalImportPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/sales-invoices/import — Validation
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Import_AdminToken_EmptyInvoicesList_Returns400()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { fileName = "test.xlsx", invoices = Array.Empty<object>() };

        var response = await _client.PostAsJsonAsync(ImportUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Import_AdminToken_EmptyFileName_Returns400()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = MinimalImportPayload(fileName: "");

        var response = await _client.PostAsJsonAsync(ImportUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/sales-invoices/import — Happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Import_AdminToken_ValidPayload_Returns200WithBatchResult()
    {
        // The in-memory SQLite DB has no distributors or products seeded, so the invoice
        // will be skipped (distributor alias not found). This is correct and expected:
        // the batch itself succeeds, but with skippedInvoices = 1 and status = "Failed".
        //
        // IMPORTANT: SalesInvoiceImportBatch.ImportedBy has a FK to Users.
        // The DataSeeder seeds one admin user with ID=1, so we must use userId=1 here.
        var seededAdminToken = AuthHelper.GenerateToken(userId: 1, role: "Admin", email: "admin@sfa.com", name: "System Admin");
        SetToken(seededAdminToken);

        var response = await _client.PostAsJsonAsync(ImportUrl, MinimalImportPayload());

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"import failed: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        data.GetProperty("totalInvoices").GetInt32().Should().Be(1);
        data.GetProperty("importedInvoices").GetInt32().Should().Be(0);
        data.GetProperty("skippedInvoices").GetInt32().Should().Be(1);
        data.GetProperty("status").GetString().Should().Be("Failed");
        data.GetProperty("batchNumber").GetString().Should().StartWith("IMP-");

        var errors = data.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
    }
}
