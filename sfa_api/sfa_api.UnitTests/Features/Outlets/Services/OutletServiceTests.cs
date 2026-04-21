using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Outlets.Repositories;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Features.Outlets.Services;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Infrastructure.Caching;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.UnitTests.Features.Outlets.Services;

public class OutletServiceTests
{
    private readonly Mock<IOutletRepository> _repoMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly OutletService _sut;

    public OutletServiceTests()
    {
        _repoMock  = new Mock<IOutletRepository>();
        _cacheMock = new Mock<ICacheService>();
        _sut = new OutletService(_repoMock.Object, _cacheMock.Object, NullLogger<OutletService>.Instance);
    }

    // ─────────────────────────────────────────────────
    // Fake factory helpers
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
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Region = CreateFakeRegion(regionId)
    };

    private static Territory CreateFakeTerritory(int id = 30, int areaId = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Territory", AreaId = areaId, RegionId = regionId, IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Area = CreateFakeArea(areaId, regionId)
    };

    private static Division CreateFakeDivision(int id = 40, int territoryId = 30, int areaId = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Division", TerritoryId = territoryId, AreaId = areaId, RegionId = regionId,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId)
    };

    private static RouteEntity CreateFakeRoute(int id = 50, int divisionId = 40, int territoryId = 30, int areaId = 20, int regionId = 10) => new()
    {
        Id = id, Name = "Test Route", PinColor = "#FF5733",
        DivisionId = divisionId, TerritoryId = territoryId, AreaId = areaId, RegionId = regionId,
        IsActive = true,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Division = CreateFakeDivision(divisionId, territoryId, areaId, regionId),
        Territory = CreateFakeTerritory(territoryId, areaId, regionId),
        Area = CreateFakeArea(areaId, regionId),
        Region = CreateFakeRegion(regionId)
    };

    private static Outlet CreateFakeOutlet(int id = 1, int routeId = 50, int divisionId = 40, int territoryId = 30, int areaId = 20, int regionId = 10, bool isActive = true) => new()
    {
        Id = id,
        Name = "Test Outlet",
        Address = "123 Test Street",
        Tel = "0771234567",
        NicNo = "901234567V",
        CreditLimit = 0,
        Latitude = 6.9271,
        Longitude = 79.8612,
        OutletType = OutletType.Medium,
        OutletCategory = OutletCategory.Wholesale,
        ProvinceCode = 1,
        DistrictCode = 11,
        RouteId = routeId,
        DivisionId = divisionId,
        TerritoryId = territoryId,
        AreaId = areaId,
        RegionId = regionId,
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Route = CreateFakeRoute(routeId, divisionId, territoryId, areaId, regionId)
    };

    private static CreateOutletRequest CreateValidCreateRequest() => new()
    {
        Name = "New Outlet",
        Address = "456 New Street",
        Tel = "0779876543",
        NicNo = "851234567V",
        CreditLimit = 0,
        Latitude = 6.9271,
        Longitude = 79.8612,
        OutletType = "Medium",
        OutletCategory = "Wholesale",
        ProvinceCode = 1,
        DistrictCode = 11,
        RouteId = 50
    };

    private static UpdateOutletRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Outlet",
        Address = "789 Updated Street",
        Tel = "0771111111",
        NicNo = "851234567V",
        CreditLimit = 500,
        Latitude = 7.0,
        Longitude = 80.0,
        OutletType = "Large",
        OutletCategory = "SMMT",
        ProvinceCode = 2,
        DistrictCode = 22,
        RouteId = 50
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingOutlet_ReturnsMappedDto()
    {
        var outlet = CreateFakeOutlet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(outlet.Id);
        result.Name.Should().Be(outlet.Name);
        result.Address.Should().Be(outlet.Address);
        result.Tel.Should().Be(outlet.Tel);
        result.NicNo.Should().Be(outlet.NicNo);
        result.RouteId.Should().Be(outlet.RouteId);
        result.DivisionId.Should().Be(outlet.DivisionId);
        result.TerritoryId.Should().Be(outlet.TerritoryId);
        result.AreaId.Should().Be(outlet.AreaId);
        result.RegionId.Should().Be(outlet.RegionId);
        result.OutletType.Should().Be("Medium");
        result.OutletCategory.Should().Be("Wholesale");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(outlet.CreatedAt);
        result.UpdatedAt.Should().Be(outlet.UpdatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentOutlet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Outlet?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("OUTLET_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedOutletListDto()
    {
        var outlets = new[] { CreateFakeOutlet(1), CreateFakeOutlet(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((outlets.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Outlets.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Outlet>(), 0));

        await _sut.GetAllAsync(2, 10);

        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Outlet>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Outlets.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ActiveFilter_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, true, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Outlet>(), 0));

        await _sut.GetAllAsync(1, 10, isActive: true);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_InactiveFilter_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, false, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Outlet>(), 0));

        await _sut.GetAllAsync(1, 10, isActive: false);

        _repoMock.Verify(r => r.GetAllAsync(0, 10, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SearchParam_ForwardedToRepository()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, "pharmacy", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Outlet>(), 0));

        await _sut.GetAllAsync(1, 10, search: "pharmacy");

        _repoMock.Verify(r => r.GetAllAsync(0, 10, null, "pharmacy", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // GetAllActiveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveOutlets()
    {
        var active = new[] { CreateFakeOutlet(1, isActive: true), CreateFakeOutlet(2, isActive: true) };
        _repoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(active.AsEnumerable());

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.IsActive);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_RouteNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_InvalidOutletType_ThrowsValidationException()
    {
        var request = CreateValidCreateRequest();
        request.OutletType = "Giant";
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("OutletType");
    }

    [Fact]
    public async Task CreateAsync_InvalidOutletCategory_ThrowsValidationException()
    {
        var request = CreateValidCreateRequest();
        request.OutletCategory = "Hypermarket";
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("OutletCategory");
    }

    [Fact]
    public async Task CreateAsync_InvalidBillingPriceType_ThrowsValidationException()
    {
        var request = CreateValidCreateRequest();
        request.BillingPriceType = "DiscountPrice";
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("BillingPriceType");
    }

    [Fact]
    public async Task CreateAsync_DuplicateNicNo_ThrowsDuplicateResourceException()
    {
        var request = CreateValidCreateRequest();
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NICNO_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_DenormalizesAncestorIds()
    {
        var request = CreateValidCreateRequest();
        var route = CreateFakeRoute(id: 50, divisionId: 40, territoryId: 30, areaId: 20, regionId: 10);
        SetupSuccessfulCreate(request, route);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.RouteId.Should().Be(50);
        captured.DivisionId.Should().Be(40);
        captured.TerritoryId.Should().Be(30);
        captured.AreaId.Should().Be(20);
        captured.RegionId.Should().Be(10);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 5);

        captured!.CreatedBy.Should().Be(5);
        captured.UpdatedBy.Should().Be(5);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        captured.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: null);

        captured!.CreatedBy.Should().BeNull();
        captured.UpdatedBy.Should().BeNull();
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
    public async Task CreateAsync_ValidRequest_SetsCorrectBusinessFields()
    {
        var request = CreateValidCreateRequest();
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.Name.Should().Be(request.Name);
        captured.Address.Should().Be(request.Address);
        captured.Tel.Should().Be(request.Tel);
        captured.NicNo.Should().Be(request.NicNo);
        captured.Latitude.Should().Be(request.Latitude);
        captured.Longitude.Should().Be(request.Longitude);
        captured.OutletType.Should().Be(OutletType.Medium);
        captured.OutletCategory.Should().Be(OutletCategory.Wholesale);
    }

    [Fact]
    public async Task CreateAsync_WithNullBillingPriceType_SetsBillingPriceTypeNull()
    {
        var request = CreateValidCreateRequest();
        request.BillingPriceType = null;
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.BillingPriceType.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidBillingPriceType_SetsBillingPriceType()
    {
        var request = CreateValidCreateRequest();
        request.BillingPriceType = "DealerPrice";
        SetupSuccessfulCreate(request);
        Outlet? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Callback<Outlet, CancellationToken>((e, _) => captured = e)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.BillingPriceType.Should().Be(BillingPriceType.DealerPrice);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentOutlet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Outlet?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("OUTLET_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_RouteNotFound_ThrowsNotFoundException()
    {
        var outlet = CreateFakeOutlet();
        var request = CreateValidUpdateRequest();
        request.RouteId = 999;
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((RouteEntity?)null);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNicNo_ThrowsDuplicateResourceException()
    {
        var outlet = CreateFakeOutlet();
        var request = CreateValidUpdateRequest();
        request.NicNo = "999999999V";
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("NICNO_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var outlet = CreateFakeOutlet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        SetupSuccessfulUpdate(outlet, CreateValidUpdateRequest(), 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        outlet.UpdatedBy.Should().Be(7);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var outlet = CreateFakeOutlet();
        var request = CreateValidUpdateRequest();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        SetupSuccessfulUpdate(outlet, request, 1);

        await _sut.UpdateAsync(1, request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NewRoute_DenormalizesNewAncestorIds()
    {
        var outlet = CreateFakeOutlet();
        var newRoute = CreateFakeRoute(id: 60, divisionId: 70, territoryId: 80, areaId: 90, regionId: 100);
        var request = CreateValidUpdateRequest();
        request.RouteId = 60;
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(60, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(newRoute);
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(outlet);

        await _sut.UpdateAsync(1, request, callerId: 1);

        outlet.RouteId.Should().Be(60);
        outlet.DivisionId.Should().Be(70);
        outlet.TerritoryId.Should().Be(80);
        outlet.AreaId.Should().Be(90);
        outlet.RegionId.Should().Be(100);
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NonExistentOutlet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Outlet?)null);

        var act = () => _sut.DeleteAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("OUTLET_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_ExistingOutlet_CallsDeleteAndSaveChanges()
    {
        var outlet = CreateFakeOutlet();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingOutlet_SetsIsActiveTrue()
    {
        var outlet = CreateFakeOutlet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 1);

        outlet.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_ExistingOutlet_UpdatesAuditFields()
    {
        var outlet = CreateFakeOutlet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 7);

        outlet.UpdatedBy.Should().Be(7);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ActivateAsync_NonExistentOutlet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Outlet?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("OUTLET_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_CallsSaveChanges()
    {
        var outlet = CreateFakeOutlet(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.ActivateAsync(1, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingOutlet_SetsIsActiveFalse()
    {
        var outlet = CreateFakeOutlet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 1);

        outlet.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ExistingOutlet_UpdatesAuditFields()
    {
        var outlet = CreateFakeOutlet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 9);

        outlet.UpdatedBy.Should().Be(9);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentOutlet_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Outlet?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("OUTLET_NOT_FOUND");
    }

    [Fact]
    public async Task DeactivateAsync_CallsSaveChanges()
    {
        var outlet = CreateFakeOutlet(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(outlet);
        _repoMock.Setup(r => r.UpdateAsync(outlet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeactivateAsync(1, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(CreateOutletRequest request, RouteEntity? route = null)
    {
        var fakeRoute = route ?? CreateFakeRoute(request.RouteId);
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(fakeRoute);
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Outlet>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeOutlet());
    }

    private void SetupSuccessfulUpdate(Outlet existingOutlet, UpdateOutletRequest request, int outletId)
    {
        _repoMock.Setup(r => r.GetRouteWithAncestorsAsync(request.RouteId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeRoute(request.RouteId));
        _repoMock.Setup(r => r.ExistsByNicNoAsync(request.NicNo, outletId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(existingOutlet, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(outletId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingOutlet);
    }
}
