using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Areas.Repositories;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.Areas.Services;
using sfa_api.Features.Regions.Entities;

namespace sfa_api.UnitTests.Features.Areas.Services;

public class AreaServiceTests
{
    private readonly Mock<IAreaRepository> _repoMock;
    private readonly AreaService _sut;

    public AreaServiceTests()
    {
        _repoMock = new Mock<IAreaRepository>();
        _sut = new AreaService(_repoMock.Object, NullLogger<AreaService>.Instance);
    }

    private static Region CreateFakeRegion(int id = 1, string name = "Test Region") => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Area CreateFakeArea(int id = 1, int regionId = 1, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Area",
        RegionId = regionId,
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1,
        Region = CreateFakeRegion(regionId)
    };

    private static CreateAreaRequest CreateValidCreateRequest() => new()
    {
        Name = "North Area",
        RegionId = 1
    };

    private static UpdateAreaRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Area",
        RegionId = 1
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingArea_ReturnsMappedDto()
    {
        var area = CreateFakeArea();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(area.Id);
        result.Name.Should().Be(area.Name);
        result.RegionId.Should().Be(area.RegionId);
        result.RegionName.Should().Be(area.Region!.Name);
        result.IsActive.Should().Be(area.IsActive);
        result.CreatedAt.Should().Be(area.CreatedAt);
        result.UpdatedAt.Should().Be(area.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentArea_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Area?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedAreaListDto()
    {
        var areas = new[] { CreateFakeArea(1), CreateFakeArea(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((areas.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Areas.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Area>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page - 1) * pageSize = (2 - 1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyAreasList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Area>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Areas.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyMappedDtos()
    {
        var activeAreas = new[]
        {
            new Area { Id = 1, Name = "Alpha Area", RegionId = 1, IsActive = true, Region = CreateFakeRegion(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Area { Id = 2, Name = "Beta Area",  RegionId = 1, IsActive = true, Region = CreateFakeRegion(), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeAreas.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(dto => dto.IsActive);
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepo_ReturnsEmptyEnumerable()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Area>());

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_RegionNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.RegionExistsAsync(request.RegionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("REGION_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.RegionExistsAsync(request.RegionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.RegionId, It.IsAny<CancellationToken>()))
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
        Area? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                 .Callback<Area, CancellationToken>((a, _) => captured = a)
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
        Area? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                 .Callback<Area, CancellationToken>((a, _) => captured = a)
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
        Area? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                 .Callback<Area, CancellationToken>((a, _) => captured = a)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentArea_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Area?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_RegionNotFound_ThrowsNotFoundException()
    {
        var area = CreateFakeArea();
        var request = new UpdateAreaRequest { Name = "New Name", RegionId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        _repoMock.Setup(r => r.RegionExistsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("REGION_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var area = CreateFakeArea();
        var request = new UpdateAreaRequest { Name = "Taken Area", RegionId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        _repoMock.Setup(r => r.RegionExistsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Area", 1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        var area = CreateFakeArea();
        area.Name = "My Area";
        var request = new UpdateAreaRequest { Name = "My Area", RegionId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        _repoMock.Setup(r => r.RegionExistsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("My Area", 1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, area);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_MutatesAreaFieldsCorrectly()
    {
        var area = CreateFakeArea();
        var request = new UpdateAreaRequest { Name = "Renamed Area", RegionId = 2 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        _repoMock.Setup(r => r.RegionExistsAsync(2, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync("Renamed Area", 2, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        var updatedArea = CreateFakeArea(id: 1, regionId: 2);
        updatedArea.Name = "Renamed Area";
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(updatedArea);

        var result = await _sut.UpdateAsync(1, request, callerId: 1);

        result.Name.Should().Be("Renamed Area");
        result.RegionId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var area = CreateFakeArea();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        SetupSuccessfulUpdate(area, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        area.UpdatedBy.Should().Be(7);
        area.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var area = CreateFakeArea();
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
        SetupSuccessfulUpdate(area, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingArea_SetsIsActiveTrue()
    {
        var area = CreateFakeArea(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);

        await _sut.ActivateAsync(1, callerId: 1);

        area.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(area, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentArea_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Area?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var area = CreateFakeArea(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);

        await _sut.ActivateAsync(1, callerId: 8);

        area.UpdatedBy.Should().Be(8);
        area.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingArea_SetsIsActiveFalse()
    {
        var area = CreateFakeArea(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);

        await _sut.DeactivateAsync(1, callerId: 1);

        area.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(area, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentArea_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Area?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("AREA_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var area = CreateFakeArea(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);

        await _sut.DeactivateAsync(1, callerId: 6);

        area.UpdatedBy.Should().Be(6);
        area.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    /// <summary>
    /// Sets up all repo calls required for a successful CreateAsync flow
    /// (region exists, no name duplicate, create + save + re-fetch).
    /// </summary>
    private void SetupSuccessfulCreate(CreateAreaRequest request)
    {
        _repoMock.Setup(r => r.RegionExistsAsync(request.RegionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.RegionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        // Re-fetch after create returns a populated entity
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeArea());
    }

    /// <summary>
    /// Sets up all repo calls required for a successful UpdateAsync flow
    /// (region exists, no name duplicate, update + save + re-fetch).
    /// Note: GetByIdAsync for the initial fetch must be set up by the calling test.
    /// </summary>
    private void SetupSuccessfulUpdate(Area existingArea, UpdateAreaRequest request, int areaId)
    {
        _repoMock.Setup(r => r.RegionExistsAsync(request.RegionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.RegionId, areaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(existingArea, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingArea);
    }

    private void SetupGetByIdAfterUpdate(int areaId, Area area)
    {
        _repoMock.Setup(r => r.UpdateAsync(area, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(area);
    }
}
