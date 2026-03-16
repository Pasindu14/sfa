using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Products;

[Collection(SfaApiCollection.Name)]
public class ProductsApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ProductsApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static object CreateProductPayload(
        string code = "TEST-001",
        string itemDescription = "Test Product Description",
        string? printDescription = null,
        int piecesPerPack = 12,
        string? imageUrl = null,
        string? remarks = null) => new
        {
            code,
            itemDescription,
            printDescription,
            piecesPerPack,
            imageUrl,
            remarks
        };

    // ─────────────────────────────────────────────────
    // Authentication (401)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductById_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/products/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/v1/products", CreateProductPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/v1/products/1", CreateProductPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.DeleteAsync("/api/v1/products/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization (403) — Admin only
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllProducts_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllProducts_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PostAsJsonAsync("/api/v1/products", CreateProductPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.PostAsJsonAsync("/api/v1/products", CreateProductPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProduct_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.PutAsJsonAsync("/api/v1/products/1", CreateProductPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.DeleteAsync("/api/v1/products/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var response = await _client.DeleteAsync("/api/v1/products/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/products — Admin happy path
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllProducts_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAllProducts_WithPaginationParams_Returns200()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllProducts_WithSearchParam_ReturnsMatchingProducts()
    {
        SetToken(AuthHelper.AdminToken);
        await _client.PostAsJsonAsync("/api/v1/products", CreateProductPayload(
            code: "SRCH-ALPHA-001",
            itemDescription: "Searchable Alpha Widget Product"));

        var response = await _client.GetAsync("/api/v1/products?search=Searchable+Alpha+Widget");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        if (data.TryGetProperty("products", out var products) && products.ValueKind == JsonValueKind.Array)
        {
            foreach (var prod in products.EnumerateArray())
                prod.GetProperty("itemDescription").GetString()!.ToLower().Should().Contain("searchable alpha widget");
        }
    }

    // ─────────────────────────────────────────────────
    // POST /api/v1/products — Create + GET by ID
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_AsAdmin_Returns201AndCanGetById()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(
            code: "GAMMA-001",
            itemDescription: "Gamma Product Full Description",
            printDescription: "GAMMA PRODUCT",
            piecesPerPack: 24,
            imageUrl: "https://cdn.example.com/gamma.jpg",
            remarks: "Premium product");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        createBody.GetProperty("success").GetBoolean().Should().BeTrue();

        var productId = createBody.GetProperty("data").GetProperty("id").GetInt32();
        productId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("code").GetString().Should().Be("GAMMA-001");
        getBody.GetProperty("data").GetProperty("itemDescription").GetString().Should().Be("Gamma Product Full Description");
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_SetsIsActiveByDefault()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(
            code: "DELTA-001",
            itemDescription: "Delta Product Description");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", payload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        createBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ResponseIncludesAllFields()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(
            code: "FULL-001",
            itemDescription: "Full Field Product Description",
            printDescription: "FULL FIELD PRODUCT",
            piecesPerPack: 48,
            imageUrl: "https://cdn.example.com/full.jpg",
            remarks: "All fields populated");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", payload);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");

        data.GetProperty("code").GetString().Should().Be("FULL-001");
        data.GetProperty("itemDescription").GetString().Should().Be("Full Field Product Description");
        data.GetProperty("printDescription").GetString().Should().Be("FULL FIELD PRODUCT");
        data.GetProperty("piecesPerPack").GetInt32().Should().Be(48);
        data.GetProperty("imageUrl").GetString().Should().Be("https://cdn.example.com/full.jpg");
        data.GetProperty("remarks").GetString().Should().Be("All fields populated");
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Validation failures (400)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_InvalidData_Returns400WithFieldErrors()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new
        {
            code = "",              // required
            itemDescription = "",   // required
            piecesPerPack = -1      // must be >= 0
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        error.GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task CreateProduct_EmptyCode_Returns400WithCodeFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(code: "");

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("Code", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_EmptyItemDescription_Returns400WithFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(itemDescription: "");

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("ItemDescription", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_NegativePiecesPerPack_Returns400WithFieldError()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = new
        {
            code = "VALID-001",
            itemDescription = "Valid Description",
            piecesPerPack = -5
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var fields = body.GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("PiecesPerPack", out _).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // POST — Duplicate conflicts (409)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_DuplicateCode_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        var first = CreateProductPayload(
            code: "DUP-CODE-001",
            itemDescription: "First Product with Dup Code");

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/products", first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = CreateProductPayload(
            code: "DUP-CODE-001",       // same code
            itemDescription: "Second Product with Same Code");

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/products", second);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("CODE_DUPLICATE");
    }

    // ─────────────────────────────────────────────────
    // GET /api/v1/products/{id} — Not Found (404)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProductById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // PUT /api/v1/products/{id} — Update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_AsAdmin_Returns200WithUpdatedData()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateProductPayload(
            code: "BFR-UPD-001",
            itemDescription: "Before Update Product");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var updatePayload = CreateProductPayload(
            code: "AFT-UPD-001",
            itemDescription: "After Update Product",
            printDescription: "UPDATED",
            piecesPerPack: 48);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/products/{id}", updatePayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        updateBody.GetProperty("success").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("data").GetProperty("code").GetString().Should().Be("AFT-UPD-001");
        updateBody.GetProperty("data").GetProperty("itemDescription").GetString().Should().Be("After Update Product");
        updateBody.GetProperty("data").GetProperty("piecesPerPack").GetInt32().Should().Be(48);
    }

    [Fact]
    public async Task UpdateProduct_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(code: "GHOST-001", itemDescription: "Ghost Product");

        var response = await _client.PutAsJsonAsync("/api/v1/products/99999", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_InvalidData_Returns400()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateProductPayload(
            code: "VLD-UPD-001",
            itemDescription: "Valid Product Before Update");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var invalidPayload = new
        {
            code = "",                     // required
            itemDescription = "Valid Desc",
            piecesPerPack = 0
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/products/{id}", invalidPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task UpdateProduct_DuplicateCodeOfOtherProduct_Returns409()
    {
        SetToken(AuthHelper.AdminToken);

        var first = CreateProductPayload(code: "CONFLICT-A-001", itemDescription: "Conflict Product A");
        var second = CreateProductPayload(code: "CONFLICT-B-001", itemDescription: "Conflict Product B");

        await _client.PostAsJsonAsync("/api/v1/products", first);
        var secondResp = await _client.PostAsJsonAsync("/api/v1/products", second);
        var secondId = (await secondResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        // Try to update second product to use first product's code
        var updatePayload = CreateProductPayload(
            code: "CONFLICT-A-001",     // taken by first product
            itemDescription: "Conflict Product B Updated");

        var response = await _client.PutAsJsonAsync($"/api/v1/products/{secondId}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─────────────────────────────────────────────────
    // DELETE /api/v1/products/{id} — Soft delete
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_AsAdmin_Returns204()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateProductPayload(
            code: "DEL-001",
            itemDescription: "Product To Be Deleted");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/products/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_SetsIsActiveFalse()
    {
        SetToken(AuthHelper.AdminToken);

        var createPayload = CreateProductPayload(
            code: "SOFTDEL-001",
            itemDescription: "Product For Soft Delete Test");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createPayload);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        await _client.DeleteAsync($"/api/v1/products/{id}");

        // GET should still return the product (soft delete — record stays)
        var getResponse = await _client.GetAsync($"/api/v1/products/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProduct_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.DeleteAsync("/api/v1/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // Response Envelope Structure
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/products");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.TryGetProperty("success", out _).Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ErrorResponse_ContainsExpectedErrorFields()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/products/99999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        body.GetProperty("success").GetBoolean().Should().BeFalse();

        var error = body.GetProperty("error");
        error.TryGetProperty("code", out _).Should().BeTrue();
        error.TryGetProperty("message", out _).Should().BeTrue();
        error.TryGetProperty("traceId", out _).Should().BeTrue();
        error.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_Returns201WithLocationHeader()
    {
        SetToken(AuthHelper.AdminToken);

        var payload = CreateProductPayload(
            code: "LOC-HDR-001",
            itemDescription: "Location Header Test Product");

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/products/");
    }
}
