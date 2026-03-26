using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.UserReportingLines.DTOs;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.UserReportingLines.Requests;
using sfa_api.Features.UserReportingLines.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.UnitTests.Features.UserReportingLines.Services;

public class UserReportingLineServiceTests
{
    private readonly Mock<IUserReportingLineRepository> _repoMock;
    private readonly UserReportingLineService _sut;

    public UserReportingLineServiceTests()
    {
        _repoMock = new Mock<IUserReportingLineRepository>();
        _sut = new UserReportingLineService(_repoMock.Object, NullLogger<UserReportingLineService>.Instance);
    }

    private static User CreateFakeUser(int id, UserRole role = UserRole.SalesRep) => new()
    {
        Id = id,
        Name = $"User {id}",
        Username = $"user{id}",
        Email = $"user{id}@test.com",
        Phone = $"+9411{id:D7}",
        Role = role,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static UserReportingLine CreateFakeLine(
        int id = 1,
        int userId = 10,
        int managerId = 20,
        bool isActive = true) => new()
    {
        Id = id,
        UserId = userId,
        ReportsToUserId = managerId,
        EffectiveFrom = new DateOnly(2026, 1, 1),
        IsActive = isActive,
        User = CreateFakeUser(userId),
        ReportsToUser = CreateFakeUser(managerId),
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static CreateUserReportingLineRequest CreateValidRequest(int userId = 10, int managerId = 20) => new()
    {
        UserId = userId,
        ReportsToUserId = managerId,
        EffectiveFrom = new DateOnly(2026, 3, 26)
    };

    private static UpdateUserReportingLineRequest CreateValidUpdateRequest(int managerId = 20) => new()
    {
        ReportsToUserId = managerId,
        EffectiveFrom = new DateOnly(2026, 4, 1)
    };

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingLine_ReturnsMappedDto()
    {
        var line = CreateFakeLine();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(line.Id);
        result.UserId.Should().Be(line.UserId);
        result.UserName.Should().Be(line.User!.Name);
        result.UserRole.Should().Be(line.User.Role.ToString());
        result.ReportsToUserId.Should().Be(line.ReportsToUserId);
        result.ReportsToUserName.Should().Be(line.ReportsToUser!.Name);
        result.EffectiveFrom.Should().Be(line.EffectiveFrom);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentLine_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserReportingLine?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERREPORTINGLINE_NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedListDto()
    {
        var lines = new[] { CreateFakeLine(1), CreateFakeLine(2, userId: 11, managerId: 21) };
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((lines.AsEnumerable(), 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.UserReportingLines.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Page2_CalculatesCorrectSkip()
    {
        _repoMock.Setup(r => r.GetAllAsync(10, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<UserReportingLine>(), 0));

        await _sut.GetAllAsync(2, 10);

        _repoMock.Verify(r => r.GetAllAsync(10, 10, null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(0, 10, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<UserReportingLine>(), 0));

        var result = await _sut.GetAllAsync(1, 10);

        result.UserReportingLines.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────
    // GetSubordinatesAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSubordinatesAsync_UserNotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.UserExistsAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.GetSubordinatesAsync(99, directOnly: true);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetSubordinatesAsync_DirectOnly_ReturnsOnlyDirectReports()
    {
        _repoMock.Setup(r => r.UserExistsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.GetDirectReportsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { CreateFakeLine(1, userId: 10, managerId: 20) });

        var result = (await _sut.GetSubordinatesAsync(20, directOnly: true)).ToList();

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(10);
    }

    [Fact]
    public async Task GetSubordinatesAsync_FullSubtree_TraversesBfsLevels()
    {
        // Manager 20 → User 10 → User 5 (two levels)
        _repoMock.Setup(r => r.UserExistsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.GetDirectReportsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { CreateFakeLine(1, userId: 10, managerId: 20) });
        _repoMock.Setup(r => r.GetDirectReportsAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new[] { CreateFakeLine(2, userId: 5, managerId: 10) });
        _repoMock.Setup(r => r.GetDirectReportsAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());

        var result = (await _sut.GetSubordinatesAsync(20, directOnly: false)).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.UserId).Should().BeEquivalentTo(new[] { 10, 5 });
    }

    [Fact]
    public async Task GetSubordinatesAsync_NoDirectReports_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.UserExistsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.GetDirectReportsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enumerable.Empty<UserReportingLine>());

        var result = await _sut.GetSubordinatesAsync(20, directOnly: false);

        result.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_UserNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidRequest(userId: 10, managerId: 20);
        _repoMock.Setup(r => r.UserExistsAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_ManagerNotFound_ThrowsNotFoundException()
    {
        var request = CreateValidRequest(userId: 10, managerId: 20);
        _repoMock.Setup(r => r.UserExistsAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.UserExistsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_AdminUserAsSubordinate_ThrowsBusinessRuleException()
    {
        var request = CreateValidRequest(userId: 10, managerId: 20);
        _repoMock.Setup(r => r.UserExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.IsAdminOrDistributorAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, callerId: 1);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("USER_ROLE_NOT_ASSIGNABLE");
    }

    [Fact]
    public async Task CreateAsync_WithExistingActiveLine_DeactivatesOldLine()
    {
        var request = CreateValidRequest();
        var existingLine = CreateFakeLine(id: 5, userId: 10, managerId: 99);
        SetupSuccessfulCreate(request, existingLine);

        UserReportingLine? deactivatedLine = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserReportingLine>(), It.IsAny<CancellationToken>()))
                 .Callback<UserReportingLine, CancellationToken>((l, _) => deactivatedLine = l)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        deactivatedLine.Should().NotBeNull();
        deactivatedLine!.Id.Should().Be(5);
        deactivatedLine.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsAuditFields()
    {
        var request = CreateValidRequest();
        SetupSuccessfulCreate(request, existingLine: null);
        UserReportingLine? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserReportingLine>(), It.IsAny<CancellationToken>()))
                 .Callback<UserReportingLine, CancellationToken>((l, _) => captured = l)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 7);

        captured!.CreatedBy.Should().Be(7);
        captured.UpdatedBy.Should().Be(7);
        captured.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        captured.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsIsActiveTrue()
    {
        var request = CreateValidRequest();
        SetupSuccessfulCreate(request, existingLine: null);
        UserReportingLine? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserReportingLine>(), It.IsAny<CancellationToken>()))
                 .Callback<UserReportingLine, CancellationToken>((l, _) => captured = l)
                 .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, callerId: 1);

        captured!.IsActive.Should().BeTrue();
        captured.UserId.Should().Be(request.UserId);
        captured.ReportsToUserId.Should().Be(request.ReportsToUserId);
        captured.EffectiveFrom.Should().Be(request.EffectiveFrom);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsSaveChangesOnce()
    {
        var request = CreateValidRequest();
        SetupSuccessfulCreate(request, existingLine: null);

        await _sut.CreateAsync(request, callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistentLine_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserReportingLine?)null);

        var act = () => _sut.UpdateAsync(99, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERREPORTINGLINE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_ManagerNotFound_ThrowsNotFoundException()
    {
        var line = CreateFakeLine();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);
        _repoMock.Setup(r => r.UserExistsAsync(20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var act = () => _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_SelfReport_ThrowsBusinessRuleException()
    {
        // userId == managerId
        var line = CreateFakeLine(userId: 10, managerId: 20);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);
        _repoMock.Setup(r => r.UserExistsAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new UpdateUserReportingLineRequest { ReportsToUserId = 10, EffectiveFrom = new DateOnly(2026, 4, 1) };
        var act = () => _sut.UpdateAsync(1, request, callerId: 1);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("SELF_REPORTING_NOT_ALLOWED");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_SetsAuditFields()
    {
        var line = CreateFakeLine();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);
        SetupSuccessfulUpdate(line, CreateValidUpdateRequest(), lineId: 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 9);

        line.UpdatedBy.Should().Be(9);
        line.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_CallsSaveChangesOnce()
    {
        var line = CreateFakeLine();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);
        SetupSuccessfulUpdate(line, CreateValidUpdateRequest(), lineId: 1);

        await _sut.UpdateAsync(1, CreateValidUpdateRequest(), callerId: 1);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingLine_SetsIsActiveFalse()
    {
        var line = CreateFakeLine(isActive: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);

        await _sut.DeleteAsync(1, callerId: 1);

        line.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(line, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentLine_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserReportingLine?)null);

        var act = () => _sut.DeleteAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERREPORTINGLINE_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_SetsAuditFields()
    {
        var line = CreateFakeLine();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);

        await _sut.DeleteAsync(1, callerId: 4);

        line.UpdatedBy.Should().Be(4);
        line.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // ActivateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_ExistingLine_SetsIsActiveTrue()
    {
        var line = CreateFakeLine(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);

        await _sut.ActivateAsync(1, callerId: 1);

        line.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(line, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_NonExistentLine_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserReportingLine?)null);

        var act = () => _sut.ActivateAsync(99, callerId: 1);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("USERREPORTINGLINE_NOT_FOUND");
    }

    [Fact]
    public async Task ActivateAsync_SetsAuditFields()
    {
        var line = CreateFakeLine(isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);

        await _sut.ActivateAsync(1, callerId: 3);

        line.UpdatedBy.Should().Be(3);
        line.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private void SetupSuccessfulCreate(CreateUserReportingLineRequest request, UserReportingLine? existingLine)
    {
        _repoMock.Setup(r => r.UserExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.IsAdminOrDistributorAsync(request.UserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.GetActiveByUserIdAsync(request.UserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingLine);
        if (existingLine is not null)
            _repoMock.Setup(r => r.UpdateAsync(existingLine, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<UserReportingLine>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeLine(userId: request.UserId, managerId: request.ReportsToUserId));
    }

    private void SetupSuccessfulUpdate(UserReportingLine line, UpdateUserReportingLineRequest request, int lineId)
    {
        _repoMock.Setup(r => r.UserExistsAsync(request.ReportsToUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateAsync(line, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(lineId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(line);
    }
}
