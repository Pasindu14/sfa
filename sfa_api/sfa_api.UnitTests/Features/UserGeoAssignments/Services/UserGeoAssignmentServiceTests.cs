using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserGeoAssignments.Services;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.UnitTests.Features.UserGeoAssignments.Services;

public class UserGeoAssignmentServiceTests
{
    private readonly Mock<IUserGeoAssignmentRepository> _repoMock;
    private readonly UserGeoAssignmentService _sut;

    public UserGeoAssignmentServiceTests()
    {
        _repoMock = new Mock<IUserGeoAssignmentRepository>();
        _sut = new UserGeoAssignmentService(
            _repoMock.Object,
            NullLogger<UserGeoAssignmentService>.Instance);
    }

    // ─────────────────────────────────────────────────
    // Fake builders
    // ─────────────────────────────────────────────────

    private static Region CreateFakeRegion(int id = 10) => new()
    {
        Id = id, Name = "Test Region", IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Area CreateFakeArea(int id = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Area", RegionId = regionId, IsActive = true,
        Region = CreateFakeRegion(regionId),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Territory CreateFakeTerritory(int id = 30, int areaId = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Territory", AreaId = areaId, RegionId = regionId, IsActive = true,
        Area = CreateFakeArea(areaId, regionId),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Division CreateFakeDivision(
        int id = 5, int territoryId = 30, int areaId = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Division",
        TerritoryId = territoryId, AreaId = areaId, RegionId = regionId,
        IsActive = true,
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static User CreateFakeUser(int id, UserRole role = UserRole.SalesRep) => new()
    {
        Id = id, Name = $"User {id}", Role = role, IsActive = true, IsDeleted = false,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static UserGeoAssignment CreateFakeGeo(
        int id = 1, int userId = 10, bool isActive = true) => new()
    {
        Id = id,
        UserId = userId,
        DivisionId = 5, TerritoryId = 30, AreaId = 20, RegionId = 10,
        EffectiveFrom = new DateOnly(2026, 1, 1),
        IsActive = isActive,
        User = CreateFakeUser(userId),
        Division = CreateFakeDivision(),
        Territory = CreateFakeTerritory(),
        Area = CreateFakeArea(),
        Region = CreateFakeRegion(),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static UserReportingLine CreateFakeRl(int userId = 10, int managerId = 20) => new()
    {
        Id = 100, UserId = userId, ReportsToUserId = managerId,
        EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true,
        User = CreateFakeUser(userId),
        ReportsToUser = CreateFakeUser(managerId),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static CreateUserAssignmentRequest CreateValidRequest(int userId = 10, int? divisionId = 5) => new()
    {
        UserId = userId,
        DivisionId = divisionId,
        EffectiveFrom = new DateOnly(2026, 3, 26)
    };

    private static UpdateUserAssignmentRequest CreateValidUpdateRequest(int? divisionId = 5) => new()
    {
        DivisionId = divisionId,
        EffectiveFrom = new DateOnly(2026, 4, 1)
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingRecord_ReturnsMappedDto()
    {
        var geo = CreateFakeGeo();
        var rl = CreateFakeRl(geo.UserId, 20);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { rl });

        var result = await _sut.GetByIdAsync(1);

        result.Id.Should().Be(geo.Id);
        result.UserId.Should().Be(geo.UserId);
        result.UserName.Should().Be(geo.User!.Name);
        result.DivisionId.Should().Be(geo.DivisionId);
        result.TerritoryId.Should().Be(geo.TerritoryId);
        result.AreaId.Should().Be(geo.AreaId);
        result.RegionId.Should().Be(geo.RegionId);
        result.ReportsToUserId.Should().Be(rl.ReportsToUserId);
        result.ReportsToUserName.Should().Be(rl.ReportsToUser!.Name);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NoActiveRl_ReturnsDtoWithNullReportsTo()
    {
        var geo = CreateFakeGeo();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());

        var result = await _sut.GetByIdAsync(1);

        result.ReportsToUserId.Should().BeNull();
        result.ReportsToUserName.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentRecord_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserGeoAssignment?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERASSIGNMENT_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedListDto()
    {
        var geos = new[] { CreateFakeGeo(1, userId: 10), CreateFakeGeo(2, userId: 11) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((geos.AsEnumerable(), 2));
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());

        var result = await _sut.GetAllAsync(1, 10);

        result.UserAssignments.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<UserGeoAssignment>(), 0));
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());

        await _sut.GetAllAsync(2, 10);

        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // GetStatsAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_MapsStatsTupleToDto()
    {
        _repoMock.Setup(r => r.GetStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Total: 24, Active: 18, ActiveTerritories: 8, ThisMonth: 6));

        var result = await _sut.GetStatsAsync();

        result.TotalAssignments.Should().Be(24);
        result.ActiveAssignments.Should().Be(18);
        result.ActiveTerritories.Should().Be(8);
        result.AssignmentsThisMonth.Should().Be(6);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_UserNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidRequest(userId: 10);
        _repoMock.Setup(r => r.UserExistsAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_AdminUserAsSubordinate_ThrowsBusinessRuleException()
    {
        var request = CreateValidRequest(userId: 10);
        _repoMock.Setup(r => r.UserExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.IsAdminOrDistributorAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("USER_ROLE_NOT_ASSIGNABLE");
    }

    [Fact]
    public async Task CreateAsync_WithExistingGeoAssignment_DeactivatesOldGeo()
    {
        var request = CreateValidRequest();
        var existingGeo = CreateFakeGeo(id: 50, userId: 10, isActive: true);
        SetupSuccessfulCreate(request, existingGeo: existingGeo);
        UserGeoAssignment? deactivated = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserGeoAssignment>(), It.IsAny<CancellationToken>()))
                 .Callback<UserGeoAssignment, CancellationToken>((g, _) => deactivated = g)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        deactivated.Should().NotBeNull();
        deactivated!.Id.Should().Be(50);
        deactivated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidRequest();
        SetupSuccessfulCreate(request);
        UserGeoAssignment? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserGeoAssignment>(), It.IsAny<CancellationToken>()))
                 .Callback<UserGeoAssignment, CancellationToken>((g, _) => captured = g)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 7);

        captured!.CreatedBy.Should().Be(7);
        captured.UpdatedBy.Should().Be(7);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsSaveChangesOnce()
    {
        var request = CreateValidRequest();
        SetupSuccessfulCreate(request);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsGeoIdsFromRequest()
    {
        var request = new CreateUserAssignmentRequest
        {
            UserId = 10,
            RegionId = 1, AreaId = 2, TerritoryId = 3, DivisionId = 4, RouteId = 5,
            EffectiveFrom = new DateOnly(2026, 3, 26)
        };
        SetupSuccessfulCreate(request);
        UserGeoAssignment? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserGeoAssignment>(), It.IsAny<CancellationToken>()))
                 .Callback<UserGeoAssignment, CancellationToken>((g, _) => captured = g)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.RegionId.Should().Be(1);
        captured.AreaId.Should().Be(2);
        captured.TerritoryId.Should().Be(3);
        captured.DivisionId.Should().Be(4);
        captured.RouteId.Should().Be(5);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentRecord_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserGeoAssignment?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERASSIGNMENT_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_SetsAuditFields()
    {
        var geo = CreateFakeGeo();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        SetupSuccessfulUpdate(geo, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 8);

        geo.UpdatedBy.Should().Be(8);
        geo.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_CallsSaveChangesOnce()
    {
        var geo = CreateFakeGeo();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        SetupSuccessfulUpdate(geo, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SetsGeoIdsFromRequest()
    {
        var geo = CreateFakeGeo();
        var request = new UpdateUserAssignmentRequest
        {
            RegionId = 11, AreaId = 22, TerritoryId = 33, DivisionId = 44, RouteId = 55,
            EffectiveFrom = new DateOnly(2026, 4, 1)
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        SetupSuccessfulUpdate(geo, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        geo.RegionId.Should().Be(11);
        geo.AreaId.Should().Be(22);
        geo.TerritoryId.Should().Be(33);
        geo.DivisionId.Should().Be(44);
        geo.RouteId.Should().Be(55);
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NonExistentRecord_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserGeoAssignment?)null);

        var act = () => _sut.DeleteAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERASSIGNMENT_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_ExistingRecord_SetsGeoIsActiveFalse()
    {
        var geo = CreateFakeGeo(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        _repoMock.Setup(r => r.UpdateAsync(geo, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1, callerId: 1);

        geo.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(geo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsSaveChangesOnce()
    {
        var geo = CreateFakeGeo(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserGeoAssignment>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(
        CreateUserAssignmentRequest request,
        UserGeoAssignment? existingGeo = null)
    {
        _repoMock.Setup(r => r.UserExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.IsAdminOrDistributorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.DivisionExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        _repoMock.Setup(r => r.GetActiveByUserIdAsync(request.UserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingGeo);
        if (existingGeo is not null)
            _repoMock.Setup(r => r.UpdateAsync(existingGeo, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserGeoAssignment>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeGeo(userId: request.UserId));
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());
    }

    private void SetupSuccessfulUpdate(
        UserGeoAssignment geo,
        UpdateUserAssignmentRequest request,
        int geoId)
    {
        _repoMock.Setup(r => r.UpdateAsync(geo, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(geoId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(geo);
        _repoMock.Setup(r => r.GetActiveReportingLinesByUserIdsAsync(
                     It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());
    }
}
