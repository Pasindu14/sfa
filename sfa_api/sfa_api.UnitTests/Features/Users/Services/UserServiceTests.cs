using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Areas.Repositories;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Divisions.Repositories;
using sfa_api.Features.Regions.Repositories;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Features.Users.DTOs;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Services;

namespace sfa_api.UnitTests.Features.Users.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly Mock<IDistributorRepository> _distributorRepoMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _distributorRepoMock = new Mock<IDistributorRepository>();
        _sut = new UserService(
            _repoMock.Object,
            _distributorRepoMock.Object,
            new Mock<IRegionRepository>().Object,
            new Mock<IAreaRepository>().Object,
            new Mock<ITerritoryRepository>().Object,
            new Mock<IDivisionRepository>().Object,
            NullLogger<UserService>.Instance);
    }

    private static User CreateFakeUser(int id = 1, string role = "Admin") => new()
    {
        Id = id,
        Name = "John Doe",
        Username = "johndoe",
        Email = "john@example.com",
        Phone = "1234567890",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
        Role = Enum.Parse<UserRole>(role),
        DeviceId = null,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = null,
        UpdatedBy = null,
        IsDeleted = false
    };

    private static CreateUserRequest CreateValidRequest() => new()
    {
        Name = "Jane Smith",
        Username = "janesmith",
        Email = "jane@example.com",
        Phone = "9876543210",
        Password = "Str0ng@Pass",
        Role = "Admin"
    };

    // ─────────────────────────────────────────────────
    // GetUserByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsDto()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(3, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync(3);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
        result.Username.Should().Be(user.Username);
        result.Email.Should().Be(user.Email);
        result.Phone.Should().Be(user.Phone);
        result.Role.Should().Be("Admin");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => _sut.GetUserByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // GetAllUsersAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_ReturnsPaginatedList()
    {
        var users = new[] { CreateFakeUser(1), CreateFakeUser(2) };
        _repoMock.Setup(r => r.GetAllUsersAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((users.AsEnumerable(), 2));

        var result = await _sut.GetAllUsersAsync(1, 10);

        result.Users.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllUsersAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllUsersAsync(10, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<User>(), 0));

        await _sut.GetAllUsersAsync(2, 10);

        // skip = (page-1) * pageSize = (2-1) * 10 = 10
        _repoMock.Verify(r => r.GetAllUsersAsync(10, 10, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllUsersAsync(0, 10, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<User>(), 0));

        var result = await _sut.GetAllUsersAsync(1, 10);

        result.Users.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "test";

        _repoMock.Setup(r => r.GetAllUsersAsync(0, 10, search, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<User>(), 0));

        await _sut.GetAllUsersAsync(page: 1, pageSize: 10, search: search);

        _repoMock.Verify(r => r.GetAllUsersAsync(0, 10, search, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // CreateUserAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ReturnsDto()
    {
        var request = CreateValidRequest();
        SetupNoDuplicates();

        var result = await _sut.CreateUserAsync(request, callerId: 1);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Username.Should().Be(request.Username);
        result.Email.Should().Be(request.Email);
        result.Role.Should().Be("Admin");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_HashesPassword()
    {
        var request = CreateValidRequest();
        SetupNoDuplicates();
        User? capturedUser = null;
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => capturedUser = u)
                 .Returns(Task.CompletedTask);

        await _sut.CreateUserAsync(request, callerId: 1);

        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, capturedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidRequest();
        SetupNoDuplicates();
        User? capturedUser = null;
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => capturedUser = u)
                 .Returns(Task.CompletedTask);

        await _sut.CreateUserAsync(request, callerId: 5);

        capturedUser!.CreatedBy.Should().Be(5);
        capturedUser.UpdatedBy.Should().Be(5);
        capturedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_CallsSaveChanges()
    {
        var request = CreateValidRequest();
        SetupNoDuplicates();

        await _sut.CreateUserAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateUserAsync(request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateUserAsync(request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task CreateUserAsync_DuplicatePhone_ThrowsDuplicateResourceException()
    {
        var request = CreateValidRequest();
        _repoMock.Setup(r => r.ExistsByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(request.Phone, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateUserAsync(request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_ThrowsValidationException()
    {
        var request = CreateValidRequest();
        request.Role = "InvalidRole";
        SetupNoDuplicates();

        var act = () => _sut.CreateUserAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("Role");
    }

    [Fact]
    public async Task CreateUserAsync_SalesRepRole_SetsDeviceId()
    {
        var request = CreateValidRequest();
        request.Role = "SalesRep";
        request.DeviceId = "device-123";
        SetupNoDuplicates();
        User? capturedUser = null;
        _repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => capturedUser = u)
                 .Returns(Task.CompletedTask);

        await _sut.CreateUserAsync(request, callerId: 1);

        capturedUser!.Role.Should().Be(UserRole.SalesRep);
        capturedUser.DeviceId.Should().Be("device-123");
    }

    // ─────────────────────────────────────────────────
    // UpdateUserAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        SetupNoDuplicatesForUpdate(1);

        var request = new UpdateUserRequest
        {
            Name = "Updated Name",
            Username = "updateduser",
            Email = "updated@example.com",
            Phone = "5555555555",
            Role = "Manager"
        };

        var result = await _sut.UpdateUserAsync(1, request, callerId: 2);

        result.Name.Should().Be("Updated Name");
        result.Username.Should().Be("updateduser");
        result.Email.Should().Be("updated@example.com");
        result.Role.Should().Be("Manager");
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var request = new UpdateUserRequest { Name = "X", Username = "x", Email = "x@x.com", Phone = "1234567890", Role = "Admin" };
        var act = () => _sut.UpdateUserAsync(99, request, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUserAsync_DuplicateUsername_ThrowsDuplicateResourceException()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _repoMock.Setup(r => r.ExistsByUsernameAsync("taken", 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new UpdateUserRequest { Name = "X", Username = "taken", Email = "x@x.com", Phone = "1234567890", Role = "Admin" };
        var act = () => _sut.UpdateUserAsync(1, request, callerId: 1);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidRole_ThrowsValidationException()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        SetupNoDuplicatesForUpdate(1);

        var request = new UpdateUserRequest { Name = "X", Username = "x", Email = "x@x.com", Phone = "1234567890", Role = "BadRole" };
        var act = () => _sut.UpdateUserAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("Role");
    }

    [Fact]
    public async Task UpdateUserAsync_SetsAuditFields()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        SetupNoDuplicatesForUpdate(1);

        var request = new UpdateUserRequest { Name = "X", Username = "x", Email = "x@x.com", Phone = "1234567890", Role = "Admin" };
        await _sut.UpdateUserAsync(1, request, callerId: 7);

        user.UpdatedBy.Should().Be(7);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // DeleteUserAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_CallsRepoDeleteAndSave()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        await _sut.DeleteUserAsync(1);

        _repoMock.Verify(r => r.DeleteUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => _sut.DeleteUserAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────────
    // ChangePasswordAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_UpdatesHash()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Test@1234",
            NewPassword = "NewStr0ng@Pass"
        };

        await _sut.ChangePasswordAsync(1, request, callerId: 1);

        BCrypt.Net.BCrypt.Verify("NewStr0ng@Pass", user.PasswordHash).Should().BeTrue();
        _repoMock.Verify(r => r.UpdateUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsValidationException()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewStr0ng@Pass"
        };

        var act = () => _sut.ChangePasswordAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Fields.Should().ContainKey("CurrentPassword");
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var request = new ChangePasswordRequest { CurrentPassword = "x", NewPassword = "y" };
        var act = () => _sut.ChangePasswordAsync(99, request, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_SetsAuditFields()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var request = new ChangePasswordRequest { CurrentPassword = "Test@1234", NewPassword = "NewStr0ng@Pass" };
        await _sut.ChangePasswordAsync(1, request, callerId: 3);

        user.UpdatedBy.Should().Be(3);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // ResetPasswordAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ExistingUser_UpdatesHash()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var request = new ResetPasswordRequest { NewPassword = "Reset@Pass1" };
        await _sut.ResetPasswordAsync(1, request, callerId: 1);

        BCrypt.Net.BCrypt.Verify("Reset@Pass1", user.PasswordHash).Should().BeTrue();
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var request = new ResetPasswordRequest { NewPassword = "Reset@Pass1" };
        var act = () => _sut.ResetPasswordAsync(99, request, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_SetsAuditFields()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var request = new ResetPasswordRequest { NewPassword = "Reset@Pass1" };
        await _sut.ResetPasswordAsync(1, request, callerId: 4);

        user.UpdatedBy.Should().Be(4);
    }

    // ─────────────────────────────────────────────────
    // DeactivateUserAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUserAsync_ExistingUser_SetsIsActiveFalse()
    {
        var user = CreateFakeUser();
        user.IsActive = true;
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        await _sut.DeactivateUserAsync(1, callerId: 1);

        user.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => _sut.DeactivateUserAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsAuditFields()
    {
        var user = CreateFakeUser();
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        await _sut.DeactivateUserAsync(1, callerId: 6);

        user.UpdatedBy.Should().Be(6);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // ActivateUserAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateUserAsync_ExistingUser_SetsIsActiveTrue()
    {
        var user = CreateFakeUser();
        user.IsActive = false;
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        await _sut.ActivateUserAsync(1, callerId: 1);

        user.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => _sut.ActivateUserAsync(99, callerId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ActivateUserAsync_SetsAuditFields()
    {
        var user = CreateFakeUser();
        user.IsActive = false;
        _repoMock.Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        await _sut.ActivateUserAsync(1, callerId: 8);

        user.UpdatedBy.Should().Be(8);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupNoDuplicates()
    {
        _repoMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }

    private void SetupNoDuplicatesForUpdate(int excludeUserId)
    {
        _repoMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), excludeUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), excludeUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), excludeUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
    }
}
