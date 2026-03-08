using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Distributors.Services;

namespace sfa_api.UnitTests.Features.Distributors.Services;

public class DistributorServiceTests
{
    private readonly Mock<IDistributorRepository> _repoMock;
    private readonly DistributorService _sut;

    public DistributorServiceTests()
    {
        _repoMock = new Mock<IDistributorRepository>();
        _sut = new DistributorService(_repoMock.Object, NullLogger<DistributorService>.Instance);
    }

    private static Distributor CreateFakeDistributor(int id = 1) => new()
    {
        Id = id,
        Name = "Test Distributor",
        Address = "123 Main Street",
        Phone = "0771234567",
        Email = "distributor@example.com",
        Alias = 101,
        TradeDiscount = 10.00m,
        Commission = 5.00m,
        Remark = "Test remark",
        VatRegNo = "VAT123456",
        Latitude = 6.9271,
        Longitude = 79.8612,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = 1,
        UpdatedBy = 1,
        IsDeleted = false
    };

    private static CreateDistributorRequest CreateValidRequest() => new()
    {
        Name = "New Distributor",
        Address = "456 Commerce Road",
        Phone = "0779876543",
        Email = "newdist@example.com",
        Alias = 202,
        TradeDiscount = 15.00m,
        Commission = 7.50m,
        Remark = "New distributor remark",
        VatRegNo = "VAT654321",
        Latitude = 7.2906,
        Longitude = 80.6337
    };

    private static UpdateDistributorRequest CreateValidUpdateRequest() => new()
    {
        Name = "Updated Distributor",
        Address = "789 Updated Street",
        Phone = "0771112233",
        Email = "updated@example.com",
        Alias = 303,
        TradeDiscount = 20.00m,
        Commission = 10.00m,
        Remark = "Updated remark",
        VatRegNo = "VAT999888",
        Latitude = 8.3114,
        Longitude = 80.4037
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingDistributor_ReturnsDto()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(distributor.Id);
        result.Name.Should().Be(distributor.Name);
        result.Address.Should().Be(distributor.Address);
        result.Phone.Should().Be(distributor.Phone);
        result.Email.Should().Be(distributor.Email);
        result.Alias.Should().Be(distributor.Alias);
        result.TradeDiscount.Should().Be(distributor.TradeDiscount);
        result.Commission.Should().Be(distributor.Commission);
        result.Remark.Should().Be(distributor.Remark);
        result.VatRegNo.Should().Be(distributor.VatRegNo);
        result.Latitude.Should().Be(distributor.Latitude);
        result.Longitude.Should().Be(distributor.Longitude);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentDistributor_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

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
        var distributors = new[] { CreateFakeDistributor(1), CreateFakeDistributor(2) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((distributors.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.Distributors.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Distributor>(), 0));

        await _sut.GetAllAsync(2, 10);

        // skip = (page-1) * pageSize = (2-1) * 10 = 10
        _repoMock.Verify(r => r.GetAllAsync(10, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Distributor>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.Distributors.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var request = CreateValidRequest();
        SetupNoDuplicatesForCreate();

        var result = await _sut.CreateAsync(request, callerId: 1);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Address.Should().Be(request.Address);
        result.Phone.Should().Be(request.Phone);
        result.Email.Should().Be(request.Email);
        result.Alias.Should().Be(request.Alias);
        result.TradeDiscount.Should().Be(request.TradeDiscount);
        result.Commission.Should().Be(request.Commission);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidRequest();
        SetupNoDuplicatesForCreate();
        Distributor? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Distributor>(), It.IsAny<CancellationToken>()))
                 .Callback<Distributor, CancellationToken>((d, _) => captured = d)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidRequest();
        SetupNoDuplicatesForCreate();
        Distributor? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Distributor>(), It.IsAny<CancellationToken>()))
                 .Callback<Distributor, CancellationToken>((d, _) => captured = d)
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
        SetupNoDuplicatesForCreate();

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("EMAIL_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_DuplicatePhone_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(request.Phone, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("PHONE_DUPLICATE");
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_DoesNotCheckPhone()
    {
        // Email check short-circuits — phone check should never be called
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateResourceException>(() => _sut.CreateAsync(request, callerId: 1));

        _repoMock.Verify(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullCallerId_SetsNullAuditFields()
    {
        var request = CreateValidRequest();
        SetupNoDuplicatesForCreate();
        Distributor? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Distributor>(), It.IsAny<CancellationToken>()))
                 .Callback<Distributor, CancellationToken>((d, _) => captured = d)
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
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);
        SetupNoDuplicatesForUpdate(1);

        var request = CreateValidUpdateRequest();
        var result = await _sut.UpdateAsync(1, request, callerId: 2);

        result.Name.Should().Be(request.Name);
        result.Address.Should().Be(request.Address);
        result.Phone.Should().Be(request.Phone);
        result.Email.Should().Be(request.Email);
        result.Alias.Should().Be(request.Alias);
        result.TradeDiscount.Should().Be(request.TradeDiscount);
        result.Commission.Should().Be(request.Commission);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentDistributor_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateEmail_ThrowsDuplicateResourceException()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);
        _repoMock.Setup(r => r.ExistsByEmailAsync("taken@example.com", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = CreateValidUpdateRequest();
        request.Email = "taken@example.com";
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("EMAIL_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_DuplicatePhone_ThrowsDuplicateResourceException()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync("0770001111", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = CreateValidUpdateRequest();
        request.Phone = "0770001111";
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<DuplicateResourceException>();
        ex.Which.ErrorCode.Should().Be("PHONE_DUPLICATE");
    }

    [Fact]
    public async Task UpdateAsync_SetsAuditFields()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);
        SetupNoDuplicatesForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 7);

        distributor.UpdatedBy.Should().Be(7);
        distributor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_CallsSaveChanges()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);
        SetupNoDuplicatesForUpdate(1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingDistributor_CallsRepoDeleteAndSave()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentDistributor_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentDistributor_NeverCallsRepoDelete()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));

        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingDistributor_SetsIsActiveTrue()
    {
        var distributor = CreateFakeDistributor();
        distributor.IsActive = false;
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        await _sut.ActivateAsync(1, callerId: 1);

        distributor.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(distributor, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentDistributor_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var distributor = CreateFakeDistributor();
        distributor.IsActive = false;
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        await _sut.ActivateAsync(1, callerId: 8);

        distributor.UpdatedBy.Should().Be(8);
        distributor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeactivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ExistingDistributor_SetsIsActiveFalse()
    {
        var distributor = CreateFakeDistributor();
        distributor.IsActive = true;
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        await _sut.DeactivateAsync(1, callerId: 1);

        distributor.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(distributor, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentDistributor_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Distributor?)null);

        var act = () => _sut.DeactivateAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_SetsAuditFields()
    {
        var distributor = CreateFakeDistributor();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(distributor);

        await _sut.DeactivateAsync(1, callerId: 6);

        distributor.UpdatedBy.Should().Be(6);
        distributor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupNoDuplicatesForCreate()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }

    private void SetupNoDuplicatesForUpdate(int excludeId)
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), excludeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), excludeId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }
}
