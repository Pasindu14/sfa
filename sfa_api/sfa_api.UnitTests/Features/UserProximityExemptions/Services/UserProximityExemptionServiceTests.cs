using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.UserProximityExemptions.Repositories;
using sfa_api.Features.UserProximityExemptions.Requests;
using sfa_api.Features.UserProximityExemptions.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.UnitTests.Features.UserProximityExemptions.Services;

public class UserProximityExemptionServiceTests
{
    private readonly Mock<IUserProximityExemptionRepository> _repoMock = new();
    private readonly UserProximityExemptionService _sut;

    private const int RepId = 42;
    private const int AdminId = 1;

    public UserProximityExemptionServiceTests()
    {
        _sut = new UserProximityExemptionService(
            _repoMock.Object,
            NullLogger<UserProximityExemptionService>.Instance);
    }

    private static User Rep(bool isActive = true, UserRole role = UserRole.SalesRep) => new()
    {
        Id = RepId, Name = "Test Rep", Username = "testrep", Role = role, IsActive = isActive
    };

    private static GrantProximityExemptionRequest Request(DateOnly? until = null) => new()
    {
        ValidUntil = until ?? SriLankaTime.Today.AddDays(3),
        Reason = nameof(ProximityExemptionReason.BadOutletCoordinates),
        Notes = "  Market outlets share one pin  "
    };

    private void SetupRep(User? user) =>
        _repoMock.Setup(r => r.GetUserAsync(RepId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

    private void SetupNoLiveGrant() =>
        _repoMock.Setup(r => r.GetEffectiveAsync(RepId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserProximityExemption?)null);

    // ─────────────────────────────────────────────────
    // Grant
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GrantAsync_StoresTheColomboMidnightAfterTheChosenDay()
    {
        // The admin picks an inclusive business date. If ValidTo were stored as
        // UTC midnight of that date, the grant would die at 05:30 on its final
        // morning — hours before the rep finishes the round it was granted for.
        var until = new DateOnly(2026, 9, 20);
        SetupRep(Rep());
        SetupNoLiveGrant();
        UserProximityExemption? saved = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<UserProximityExemption>(), It.IsAny<CancellationToken>()))
                 .Callback<UserProximityExemption, CancellationToken>((e, _) => saved = e)
                 .Returns(Task.CompletedTask);

        await _sut.GrantAsync(RepId, Request(until), AdminId);

        saved.Should().NotBeNull();
        saved!.ValidTo.Should().Be(SriLankaTime.StartOfDayUtc(until.AddDays(1)));
        // 2026-09-21 00:00 Colombo == 2026-09-20 18:30 UTC
        saved.ValidTo.Should().Be(new DateTime(2026, 9, 20, 18, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GrantAsync_RecordsReasonGranterAndTrimmedNotes()
    {
        SetupRep(Rep());
        SetupNoLiveGrant();
        UserProximityExemption? saved = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<UserProximityExemption>(), It.IsAny<CancellationToken>()))
                 .Callback<UserProximityExemption, CancellationToken>((e, _) => saved = e)
                 .Returns(Task.CompletedTask);

        await _sut.GrantAsync(RepId, Request(), AdminId);

        saved!.Reason.Should().Be(ProximityExemptionReason.BadOutletCoordinates);
        saved.GrantedByUserId.Should().Be(AdminId);
        saved.CreatedBy.Should().Be(AdminId);
        saved.IsActive.Should().BeTrue();
        saved.Notes.Should().Be("Market outlets share one pin");
    }

    [Fact]
    public async Task GrantAsync_BlankNotes_StoredAsNull()
    {
        SetupRep(Rep());
        SetupNoLiveGrant();
        UserProximityExemption? saved = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<UserProximityExemption>(), It.IsAny<CancellationToken>()))
                 .Callback<UserProximityExemption, CancellationToken>((e, _) => saved = e)
                 .Returns(Task.CompletedTask);

        var request = Request();
        request.Notes = "   ";

        await _sut.GrantAsync(RepId, request, AdminId);

