# Example: Category Feature Tests

A complete unit + integration test example for the simple Category feature (name, description, admin-only CRUD).

---

## Unit Tests — CategoryServiceTests.cs

```csharp
// Path: sfa_api/sfa_api.UnitTests/Features/Categories/Services/CategoryServiceTests.cs

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Categories.Entities;
using sfa_api.Features.Categories.Repositories;
using sfa_api.Features.Categories.Requests;
using sfa_api.Features.Categories.Services;

namespace sfa_api.UnitTests.Features.Categories.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repoMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _repoMock = new Mock<ICategoryRepository>();
        _sut = new CategoryService(_repoMock.Object, NullLogger<CategoryService>.Instance);
    }

    private static Category CreateFakeCategory(int id = 1) => new()
    {
        Id = id,
        Name = "Electronics",
        Description = "Electronic goods",
        IsActive = true,
        IsDeleted = false,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static CreateCategoryRequest CreateValidRequest() => new()
    {
        Name = "Electronics",
        Description = "Electronic goods",
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsDto()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(CreateFakeCategory());

        var result = await _sut.GetByIdAsync(1);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Category?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        SetupNoDuplicates();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Category>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(CreateValidRequest(), callerId: 1);

        result.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        SetupNoDuplicates();
        Category? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Category>(), default))
                 .Callback<Category, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.CreateAsync(CreateValidRequest(), callerId: 5);

        captured!.CreatedBy.Should().Be(5);
        captured.UpdatedBy.Should().Be(5);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsSaveChanges()
    {
        SetupNoDuplicates();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Category>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.CreateAsync(CreateValidRequest(), callerId: null);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync("Electronics", default)).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(CreateValidRequest(), callerId: null);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_CallsRepoAndSaveChanges()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(CreateFakeCategory());
        _repoMock.Setup(r => r.DeleteAsync(1, default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Category?)null);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupNoDuplicates()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }
}
```

---

## Integration Tests — CategoriesApiTests.cs

```csharp
// Path: sfa_api/sfa_api.IntegrationTests/Features/Categories/CategoriesApiTests.cs

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Categories;

[Collection(SfaApiCollection.Name)]
public class CategoriesApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public CategoriesApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static object CreatePayload(string name = "Test Category", string? description = null)
        => new { name, description };

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithEnvelope()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Create_InvalidData_Returns400WithFieldErrors()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name = "" };

        var response = await _client.PostAsJsonAsync("/api/v1/categories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Create_ValidData_Returns201()
    {
        SetToken(AuthHelper.AdminToken);
        var payload = CreatePayload($"Category_{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/v1/categories", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateThenGet_ReturnsCorrectData()
    {
        SetToken(AuthHelper.AdminToken);
        var name = $"Category_{Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", CreatePayload(name));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var getResponse = await _client.GetAsync($"/api/v1/categories/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        getBody.GetProperty("data").GetProperty("name").GetString().Should().Be(name);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync("/api/v1/categories/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task Delete_ExistingCategory_Returns204()
    {
        SetToken(AuthHelper.AdminToken);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories",
            CreatePayload($"ToDelete_{Guid.NewGuid():N}"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var id = createBody.GetProperty("data").GetProperty("id").GetInt32();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/categories/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/categories/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```
