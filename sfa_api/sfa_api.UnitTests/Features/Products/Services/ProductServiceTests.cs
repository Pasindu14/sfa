using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.ProductCategories.Repositories;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.Products.Requests;
using sfa_api.Features.Products.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Products.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<IFleetRepository> _fleetRepoMock;
    private readonly Mock<IProductCategoryRepository> _categoryRepoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _repoMock          = new Mock<IProductRepository>();
        _fleetRepoMock     = new Mock<IFleetRepository>();
        _categoryRepoMock  = new Mock<IProductCategoryRepository>();
        _cacheMock         = new Mock<ICacheService>();
        _sut = new ProductService(_repoMock.Object, _fleetRepoMock.Object, _categoryRepoMock.Object, _cacheMock.Object, NullLogger<ProductService>.Instance);
    }

    private static Product CreateFakeProduct(int id = 1) => new()
    {
        Id = id,
        Code = $"PROD-{id:D3}",
        ItemDescription = "Test Product Description",
        PrintDescription = "TEST PRODUCT",
        PiecesPerPack = 12,
        ImageUrl = "https://example.com/image.jpg",
        Remarks = "Test remark",
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1
    };

    private static CreateProductRequest CreateValidRequest() => new()
    {
        Code = "NEW-001",
        ItemDescription = "New Product Item Description",
        PrintDescription = "NEW PRODUCT",
        PiecesPerPack = 6,
        ImageUrl = null,
        Remarks = null
    };

    private static UpdateProductRequest CreateValidUpdateRequest() => new()
    {
        Code = "UPD-001",
        ItemDescription = "Updated Product Description",
        PrintDescription = "UPDATED PRODUCT",
        PiecesPerPack = 24,
        ImageUrl = "https://example.com/updated.jpg",
        Remarks = "Updated remark"
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsDto()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(product.Id);
        result.Code.Should().Be(product.Code);
        result.ItemDescription.Should().Be(product.ItemDescription);
        result.PrintDescription.Should().Be(product.PrintDescription);
        result.PiecesPerPack.Should().Be(product.PiecesPerPack);
        result.ImageUrl.Should().Be(product.ImageUrl);
        result.Remarks.Should().Be(product.Remarks);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentProduct_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>()
                 .WithMessage("*99*");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedList()
    {
        var products = new[] { CreateFakeProduct(1), CreateFakeProduct(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((products.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Products.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page-1) * pageSize = (2-1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_ForwardsSearchToRepo()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, "widget", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        await _sut.GetAllAsync(1, 10, search: "widget");

        _repoMock.Verify(r => r.GetAllAsync(0, 10, "widget", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var request = CreateValidRequest();
        SetupNoDuplicateCode();

        var result = await _sut.CreateAsync(request, callerId: 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(request.Code);
        result.ItemDescription.Should().Be(request.ItemDescription);
        result.PrintDescription.Should().Be(request.PrintDescription);
        result.PiecesPerPack.Should().Be(request.PiecesPerPack);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidRequest();
        SetupNoDuplicateCode();
        Product? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                 .Callback<Product, CancellationToken>((p, _) => captured = p)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidRequest();
        SetupNoDuplicateCode();
        Product? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                 .Callback<Product, CancellationToken>((p, _) => captured = p)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 5);

        captured!.CreatedBy.Should().Be(5);
        captured.UpdatedBy.Should().Be(5);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        captured.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsSaveChanges()
    {
        var request = CreateValidRequest();
        SetupNoDuplicateCode();

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByCodeAsync(request.Code, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("CODE_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidRequest();
        SetupNoDuplicateCode();
        Product? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                 .Callback<Product, CancellationToken>((p, _) => captured = p)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_NeverCallsCreate()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByCodeAsync(request.Code, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateResourceException>(() => _sut.CreateAsync(request, callerId: 1));

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        SetupNoDuplicateCodeForUpdate(1);

        var request = CreateValidUpdateRequest();
        var result = await _sut.UpdateAsync(1, request, callerId: 2);

        result.Code.Should().Be(request.Code);
        result.ItemDescription.Should().Be(request.ItemDescription);
        result.PrintDescription.Should().Be(request.PrintDescription);
        result.PiecesPerPack.Should().Be(request.PiecesPerPack);
        result.ImageUrl.Should().Be(request.ImageUrl);
        result.Remarks.Should().Be(request.Remarks);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentProduct_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateCode_ThrowsDuplicateResourceException()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _repoMock.Setup(r => r.ExistsByCodeAsync("TAKEN-001", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = CreateValidUpdateRequest();
        request.Code = "TAKEN-001";
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("CODE_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        SetupNoDuplicateCodeForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        product.UpdatedBy.Should().Be(7);
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        SetupNoDuplicateCodeForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProduct_CallsRepoDeleteAndSave()
    {
        var product = CreateFakeProduct();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentProduct_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentProduct_NeverCallsRepoDelete()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));

        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupNoDuplicateCode()
    {
        _repoMock.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }

    private void SetupNoDuplicateCodeForUpdate(int excludeId)
    {
        _repoMock.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), excludeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }
}
