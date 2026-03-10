using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Divisions.Repositories;
using sfa_api.Features.Divisions.Requests;
using sfa_api.Features.Divisions.Services;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.UnitTests.Features.Divisions.Services;

public class DivisionServiceTests
{
    private readonly Mock<IDivisionRepository> _repoMock;
    private readonly DivisionService _sut;

    public DivisionServiceTests()
    {
        _repoMock = new Mock<IDivisionRepository>();
        _sut = new DivisionService(_repoMock.Object, NullLogger<DivisionService>.Instance);
    }

    private static Region CreateFakeRegion(int id = 10, string name = "Test Region") => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Area CreateFakeArea(int id = 20, int regionId = 10, string name = "Test Area") => new()
    {
        Id = id,
        Name = name,
        RegionId = regionId,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Region = CreateFakeRegion(regionId)
    };

    private static Territory CreateFakeTerritory(int id = 30, int areaId = 20, int regionId = 10, string name = "Test Territory") => new()
    {
        Id = id,
        Name = name,
        AreaId = areaId,
        RegionId = regionId,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Area = CreateFakeArea(areaId, regionId)
    };

    private static Division CreateFakeDivision(int id = 1, int territoryId = 30, int areaId = 20, int regionId = 10, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Division",
        TerritoryId = territoryId,
        AreaId = areaId,
        RegionId = regionId,
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1,
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId)
    };

    private static CreateDivisionRequest CreateValidCreateRequest() => new()
    {
        Name = "North Division",
        TerritoryId = 30
    };

    private static UpdateDivisionRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Division",
        TerritoryId = 30
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingDivision_ReturnsMappedDto()
    {
        var division = CreateFakeDivision();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(division.Id);
        result.Name.Should().Be(division.Name);
        result.TerritoryId.Should().Be(division.TerritoryId);
        result.TerritoryName.Should().Be(division.Territory!.Name);
        result.AreaId.Should().Be(division.AreaId);
        result.AreaName.Should().Be(division.Area!.Name);
        result.RegionId.Should().Be(division.RegionId);
        result.RegionName.Should().Be(division.Region!.Name);
        result.IsActive.Should().Be(division.IsActive);
        result.CreatedAt.Should().Be(division.CreatedAt);
        result.UpdatedAt.Should().Be(division.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentDivision_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedDivisionListDto()
    {
        var divisions = new[] { CreateFakeDivision(1), CreateFakeDivision(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((divisions.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Divisions.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Division>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page - 1) * pageSize = (2 - 1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyDivisionList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Division>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Divisions.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyMappedDtos()
    {
        var activeDivisions = new[]
        {
            new Division { Id = 1, Name = "Alpha Division", TerritoryId = 30, AreaId = 20, RegionId = 10, IsActive = true, Territory = CreateFakeTerritory(), Area = CreateFakeArea(), Region = CreateFakeRegion(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Division { Id = 2, Name = "Beta Division",  TerritoryId = 30, AreaId = 20, RegionId = 10, IsActive = true, Territory = CreateFakeTerritory(), Area = CreateFakeArea(), Region = CreateFakeRegion(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeDivisions.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(dto => dto.IsActive);
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepo_ReturnsEmptyEnumerable()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Division>());

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_TerritoryNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(request.TerritoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(request.TerritoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeTerritory(request.TerritoryId));
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.TerritoryId, It.IsAny<CancellationToken>()))
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
        Division? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Division>(), It.IsAny<CancellationToken>()))
                 .Callback<Division, CancellationToken>((d, _) => captured = d)
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
        Division? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Division>(), It.IsAny<CancellationToken>()))
                 .Callback<Division, CancellationToken>((d, _) => captured = d)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 5);

        captured!.CreatedBy.Should().Be(5);
        captured.UpdatedBy.Should().Be(5);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        captured.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_DenormalizesAncestorIds()
    {
        var request = CreateValidCreateRequest();
        var territory = CreateFakeTerritory(id: 30, areaId: 20, regionId: 10);
        SetupSuccessfulCreate(request, territory);
        Division? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Division>(), It.IsAny<CancellationToken>()))
                 .Callback<Division, CancellationToken>((d, _) => captured = d)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.TerritoryId.Should().Be(30);
        captured.AreaId.Should().Be(20);
        captured.RegionId.Should().Be(10);
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
        Division? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Division>(), It.IsAny<CancellationToken>()))
                 .Callback<Division, CancellationToken>((d, _) => captured = d)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentDivision_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_TerritoryNotFound_ThrowsNotFoundException()
    {
        var division = CreateFakeDivision();
        var request = new UpdateDivisionRequest { Name = "New Name", TerritoryId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Territory?)null);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("TERRITORY_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var division = CreateFakeDivision();
        var request = new UpdateDivisionRequest { Name = "Taken Division", TerritoryId = 30 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(30, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeTerritory(30));
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Division", 30, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        var division = CreateFakeDivision();
        division.Name = "My Division";
        var request = new UpdateDivisionRequest { Name = "My Division", TerritoryId = 30 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(30, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeTerritory(30));
        _repoMock.Setup(r => r.ExistsByNameAsync("My Division", 30, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, division);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var division = CreateFakeDivision();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        SetupSuccessfulUpdate(division, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        division.UpdatedBy.Should().Be(7);
        division.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var division = CreateFakeDivision();
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        SetupSuccessfulUpdate(division, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DenormalizesAncestorIdsFromNewTerritory()
    {
        var division = CreateFakeDivision();
        // Update to a different territory with different ancestors
        var newTerritory = CreateFakeTerritory(id: 40, areaId: 50, regionId: 60);
        var request = new UpdateDivisionRequest { Name = "Renamed Division", TerritoryId = 40 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(40, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(newTerritory);
        _repoMock.Setup(r => r.ExistsByNameAsync("Renamed Division", 40, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, division);

        await _sut.UpdateAsync(1, request, callerId: 1);

        division.TerritoryId.Should().Be(40);
        division.AreaId.Should().Be(50);
        division.RegionId.Should().Be(60);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingDivision_SetsIsActiveTrue()
    {
        var division = CreateFakeDivision(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);

        await _sut.ActivateAsync(1, callerId: 1);

        division.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(division, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentDivision_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var division = CreateFakeDivision(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);

        await _sut.ActivateAsync(1, callerId: 8);

        division.UpdatedBy.Should().Be(8);
        division.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingDivision_SetsIsActiveFalse()
    {
        var division = CreateFakeDivision(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);

        await _sut.DeactivateAsync(1, callerId: 1);

        division.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(division, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentDivision_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var division = CreateFakeDivision(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);

        await _sut.DeactivateAsync(1, callerId: 6);

        division.UpdatedBy.Should().Be(6);
        division.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(CreateDivisionRequest request, Territory? territory = null)
    {
        var fakeTerritory = territory ?? CreateFakeTerritory(request.TerritoryId);
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(request.TerritoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fakeTerritory);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.TerritoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Division>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeDivision());
    }

    private void SetupSuccessfulUpdate(Division existingDivision, UpdateDivisionRequest request, int divisionId)
    {
        _repoMock.Setup(r => r.GetTerritoryWithAncestorsAsync(request.TerritoryId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeTerritory(request.TerritoryId));
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.TerritoryId, divisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(existingDivision, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(divisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingDivision);
    }

    private void SetupGetByIdAfterUpdate(int divisionId, Division division)
    {
        _repoMock.Setup(r => r.UpdateAsync(division, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(divisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(division);
    }
}