        saved!.Notes.Should().BeNull();
    }

    [Fact]
    public async Task GrantAsync_ExistingLiveGrant_IsRevokedAndSuperseded()
    {
        // Re-granting means "make it this instead". The old row must be closed out
        // rather than left live, or two overlapping grants would both resolve and
        // the later expiry would silently win.
        var existing = new UserProximityExemption
        {
            Id = 9, UserId = RepId, IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5)
        };
        SetupRep(Rep());
        _repoMock.Setup(r => r.GetEffectiveAsync(RepId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        await _sut.GrantAsync(RepId, Request(), AdminId);

        existing.IsActive.Should().BeFalse();
        existing.RevokedAt.Should().NotBeNull();
        existing.RevokedByUserId.Should().Be(AdminId);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<UserProximityExemption>(), It.IsAny<CancellationToken>()), Times.Once);
        // One SaveChanges — the supersede and the new grant land together or not at all.
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_UnknownUser_Throws()
    {
        SetupRep(null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GrantAsync(RepId, Request(), AdminId));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Distributor)]
    public async Task GrantAsync_NonSalesRep_Rejected(UserRole role)
    {
        // The geofence only runs on rep bill submission, so granting elsewhere
        // would create a control that looks real and does nothing.
        SetupRep(Rep(role: role));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GrantAsync(RepId, Request(), AdminId));
        ex.ErrorCode.Should().Be("PROXIMITY_EXEMPTION_ROLE_INVALID");
    }

    [Fact]
    public async Task GrantAsync_DeactivatedUser_Rejected()
    {
        SetupRep(Rep(isActive: false));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GrantAsync(RepId, Request(), AdminId));
        ex.ErrorCode.Should().Be("PROXIMITY_EXEMPTION_USER_INACTIVE");
    }

    // ─────────────────────────────────────────────────
    // Revoke
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_ClosesTheGrantAndPinsTheConcurrencyToken()
    {
        var entity = new UserProximityExemption
        {
            Id = 9, UserId = RepId, IsActive = true, RowVersion = 55,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            User = Rep()
        };
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.RevokeAsync(9, new RevokeProximityExemptionRequest { RowVersion = 55 }, AdminId);

        entity.IsActive.Should().BeFalse();
        entity.RevokedByUserId.Should().Be(AdminId);
        result.IsActive.Should().BeFalse();
        result.IsCurrentlyEffective.Should().BeFalse();
        // Without the token a second admin's concurrent revoke would be overwritten.
        _repoMock.Verify(r => r.ApplyConcurrencyToken(entity, 55), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevoked_Rejected()
    {
        var entity = new UserProximityExemption { Id = 9, UserId = RepId, IsActive = false };
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.RevokeAsync(9, new RevokeProximityExemptionRequest { RowVersion = 1 }, AdminId));
        ex.ErrorCode.Should().Be("PROXIMITY_EXEMPTION_ALREADY_REVOKED");
    }

    [Fact]
    public async Task RevokeAsync_UnknownExemption_Throws()
    {
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserProximityExemption?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.RevokeAsync(9, new RevokeProximityExemptionRequest { RowVersion = 1 }, AdminId));
    }

    // ─────────────────────────────────────────────────
    // Projection
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentAsync_EchoesTheInclusiveBusinessDateTheAdminPicked()
    {
        // The UI shows "applies through" and must not render the exclusive
        // midnight boundary, which reads as a day later than what was granted.
        var until = new DateOnly(2026, 9, 20);
        var entity = new UserProximityExemption
        {
            Id = 9, UserId = RepId, IsActive = true,
            ValidFrom = new DateTime(2026, 9, 16, 6, 0, 0, DateTimeKind.Utc),
            ValidTo = SriLankaTime.StartOfDayUtc(until.AddDays(1)),
            Reason = ProximityExemptionReason.DeviceGpsFault,
            User = Rep()
        };
        SetupRep(Rep());
        _repoMock.Setup(r => r.GetEffectiveAsync(RepId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var dto = await _sut.GetCurrentAsync(RepId);

        dto.Should().NotBeNull();
        dto!.ValidUntilDate.Should().Be(until);
        dto.Reason.Should().Be(nameof(ProximityExemptionReason.DeviceGpsFault));
    }

    [Fact]
    public async Task GetCurrentAsync_NoLiveGrant_ReturnsNull()
    {
        SetupRep(Rep());
        SetupNoLiveGrant();

        (await _sut.GetCurrentAsync(RepId)).Should().BeNull();
    }
}
