using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Routes.Repositories;
using sfa_api.Features.Routes.Requests;
using sfa_api.Features.Routes.Services;
using sfa_api.Features.Territories.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.UnitTests.Features.Routes.Services;

public class RouteServiceTests
{
    private readonly Mock<IRouteRepository> _repoMock;
    private readonly RouteService _sut;

    public RouteServiceTests()
    {
        _repoMock = new Mock<IRouteRepository>();
        _sut = new RouteService(_repoMock.Object, NullLogger<RouteService>.Instance);
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

    private static Division CreateFakeDivision(int id = 40, int territoryId = 30, int areaId = 20, int regionId = 10, string name = "Test Division") => new()
    {
        Id = id,
        Name = name,
        TerritoryId = territoryId,
        AreaId = areaId,
        RegionId = regionId,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId)
    };

    private static RouteEntity CreateFakeRoute(int id = 1, int divisionId = 40, int territoryId = 30, int areaId = 20, int regionId = 10, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Route",
        PinColor = "#FF5733",
        Description = "A test route",
        DivisionId = divisionId,
        TerritoryId = territoryId,
        AreaId = areaId,
        RegionId = regionId,
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = 1,
        UpdatedBy = 1,
        Division = CreateFakeDivision(divisionId, territoryId, areaId, regionId),
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId)
    };

    private static CreateRouteRequest CreateValidCreateRequest() => new()
    {
        Name = "North Route",
        PinColor = "#FF5733",
        Description = "A route in the north",
        DivisionId = 40
    };

    private static UpdateRouteRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Route",
        PinColor = "#33FF57",
        Description = "Updated description",
        DivisionId = 40
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingRoute_ReturnsMappedDto()
    {
        var route = CreateFakeRoute();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(route.Id);
        result.Name.Should().Be(route.Name);
        result.PinColor.Should().Be(route.PinColor);
        result.Description.Should().Be(route.Description);
        result.DivisionId.Should().Be(route.DivisionId);
        result.DivisionName.Should().Be(route.Division!.Name);
        result.TerritoryId.Should().Be(route.TerritoryId);
        result.AreaId.Should().Be(route.AreaId);
        result.RegionId.Should().Be(route.RegionId);
        result.IsActive.Should().Be(route.IsActive);
        result.CreatedAt.Should().Be(route.CreatedAt);
        result.UpdatedAt.Should().Be(route.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentRoute_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedRouteListDto()
    {
        var routes = new[] { CreateFakeRoute(1), CreateFakeRoute(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((routes.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Routes.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<RouteEntity>(), 0));

        await _sut.GetAllAsync(2, 10);

        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyRouteList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<RouteEntity>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Routes.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ActiveFilter_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, true, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<RouteEntity>(), 0));

        await _sut.GetAllAsync(1, 10, isActive: true);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_InactiveFilter_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, false, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<RouteEntity>(), 0));

        await _sut.GetAllAsync(1, 10, isActive: false);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SearchParam_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, "north", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<RouteEntity>(), 0));

        await _sut.GetAllAsync(1, 10, search: "north");

        _repoMock.Verify(r => r.GetAllAsync(0, 10, null, "north", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveRoutes()
    {
        var activeRoutes = new[] { CreateFakeRoute(1, isActive: true), CreateFakeRoute(2, isActive: true) };
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(activeRoutes.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.IsActive);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DivisionNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(request.DivisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(request.DivisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeDivision(request.DivisionId));
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.DivisionId, It.IsAny<CancellationToken>()))
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
        RouteEntity? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Callback<RouteEntity, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        RouteEntity? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Callback<RouteEntity, CancellationToken>((e, _) => captured = e)
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
        var division = CreateFakeDivision(id: 40, territoryId: 30, areaId: 20, regionId: 10);
        SetupSuccessfulCreate(request, division);
        RouteEntity? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Callback<RouteEntity, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.DivisionId.Should().Be(40);
        captured.TerritoryId.Should().Be(30);
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
        RouteEntity? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Callback<RouteEntity, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsCorrectNameAndPinColor()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        RouteEntity? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Callback<RouteEntity, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.Name.Should().Be(request.Name);
        captured.PinColor.Should().Be(request.PinColor);
        captured.Description.Should().Be(request.Description);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentRoute_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DivisionNotFound_ThrowsNotFoundException()
    {
        var route = CreateFakeRoute();
        var request = new UpdateRouteRequest { Name = "New Name", PinColor = "#AABBCC", DivisionId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Division?)null);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DIVISION_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDuplicateResourceException()
    {
        var route = CreateFakeRoute();
        var request = new UpdateRouteRequest { Name = "Taken Route", PinColor = "#FF0000", DivisionId = 40 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(40, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeDivision(40));
        _repoMock.Setup(r => r.ExistsByNameAsync("Taken Route", 40, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NAME_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SameNameAsOwnRecord_DoesNotThrow()
    {
        var route = CreateFakeRoute();
        route.Name = "My Route";
        var request = new UpdateRouteRequest { Name = "My Route", PinColor = "#FF5733", DivisionId = 40 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(40, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeDivision(40));
        _repoMock.Setup(r => r.ExistsByNameAsync("My Route", 40, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, route);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var route = CreateFakeRoute();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        SetupSuccessfulUpdate(route, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        route.UpdatedBy.Should().Be(7);
        route.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var route = CreateFakeRoute();
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        SetupSuccessfulUpdate(route, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DenormalizesAncestorIdsFromNewDivision()
    {
        var route = CreateFakeRoute();
        var newDivision = CreateFakeDivision(id: 50, territoryId: 60, areaId: 70, regionId: 80);
        var request = new UpdateRouteRequest { Name = "Renamed Route", PinColor = "#00FF00", DivisionId = 50 };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(newDivision);
        _repoMock.Setup(r => r.ExistsByNameAsync("Renamed Route", 50, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        SetupGetByIdAfterUpdate(1, route);

        await _sut.UpdateAsync(1, request, callerId: 1);

        route.DivisionId.Should().Be(50);
        route.TerritoryId.Should().Be(60);
        route.AreaId.Should().Be(70);
        route.RegionId.Should().Be(80);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingRoute_SetsIsActiveTrue()
    {
        var route = CreateFakeRoute(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 1);

        route.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_ExistingRoute_UpdatesAuditFields()
    {
        var route = CreateFakeRoute(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 7);

        route.UpdatedBy.Should().Be(7);
        route.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ActivateAsync_NonExistentRoute_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_CallsSaveChanges()
    {
        var route = CreateFakeRoute(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingRoute_SetsIsActiveFalse()
    {
        var route = CreateFakeRoute(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 1);

        route.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ExistingRoute_UpdatesAuditFields()
    {
        var route = CreateFakeRoute(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 9);

        route.UpdatedBy.Should().Be(9);
        route.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentRoute_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateAsync_CallsSaveChanges()
    {
        var route = CreateFakeRoute(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(CreateRouteRequest request, Division? division = null)
    {
        var fakeDivision = division ?? CreateFakeDivision(request.DivisionId);
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(request.DivisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fakeDivision);
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.DivisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<RouteEntity>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute());
    }

    private void SetupSuccessfulUpdate(RouteEntity existingRoute, UpdateRouteRequest request, int routeId)
    {
        _repoMock.Setup(r => r.GetDivisionWithAncestorsAsync(request.DivisionId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeDivision(request.DivisionId));
        _repoMock.Setup(r => r.ExistsByNameAsync(request.Name, request.DivisionId, routeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(existingRoute, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(routeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingRoute);
    }

    private void SetupGetByIdAfterUpdate(int routeId, RouteEntity route)
    {
        _repoMock.Setup(r => r.UpdateAsync(route, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(routeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(route);
    }
}
