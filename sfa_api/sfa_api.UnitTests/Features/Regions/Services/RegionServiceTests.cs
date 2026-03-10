using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Regions.Repositories;
using sfa_api.Features.Regions.Requests;
using sfa_api.Features.Regions.Services;

namespace sfa_api.UnitTests.Features.Regions.Services;

public class RegionServiceTests
{
    private readonly Mock<IRegionRepository> _repoMock;
    private readonly RegionService _sut;

    public RegionServiceTests()
    {
        _repoMock = new Mock<IRegionRepository>();
        _sut = new RegionService(_repoMock.Object, NullLogger<RegionService>.Instance);
    }

    private static Region CreateFakeRegion(int id = 1, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Region",
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1
    };

    private static CreateRegionRequest CreateValidCreateRequest() => new()
    {
        Name = "North Region"
    };

    private static UpdateRegionRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Region"
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingRegion_ReturnsMappedDto()
    {
        var region = CreateFakeRegion();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(region.Id);
        result.Name.Should().Be(region.Name);
        result.IsActive.Should().Be(region.IsActive);
        result.CreatedAt.Should().Be(region.CreatedAt);
        result.UpdatedAt.Should().Be(region.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentRegion_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Region?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("REGION_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyMappedDtos()
    {
        var activeRegions = new[]
        {
            new Region { Id = 1, Name = "Alpha", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Region { Id = 2, Name = "Beta",  IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeRegions.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(dto => dto.IsActive);
    }

    [Fact]
    public async Task GetAllActiveAsync_MapsDtoFieldsCorrectly()
    {
        var region = new Region
        {
            Id = 7,
            Name = "Northern Region",
            IsActive = true,
            CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 3, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { region }.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        var dto = result.Single();
        dto.Id.Should().Be(7);
        dto.Name.Should().Be("Northern Region");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(region.CreatedAt);
        dto.UpdatedAt.Should().Be(region.UpdatedAt);
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepo_ReturnsEmptyEnumerable()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Region>());

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActiveAsync_CallsRepoGetAllActiveAsync()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Region>());

        await _sut.GetAllActiveAsync();

        _repoMock.Verify(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedListDto()
    {
        var regions = new[] { CreateFakeRegion(1), CreateFakeRegion(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((regions.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Regions.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Region>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page - 1) * pageSize = (2 - 1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyRegionsList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Region>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Regions.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "test";

        _repoMock.Setup(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Region>(), 0));

        await _sut.GetAllAsync(page: 1, pageSize: 10, search: search);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();

        var result = await _sut.CreateAsync(request, callerId: 1);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();
        Region? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Region>(), It.IsAny<CancellationToken>()))
                 .Callback<Region, CancellationToken>((r, _) => captured = r)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();
        Region? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Region>(), It.IsAny<CancellationToken>()))
                 .Callback<Region, CancellationToken>((r, _) => captured = r)
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
        SetupNoNameDuplicateForCreate();

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();
        Region? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Region>(), It.IsAny<CancellationToken>()))
                 .Callback<Region, CancellationToken>((r, _) => captured = r)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var region = CreateFakeRegion();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);
        SetupNoNameDuplicateForUpdate(1);

        var request = CreateValidUpdateRequest();
        var result = await _sut.UpdateAsync(1, request, callerId: 2);

        result.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentRegion_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Region?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var region = CreateFakeRegion();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Region", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new UpdateRegionRequest { Name = "Taken Region" };
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        // ExistsByNameAsync with excludeId returns false => same name on own record is allowed
        var region = CreateFakeRegion();
        region.Name = "My Region";
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);
        _repoMock.Setup(r => r.ExistsByNameAsync("My Region", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var request = new UpdateRegionRequest { Name = "My Region" };
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var region = CreateFakeRegion();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);
        SetupNoNameDuplicateForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        region.UpdatedBy.Should().Be(7);
        region.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var region = CreateFakeRegion();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);
        SetupNoNameDuplicateForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingRegion_SetsIsActiveTrue()
    {
        var region = CreateFakeRegion(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);

        await _sut.ActivateAsync(1, callerId: 1);

        region.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(region, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentRegion_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Region?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var region = CreateFakeRegion(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);

        await _sut.ActivateAsync(1, callerId: 8);

        region.UpdatedBy.Should().Be(8);
        region.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingRegion_SetsIsActiveFalse()
    {
        var region = CreateFakeRegion(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);

        await _sut.DeactivateAsync(1, callerId: 1);

        region.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(region, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentRegion_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Region?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var region = CreateFakeRegion(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(region);

        await _sut.DeactivateAsync(1, callerId: 6);

        region.UpdatedBy.Should().Be(6);
        region.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupNoNameDuplicateForCreate()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }

    private void SetupNoNameDuplicateForUpdate(int excludeId)
    {
        _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), excludeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }
}
