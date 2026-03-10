using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Features.Territories.Requests;
using sfa_api.Features.Territories.Services;

namespace sfa_api.UnitTests.Features.Territories.Services;

public class TerritoryServiceTests
{
    private readonly Mock<ITerritoryRepository> _repoMock;
    private readonly TerritoryService _sut;

    public TerritoryServiceTests()
    {
        _repoMock = new Mock<ITerritoryRepository>();
        _sut = new TerritoryService(_repoMock.Object, NullLogger<TerritoryService>.Instance);
    }

    private static Area CreateFakeArea(int id = 1, string name = "Test Area") => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Territory CreateFakeTerritory(int id = 1, int areaId = 1, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Territory",
        AreaId = areaId,
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1,
        Area = CreateFakeArea(areaId)
    };

    private static CreateTerritoryRequest CreateValidCreateRequest() => new()
    {
        Name = "North Territory",
        AreaId = 1
    };

    private static UpdateTerritoryRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Territory",
        AreaId = 1
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingTerritory_ReturnsMappedDto()
    {
        var territory = CreateFakeTerritory();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(territory.Id);
        result.Name.Should().Be(territory.Name);
        result.AreaId.Should().Be(territory.AreaId);
        result.AreaName.Should().Be(territory.Area!.Name);
        result.IsActive.Should().Be(territory.IsActive);
        result.CreatedAt.Should().Be(territory.CreatedAt);
        result.UpdatedAt.Should().Be(territory.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTerritory_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedTerritoryListDto()
    {
        var territories = new[] { CreateFakeTerritory(1), CreateFakeTerritory(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((territories.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Territories.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Territory>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page - 1) * pageSize = (2 - 1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyTerritoryList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Territory>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Territories.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyMappedDtos()
    {
        var activeTerritories = new[]
        {
            new Territory { Id = 1, Name = "Alpha Territory", AreaId = 1, IsActive = true, Area = CreateFakeArea(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Territory { Id = 2, Name = "Beta Territory",  AreaId = 1, IsActive = true, Area = CreateFakeArea(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeTerritories.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(dto => dto.IsActive);
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepo_ReturnsEmptyEnumerable()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Territory>());

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AreaNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.AreaExistsAsync(request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.AreaExistsAsync(request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Territory? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Territory>(), It.IsAny<CancellationToken>()))
                 .Callback<Territory, CancellationToken>((t, _) => captured = t)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Territory? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Territory>(), It.IsAny<CancellationToken>()))
                 .Callback<Territory, CancellationToken>((t, _) => captured = t)
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
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Territory? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Territory>(), It.IsAny<CancellationToken>()))
                 .Callback<Territory, CancellationToken>((t, _) => captured = t)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentTerritory_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_AreaNotFound_ThrowsNotFoundException()
    {
        var territory = CreateFakeTerritory();
        var request = new UpdateTerritoryRequest { Name = "New Name", AreaId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        _repoMock.Setup(r => r.AreaExistsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var territory = CreateFakeTerritory();
        var request = new UpdateTerritoryRequest { Name = "Taken Territory", AreaId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        _repoMock.Setup(r => r.AreaExistsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Territory", 1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        var territory = CreateFakeTerritory();
        territory.Name = "My Territory";
        var request = new UpdateTerritoryRequest { Name = "My Territory", AreaId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        _repoMock.Setup(r => r.AreaExistsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("My Territory", 1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, territory);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_MutatesTerritoryFieldsCorrectly()
    {
        var territory = CreateFakeTerritory();
        var request = new UpdateTerritoryRequest { Name = "Renamed Territory", AreaId = 2 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        _repoMock.Setup(r => r.AreaExistsAsync(2, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("Renamed Territory", 2, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        var updatedTerritory = CreateFakeTerritory(id: 1, areaId: 2);
        updatedTerritory.Name = "Renamed Territory";
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(updatedTerritory);

        var result = await _sut.UpdateAsync(1, request, callerId: 1);

        result.Name.Should().Be("Renamed Territory");
        result.AreaId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var territory = CreateFakeTerritory();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        SetupSuccessfulUpdate(territory, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        territory.UpdatedBy.Should().Be(7);
        territory.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var territory = CreateFakeTerritory();
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
        SetupSuccessfulUpdate(territory, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingTerritory_SetsIsActiveTrue()
    {
        var territory = CreateFakeTerritory(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);

        await _sut.ActivateAsync(1, callerId: 1);

        territory.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(territory, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentTerritory_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var territory = CreateFakeTerritory(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);

        await _sut.ActivateAsync(1, callerId: 8);

        territory.UpdatedBy.Should().Be(8);
        territory.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingTerritory_SetsIsActiveFalse()
    {
        var territory = CreateFakeTerritory(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);

        await _sut.DeactivateAsync(1, callerId: 1);

        territory.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(territory, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentTerritory_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var territory = CreateFakeTerritory(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);

        await _sut.DeactivateAsync(1, callerId: 6);

        territory.UpdatedBy.Should().Be(6);
        territory.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(CreateTerritoryRequest request)
    {
        _repoMock.Setup(r => r.AreaExistsAsync(request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Territory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeTerritory());
    }

    private void SetupSuccessfulUpdate(Territory existingTerritory, UpdateTerritoryRequest request, int territoryId)
    {
        _repoMock.Setup(r => r.AreaExistsAsync(request.AreaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.AreaId, territoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(existingTerritory, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(territoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingTerritory);
    }

    private void SetupGetByIdAfterUpdate(int territoryId, Territory territory)
    {
        _repoMock.Setup(r => r.UpdateAsync(territory, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(territoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(territory);
    }
}
