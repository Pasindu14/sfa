using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Services;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Products.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.PricingStructures.Services;

public class PricingStructureServiceTests
{
    private readonly Mock<IPricingStructureRepository> _repoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly PricingStructureService _sut;

    public PricingStructureServiceTests()
    {
        _repoMock        = new Mock<IPricingStructureRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _cacheMock       = new Mock<ICacheService>();
        _sut = new PricingStructureService(
            _repoMock.Object,
            _productRepoMock.Object,
            _cacheMock.Object,
            NullLogger<PricingStructureService>.Instance);
    }

    // ─────────────────────────────────────────────────
    // Factory helpers
    // ─────────────────────────────────────────────────

    private static PricingStructure CreateFakeStructure(int id = 1, bool isDefault = false) => new()
    {
        Id = id,
        Name = $"Structure {id}",
        Description = "Test description",
        IsDefault = isDefault,
        IsActive = true,
        Items = [],
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static PricingStructureItem CreateFakeItem(int id = 1, int structureId = 1, int productId = 10) => new()
    {
        Id = id,
        PricingStructureId = structureId,
        ProductId = productId,
        DealerPackPrice = 100m,
        DealerCasePrice = 950m,
        Product = new Product
        {
            Id = productId,
            Code = $"PROD-{productId}",
            ItemDescription = $"Product {productId}",
            IsActive = true
        },
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static CreatePricingStructureRequest CreateValidCreateRequest(bool isDefault = false) => new()
    {
        Name = "Standard Price List",
        Description = "Default description",
        IsDefault = isDefault
    };

    private static UpdatePricingStructureRequest CreateValidUpdateRequest(bool isDefault = false) => new()
    {
        Name = "Updated Price List",
        Description = "Updated description",
        IsDefault = isDefault
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingStructure_ReturnsDtoWithCorrectIdAndName()
    {
        var structure = CreateFakeStructure(id: 5);
        structure.Items = [CreateFakeItem(structureId: 5)];
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(structure);

        var result = await _sut.GetByIdAsync(5);

        result.Should().NotBeNull();
        result.Id.Should().Be(5);
        result.Name.Should().Be("Structure 5");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedDtoWithCorrectPageAndPageSize()
    {
        var structures = new[] { CreateFakeStructure(1), CreateFakeStructure(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((structures.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(page: 1, pageSize: 10);

        result.PricingStructures.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2PageSize5_CalculatesSkipOf5()
    {
        _repoMock.Setup(r => r.GetAllAsync(5, 5, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<PricingStructure>(), 0));

        await _sut.GetAllAsync(page: 2, pageSize: 5);

        _repoMock.Verify(r => r.GetAllAsync(5, 5, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsTotalCountZero()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<PricingStructure>(), 0));

        var result = await _sut.GetAllAsync(page: 1, pageSize: 10);

        result.PricingStructures.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "premium";
        _repoMock.Setup(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<PricingStructure>(), 0));

        await _sut.GetAllAsync(page: 1, pageSize: 10, search: search);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDtoWithCorrectName()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var result = await _sut.CreateAsync(request, callerId: 1);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.IsActive.Should().BeTrue();
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsCreatedByAndUpdatedByToCallerId()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);
        PricingStructure? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<PricingStructure>(), It.IsAny<CancellationToken>()))
                 .Callback<PricingStructure, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 42);

        captured.Should().NotBeNull();
        captured!.CreatedBy.Should().Be(42);
        captured.UpdatedBy.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsSaveChangesOnce()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());

        var act = () => _sut.CreateAsync(request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task CreateAsync_IsDefaultTrue_WhenCurrentDefaultExists_UnsetsOldDefault()
    {
        var request = CreateValidCreateRequest(isDefault: true);
        var oldDefault = CreateFakeStructure(id: 99, isDefault: true);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);
        _repoMock.Setup(r => r.GetCurrentDefaultAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(oldDefault);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<PricingStructure>(s => s.Id == 99 && !s.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_IsDefaultFalse_NeverCallsGetCurrentDefault()
    {
        var request = CreateValidCreateRequest(isDefault: false);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.GetCurrentDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsDtoWithUpdatedName()
    {
        var structure = CreateFakeStructure(id: 1);
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(structure);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var result = await _sut.UpdateAsync(1, request, callerId: 1);

        result.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameForDifferentId_ThrowsDuplicateResourceException()
    {
        var structure = CreateFakeStructure(id: 1);
        var conflicting = CreateFakeStructure(id: 2);
        conflicting.Name = "Updated Price List";
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(structure);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(conflicting);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task UpdateAsync_IsDefaultTrue_WhenCurrentDefaultIsOtherStructure_UnsetsOldDefault()
    {
        var structure = CreateFakeStructure(id: 1, isDefault: false);
        var oldDefault = CreateFakeStructure(id: 99, isDefault: true);
        var request = CreateValidUpdateRequest(isDefault: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(structure);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);
        _repoMock.Setup(r => r.GetCurrentDefaultAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(oldDefault);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<PricingStructure>(s => s.Id == 99 && !s.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedByToCallerId()
    {
        var structure = CreateFakeStructure(id: 1);
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(structure);
        _repoMock.Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        await _sut.UpdateAsync(1, request, callerId: 7);

        structure.UpdatedBy.Should().Be(7);
        structure.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingStructure_CallsRepoDeleteAndSaveOnce()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingStructure_CallsRepoActivateAndSaveOnce()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());

        await _sut.ActivateAsync(1);

        _repoMock.Verify(r => r.ActivateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.ActivateAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // GetItemsAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_ExistingStructure_ReturnsMappedItemDtosWithProductFields()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());
        var items = new[] { CreateFakeItem(id: 1, structureId: 1, productId: 10) };
        _repoMock.Setup(r => r.GetItemsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(items.AsEnumerable());

        var result = (await _sut.GetItemsAsync(1)).ToList();

        result.Should().HaveCount(1);
        result[0].ProductCode.Should().Be("PROD-10");
        result[0].ProductItemDescription.Should().Be("Product 10");
        result[0].DealerPackPrice.Should().Be(100m);
        result[0].DealerCasePrice.Should().Be(950m);
    }

    [Fact]
    public async Task GetItemsAsync_NotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.GetItemsAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // BulkReplaceItemsAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task BulkReplaceItemsAsync_ValidProductIds_CallsBulkReplaceAndSave()
    {
        var request = new BulkUpdateItemsRequest
        {
            Items =
            [
                new PricingStructureItemRequest { ProductId = 10, DealerPackPrice = 50m }
            ]
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());
        var activeProducts = new[]
        {
            new Product { Id = 10, Code = "PROD-10", ItemDescription = "Product 10", IsActive = true }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(0, int.MaxValue, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((activeProducts.AsEnumerable(), 1));
        _repoMock.Setup(r => r.GetItemsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([CreateFakeItem(productId: 10)]);

        await _sut.BulkReplaceItemsAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.BulkReplaceItemsAsync(
            1, It.IsAny<IEnumerable<PricingStructureItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkReplaceItemsAsync_StructureNotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PricingStructure?)null);

        var request = new BulkUpdateItemsRequest
        {
            Items = [new PricingStructureItemRequest { ProductId = 10, DealerPackPrice = 10m }]
        };

        var act = () => _sut.BulkReplaceItemsAsync(99, request, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task BulkReplaceItemsAsync_InvalidProductId_ThrowsValidationException()
    {
        var request = new BulkUpdateItemsRequest
        {
            Items =
            [
                new PricingStructureItemRequest { ProductId = 999, DealerPackPrice = 50m }
            ]
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());
        // Return empty active products list — productId 999 won't be found
        _productRepoMock.Setup(r => r.GetAllAsync(0, int.MaxValue, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        var act = () => _sut.BulkReplaceItemsAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("Items");
    }

    [Fact]
    public async Task BulkReplaceItemsAsync_ValidRequest_SetsCallerIdOnNewItems()
    {
        var request = new BulkUpdateItemsRequest
        {
            Items =
            [
                new PricingStructureItemRequest { ProductId = 10, DealerPackPrice = 75m, DealerCasePrice = 700m }
            ]
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeStructure());
        var activeProducts = new[]
        {
            new Product { Id = 10, Code = "PROD-10", ItemDescription = "Product 10", IsActive = true }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(0, int.MaxValue, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((activeProducts.AsEnumerable(), 1));

        IEnumerable<PricingStructureItem>? capturedItems = null;
        _repoMock.Setup(r => r.BulkReplaceItemsAsync(1, It.IsAny<IEnumerable<PricingStructureItem>>(), It.IsAny<CancellationToken>()))
                 .Callback<int, IEnumerable<PricingStructureItem>, CancellationToken>((_, items, _) => capturedItems = items)
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetItemsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([CreateFakeItem(productId: 10)]);

        await _sut.BulkReplaceItemsAsync(1, request, callerId: 55);

        capturedItems.Should().NotBeNull();
        capturedItems!.First().CreatedBy.Should().Be(55);
        capturedItems.First().UpdatedBy.Should().Be(55);
    }
}
