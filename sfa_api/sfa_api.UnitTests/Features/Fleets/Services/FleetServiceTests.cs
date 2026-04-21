using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Fleets.DTOs;
using sfa_api.Features.Fleets.Entities;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.Fleets.Requests;
using sfa_api.Features.Fleets.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Fleets.Services;

public class FleetServiceTests
{
    private readonly Mock<IFleetRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FleetService _sut;

    public FleetServiceTests()
    {
        _repoMock  = new Mock<IFleetRepository>();
        _cacheMock = new Mock<ICacheService>();
        _sut       = new FleetService(_repoMock.Object, _cacheMock.Object, NullLogger<FleetService>.Instance);
    }

    private static Fleet CreateFakeFleet(int id = 1, bool isActive = true) => new()
    {
        Id        = id,
        Name      = "Test Fleet",
        IsActive  = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1
    };

    private static CreateFleetRequest CreateValidCreateRequest() => new()
    {
        Name = "Northern Fleet"
    };

    private static UpdateFleetRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Fleet"
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingFleet_ReturnsMappedDto()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(fleet.Id);
        result.Name.Should().Be(fleet.Name);
        result.IsActive.Should().Be(fleet.IsActive);
        result.CreatedAt.Should().Be(fleet.CreatedAt);
        result.UpdatedAt.Should().Be(fleet.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentFleet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Fleet?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("FLEET_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedListDto()
    {
        var fleets = new[] { CreateFakeFleet(1), CreateFakeFleet(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((fleets.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Fleets.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Fleet>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page - 1) * pageSize = (2 - 1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyFleetsList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Fleet>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Fleets.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "north";
        _repoMock.Setup(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Fleet>(), 0));

        await _sut.GetAllAsync(page: 1, pageSize: 10, search: search);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyMappedDtos()
    {
        var activeFleets = new[]
        {
            new Fleet { Id = 1, Name = "Alpha", IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Fleet { Id = 2, Name = "Beta",  IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<FleetDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((IEnumerable<FleetDto>?)null);
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeFleets.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(dto => dto.IsActive);
    }

    [Fact]
    public async Task GetAllActiveAsync_MapsDtoFieldsCorrectly()
    {
        var fleet = new Fleet
        {
            Id        = 7,
            Name      = "Northern Fleet",
            IsActive  = true,
            CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 3, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<FleetDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((IEnumerable<FleetDto>?)null);
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { fleet }.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        var dto = result.Single();
        dto.Id.Should().Be(7);
        dto.Name.Should().Be("Northern Fleet");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(fleet.CreatedAt);
        dto.UpdatedAt.Should().Be(fleet.UpdatedAt);
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepo_ReturnsEmptyEnumerable()
    {
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<FleetDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((IEnumerable<FleetDto>?)null);
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Fleet>());

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActiveAsync_CallsRepoGetAllActiveAsync()
    {
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<FleetDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((IEnumerable<FleetDto>?)null);
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<Fleet>());

        await _sut.GetAllActiveAsync();

        _repoMock.Verify(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        Fleet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Fleet>(), It.IsAny<CancellationToken>()))
                 .Callback<Fleet, CancellationToken>((f, _) => captured = f)
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
        Fleet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Fleet>(), It.IsAny<CancellationToken>()))
                 .Callback<Fleet, CancellationToken>((f, _) => captured = f)
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
    public async Task CreateAsync_DuplicateName_NeverCallsRepoCreate()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateResourceException>(() => _sut.CreateAsync(request, callerId: 1));

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Fleet>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();
        Fleet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Fleet>(), It.IsAny<CancellationToken>()))
                 .Callback<Fleet, CancellationToken>((f, _) => captured = f)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_InvalidatesCacheByPrefix()
    {
        var request = CreateValidCreateRequest();
        SetupNoNameDuplicateForCreate();

        await _sut.CreateAsync(request, callerId: 1);

        _cacheMock.Verify(c => c.RemoveByPrefixAsync("fleets:list:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("fleets:all-active", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        SetupNoNameDuplicateForUpdate(1);

        var request = CreateValidUpdateRequest();
        var result  = await _sut.UpdateAsync(1, request, callerId: 2);

        result.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentFleet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Fleet?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Fleet", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new UpdateFleetRequest { Name = "Taken Fleet" };
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        // ExistsByNameAsync with excludeId returns false — updating to own name is allowed
        var fleet = CreateFakeFleet();
        fleet.Name = "My Fleet";
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        _repoMock.Setup(r => r.ExistsByNameAsync("My Fleet", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var request = new UpdateFleetRequest { Name = "My Fleet" };
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        SetupNoNameDuplicateForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        fleet.UpdatedBy.Should().Be(7);
        fleet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        SetupNoNameDuplicateForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_InvalidatesCacheByPrefix()
    {
        var fleet = CreateFakeFleet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);
        SetupNoNameDuplicateForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _cacheMock.Verify(c => c.RemoveByPrefixAsync("fleets:list:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("fleets:all-active", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingFleet_SetsIsActiveTrue()
    {
        var fleet = CreateFakeFleet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.ActivateAsync(1, callerId: 1);

        fleet.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(fleet, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentFleet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Fleet?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var fleet = CreateFakeFleet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.ActivateAsync(1, callerId: 8);

        fleet.UpdatedBy.Should().Be(8);
        fleet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ActivateAsync_InvalidatesCache()
    {
        var fleet = CreateFakeFleet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.ActivateAsync(1, callerId: 1);

        _cacheMock.Verify(c => c.RemoveByPrefixAsync("fleets:list:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("fleets:all-active", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingFleet_SetsIsActiveFalse()
    {
        var fleet = CreateFakeFleet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.DeactivateAsync(1, callerId: 1);

        fleet.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(fleet, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentFleet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Fleet?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var fleet = CreateFakeFleet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.DeactivateAsync(1, callerId: 6);

        fleet.UpdatedBy.Should().Be(6);
        fleet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DeactivateAsync_InvalidatesCache()
    {
        var fleet = CreateFakeFleet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fleet);

        await _sut.DeactivateAsync(1, callerId: 1);

        _cacheMock.Verify(c => c.RemoveByPrefixAsync("fleets:list:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("fleets:all-active", It.IsAny<CancellationToken>()), Times.Once);
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
