using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.DailyRouteAssignments.Repositories;
using sfa_api.Features.RouteUnlockRequests.Entities;
using sfa_api.Features.RouteUnlockRequests.Options;
using sfa_api.Features.RouteUnlockRequests.Repositories;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.RouteUnlockRequests.Services;
using sfa_api.Features.Supervisor.Services;
using sfa_api.Features.UserProximityExemptions.DTOs;
using sfa_api.Features.UserProximityExemptions.Services;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Notifications;

namespace sfa_api.UnitTests.Features.RouteUnlockRequests.Services;

public class RouteUnlockRequestServiceTests
{
    private const int RepId = 10;
    private const int SupervisorId = 20;
    private const int AdminId = 1;
    private const int RouteId = 3;
    private const int AssignmentId = 99;

    private readonly Mock<IRouteUnlockRequestRepository> _repo = new();
    private readonly Mock<IDailyRouteAssignmentRepository> _assignments = new();
    private readonly Mock<IUserReportingLineRepository> _lines = new();
    private readonly Mock<ISupervisorService> _supervisor = new();
    private readonly Mock<IProximityPolicyResolver> _resolver = new();
    private readonly Mock<IDistributedLockService> _locks = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IHttpContextAccessor> _http = new();

    private RouteUnlockRequest? _stored;

    public RouteUnlockRequestServiceTests()
    {
        _locks.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Mock<IAsyncDisposable>().Object);

        _assignments.Setup(a => a.GetActiveTodayAssignmentForRepAsync(RepId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyRouteAssignment { Id = AssignmentId, UserId = RepId, RouteId = RouteId });

        _resolver.Setup(r => r.ResolveAsync(RepId, RouteId, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Enforced());

        _lines.Setup(l => l.GetActiveByUserIdAsync(RepId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new UserReportingLine { UserId = RepId, ReportsToUserId = SupervisorId });

        _repo.Setup(r => r.AddAsync(It.IsAny<RouteUnlockRequest>(), It.IsAny<CancellationToken>()))
             .Callback<RouteUnlockRequest, CancellationToken>((e, _) => { e.Id = 500; _stored = e; })
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.GetWithNamesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(() => _stored);
        _repo.Setup(r => r.GetForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(() => _stored);
    }

    private RouteUnlockRequestService Build(int maxPerDay = 3)
        => new(_repo.Object, _assignments.Object, _lines.Object, _supervisor.Object, _resolver.Object,
            _locks.Object, _notifications.Object, _http.Object,
            Options.Create(new RouteUnlockOptions { MaxRequestsPerDay = maxPerDay }),
            NullLogger<RouteUnlockRequestService>.Instance);

    private static ProximityPolicy Enforced()
        => new(true, 1000, 200, null, null, null);

    private static CreateRouteUnlockRequest CreateReq(string reason = "  GPS not accurate  ")
        => new() { Reason = reason, Latitude = 7.29, Longitude = 80.63, GpsAccuracyMeters = 12.5 };

    /// Seeds the row the repo hands back for transitions.
    private RouteUnlockRequest Stored(
        RouteUnlockStatus status = RouteUnlockStatus.Pending,
        DateOnly? businessDate = null,
        int? supervisorId = SupervisorId,
        DateTime? validTo = null)
    {
        _stored = new RouteUnlockRequest
        {
            Id = 500,
            UserId = RepId,
            RouteId = RouteId,
            DailyRouteAssignmentId = AssignmentId,
            BusinessDate = businessDate ?? SriLankaTime.Today,
            Status = status,
            SupervisorUserId = supervisorId,
            ValidFrom = validTo?.AddHours(-3),
            ValidTo = validTo,
            RowVersion = 5
        };
        return _stored;
    }

    private void VerifyNotified(int userId, string type, Times times)
        => _notifications.Verify(n => n.SendToUserAsync(
            userId, It.IsAny<string>(), It.IsAny<string>(),
            It.Is<Dictionary<string, string>>(d => d["type"] == type),
            It.IsAny<CancellationToken>()), times);

    private void VerifyNoNotificationAtAll()
        => _notifications.Verify(n => n.SendToUserAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Never);

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NoAssignmentToday_ThrowsNoAssignment()
    {
        _assignments.Setup(a => a.GetActiveTodayAssignmentForRepAsync(RepId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DailyRouteAssignment?)null);

        var ex = await Build().Invoking(s => s.CreateAsync(CreateReq(), RepId))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_NO_ASSIGNMENT");
        _repo.Verify(r => r.AddAsync(It.IsAny<RouteUnlockRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AlreadyExempt_ThrowsAlreadyExempt()
    {
        _resolver.Setup(r => r.ResolveAsync(RepId, RouteId, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ProximityPolicy(false, 1000, 200, null, 7, null, ProximityPolicySource.AdminExemption));

        var ex = await Build().Invoking(s => s.CreateAsync(CreateReq(), RepId))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_ALREADY_EXEMPT");
    }

    [Fact]
    public async Task CreateAsync_OpenRequestExists_ThrowsAlreadyOpenConflict()
    {
        _repo.Setup(r => r.HasOpenForRepOnDateAsync(RepId, SriLankaTime.Today, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var ex = await Build().Invoking(s => s.CreateAsync(CreateReq(), RepId))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_ALREADY_OPEN");
    }

    [Fact]
    public async Task CreateAsync_DailyCapReached_ThrowsDailyLimit()
    {
        _repo.Setup(r => r.CountForRepOnDateAsync(RepId, SriLankaTime.Today, It.IsAny<CancellationToken>()))
             .ReturnsAsync(3);

        var ex = await Build(maxPerDay: 3).Invoking(s => s.CreateAsync(CreateReq(), RepId))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_DAILY_LIMIT");
    }

    [Fact]
    public async Task CreateAsync_OneBelowCap_Succeeds()
    {
        _repo.Setup(r => r.CountForRepOnDateAsync(RepId, SriLankaTime.Today, It.IsAny<CancellationToken>()))
             .ReturnsAsync(2);

        var dto = await Build(maxPerDay: 3).CreateAsync(CreateReq(), RepId);

        dto.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateAsync_HappyPath_SnapshotsSupervisorWritesEventAndNotifies()
    {
        var dto = await Build().CreateAsync(CreateReq(), RepId);

        dto.Status.Should().Be("Pending");
        dto.EffectiveStatus.Should().Be("Pending");
        dto.RouteId.Should().Be(RouteId);
        dto.RequestReason.Should().Be("GPS not accurate");
        dto.SupervisorUserId.Should().Be(SupervisorId);

        _stored!.DailyRouteAssignmentId.Should().Be(AssignmentId);
        _stored.BusinessDate.Should().Be(SriLankaTime.Today);
        _stored.RequestLatitude.Should().Be(7.29);
        _stored.RequestGpsAccuracyMeters.Should().Be(12.5);
        _stored.Events.Should().ContainSingle();
        var ev = _stored.Events[0];
        ev.Action.Should().Be(RouteUnlockAction.Requested);
        ev.FromStatus.Should().BeNull();
        ev.ToStatus.Should().Be(RouteUnlockStatus.Pending);
        ev.PerformedByUserId.Should().Be(RepId);
        ev.PerformedByRole.Should().Be("SalesRep");

        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotified(SupervisorId, "ROUTE_UNLOCK_REQUESTED", Times.Once());
    }

    [Fact]
    public async Task CreateAsync_NoSupervisor_SavesWithNullSupervisorAndSendsNoNotification()
    {
        _lines.Setup(l => l.GetActiveByUserIdAsync(RepId, It.IsAny<CancellationToken>()))
              .ReturnsAsync((UserReportingLine?)null);

        var dto = await Build().CreateAsync(CreateReq(), RepId);

        dto.SupervisorUserId.Should().BeNull();
        VerifyNoNotificationAtAll();
    }

    [Fact]
    public async Task CreateAsync_LockBusy_ThrowsBusy()
    {
        _locks.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((IAsyncDisposable?)null);

        var ex = await Build().Invoking(s => s.CreateAsync(CreateReq(), RepId))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_BUSY");
    }

    // ── Approve ──────────────────────────────────────────────────────────

    private static ApproveRouteUnlockRequest ApproveReq(string? note = null)
        => new() { RowVersion = 5, Note = note };

    private static ReasonedRouteUnlockRequest ReasonReq(string reason = "no reason")
        => new() { RowVersion = 5, Reason = reason };

    [Fact]
    public async Task ApproveAsync_SupervisorNotOverRep_ThrowsAuthorization()
    {
        Stored();
        _supervisor.Setup(s => s.EnsureRepUnderSupervisorAsync(SupervisorId, RepId, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new AuthorizationException("this rep"));

        await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), SupervisorId, UserRole.Supervisor))
            .Should().ThrowAsync<AuthorizationException>();

        _stored!.Status.Should().Be(RouteUnlockStatus.Pending);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UserRole.SalesRep)]
    [InlineData(UserRole.ASM)]
    [InlineData(UserRole.Distributor)]
    public async Task ApproveAsync_RoleThatCannotReview_ThrowsAuthorization(UserRole role)
    {
        Stored();

        await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), 77, role))
            .Should().ThrowAsync<AuthorizationException>();
    }

    [Theory]
    [InlineData(RouteUnlockStatus.Approved)]
    [InlineData(RouteUnlockStatus.Rejected)]
    [InlineData(RouteUnlockStatus.Cancelled)]
    [InlineData(RouteUnlockStatus.Revoked)]
    public async Task ApproveAsync_NotPending_ThrowsInvalidState(RouteUnlockStatus status)
    {
        Stored(status, validTo: DateTime.UtcNow.AddHours(2));

        var ex = await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    [Fact]
    public async Task ApproveAsync_RequestFromPastDay_ThrowsExpired()
    {
        Stored(businessDate: SriLankaTime.Today.AddDays(-1));

        var ex = await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_EXPIRED");
    }

    [Fact]
    public async Task ApproveAsync_RepMovedToAnotherRoute_ThrowsAssignmentChanged()
    {
        Stored();
        _assignments.Setup(a => a.GetActiveTodayAssignmentForRepAsync(RepId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyRouteAssignment { Id = 100, UserId = RepId, RouteId = RouteId + 1 });

        var ex = await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_ASSIGNMENT_CHANGED");
    }

    [Fact]
    public async Task ApproveAsync_AssignmentRemoved_ThrowsAssignmentChanged()
    {
        Stored();
        _assignments.Setup(a => a.GetActiveTodayAssignmentForRepAsync(RepId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DailyRouteAssignment?)null);

        var ex = await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_ASSIGNMENT_CHANGED");
    }

    [Fact]
    public async Task ApproveAsync_NotFound_Throws()
    {
        _stored = null;

        await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ApproveAsync_SupervisorHappyPath_SetsWindowReviewerEventAndNotifiesRepOnly()
    {
        Stored();
        var before = DateTime.UtcNow;

        var dto = await Build().ApproveAsync(500, ApproveReq("  ok  "), SupervisorId, UserRole.Supervisor);

        dto.Status.Should().Be("Approved");
        _stored!.ValidTo.Should().Be(SriLankaTime.StartOfDayUtc(SriLankaTime.Today.AddDays(1)));
        _stored.ValidFrom.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        _stored.ReviewedByUserId.Should().Be(SupervisorId);
        _stored.ReviewedByRole.Should().Be("Supervisor");
        _stored.ReviewNote.Should().Be("ok");
        _stored.Events.Should().ContainSingle(e =>
            e.Action == RouteUnlockAction.Approved
            && e.FromStatus == RouteUnlockStatus.Pending
            && e.ToStatus == RouteUnlockStatus.Approved
            && e.PerformedByUserId == SupervisorId
            && e.PerformedByRole == "Supervisor");

        _repo.Verify(r => r.ApplyConcurrencyToken(_stored, 5u), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotified(RepId, "ROUTE_UNLOCK_APPROVED", Times.Once());
        // The supervisor acted themselves, so they are not pinged about it.
        VerifyNotified(SupervisorId, "ROUTE_UNLOCK_APPROVED", Times.Never());
    }

    [Fact]
    public async Task ApproveAsync_AdminActing_AlsoNotifiesTheSupervisor()
    {
        Stored();

        await Build().ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin);

        _stored!.ReviewedByRole.Should().Be("Admin");
        _supervisor.Verify(s => s.EnsureRepUnderSupervisorAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNotified(RepId, "ROUTE_UNLOCK_APPROVED", Times.Once());
        VerifyNotified(SupervisorId, "ROUTE_UNLOCK_APPROVED", Times.Once());
    }

    [Fact]
    public async Task ApproveAsync_AdminActingOnRepWithoutSupervisor_NotifiesOnlyRep()
    {
        Stored(supervisorId: null);

        await Build().ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin);

        _notifications.Verify(n => n.SendToUserAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotified(RepId, "ROUTE_UNLOCK_APPROVED", Times.Once());
    }

    [Fact]
    public async Task ApproveAsync_LockBusy_ThrowsBusy()
    {
        Stored();
        _locks.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((IAsyncDisposable?)null);

        var ex = await Build().Invoking(s => s.ApproveAsync(500, ApproveReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_BUSY");
    }

    // ── Reject ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_Pending_RecordsReasonEventAndNotifiesRep()
    {
        Stored();

        var dto = await Build().RejectAsync(500, ReasonReq(" Not needed "), SupervisorId, UserRole.Supervisor);

        dto.Status.Should().Be("Rejected");
        _stored!.ReviewNote.Should().Be("Not needed");
        _stored.ReviewedByUserId.Should().Be(SupervisorId);
        _stored.ValidTo.Should().BeNull();
        _stored.Events.Should().ContainSingle(e =>
            e.Action == RouteUnlockAction.Rejected && e.Note == "Not needed"
            && e.FromStatus == RouteUnlockStatus.Pending && e.ToStatus == RouteUnlockStatus.Rejected);
        VerifyNotified(RepId, "ROUTE_UNLOCK_REJECTED", Times.Once());
    }

    [Fact]
    public async Task RejectAsync_AlreadyApproved_ThrowsInvalidState()
    {
        Stored(RouteUnlockStatus.Approved, validTo: DateTime.UtcNow.AddHours(3));

        var ex = await Build().Invoking(s => s.RejectAsync(500, ReasonReq(), AdminId, UserRole.Admin))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    [Fact]
    public async Task RejectAsync_SupervisorNotOverRep_ThrowsAuthorization()
    {
        Stored();
        _supervisor.Setup(s => s.EnsureRepUnderSupervisorAsync(SupervisorId, RepId, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new AuthorizationException("this rep"));

        await Build().Invoking(s => s.RejectAsync(500, ReasonReq(), SupervisorId, UserRole.Supervisor))
            .Should().ThrowAsync<AuthorizationException>();
    }

    // ── Revoke ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_LiveApproved_RecordsReasonEventAndNotifiesRep()
    {
        Stored(RouteUnlockStatus.Approved, validTo: DateTime.UtcNow.AddHours(4));

        var dto = await Build().RevokeAsync(500, ReasonReq(" misuse "), SupervisorId, UserRole.Supervisor);

        dto.Status.Should().Be("Revoked");
        _stored!.RevokedByUserId.Should().Be(SupervisorId);
        _stored.RevokeReason.Should().Be("misuse");
        _stored.RevokedAt.Should().NotBeNull();
        _stored.Events.Should().ContainSingle(e =>
            e.Action == RouteUnlockAction.Revoked
            && e.FromStatus == RouteUnlockStatus.Approved && e.ToStatus == RouteUnlockStatus.Revoked);
        VerifyNotified(RepId, "ROUTE_UNLOCK_REVOKED", Times.Once());
    }

    [Fact]
    public async Task RevokeAsync_ExpiredApproved_ThrowsInvalidState()
    {
        Stored(RouteUnlockStatus.Approved, validTo: DateTime.UtcNow.AddMinutes(-1));

        var ex = await Build().Invoking(s => s.RevokeAsync(500, ReasonReq("late"), AdminId, UserRole.Admin))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RouteUnlockStatus.Pending)]
    [InlineData(RouteUnlockStatus.Rejected)]
    [InlineData(RouteUnlockStatus.Cancelled)]
    [InlineData(RouteUnlockStatus.Revoked)]
    public async Task RevokeAsync_NotApproved_ThrowsInvalidState(RouteUnlockStatus status)
    {
        Stored(status);

        var ex = await Build().Invoking(s => s.RevokeAsync(500, ReasonReq("x1x"), AdminId, UserRole.Admin))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    // ── Cancel ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_OwnPending_CancelsWritesEventAndNotifiesSupervisor()
    {
        Stored();

        var dto = await Build().CancelAsync(500, new CancelRouteUnlockRequest { RowVersion = 5 }, RepId);

        dto.Status.Should().Be("Cancelled");
        _stored!.CancelledAt.Should().NotBeNull();
        _stored.Events.Should().ContainSingle(e =>
            e.Action == RouteUnlockAction.Cancelled && e.PerformedByRole == "SalesRep");
        VerifyNotified(SupervisorId, "ROUTE_UNLOCK_CANCELLED", Times.Once());
    }

    [Fact]
    public async Task CancelAsync_OtherRepsRequest_ThrowsAuthorization()
    {
        Stored();

        await Build().Invoking(s => s.CancelAsync(500, new CancelRouteUnlockRequest { RowVersion = 5 }, RepId + 1))
            .Should().ThrowAsync<AuthorizationException>();

        _stored!.Status.Should().Be(RouteUnlockStatus.Pending);
    }

    [Theory]
    [InlineData(RouteUnlockStatus.Approved)]
    [InlineData(RouteUnlockStatus.Rejected)]
    [InlineData(RouteUnlockStatus.Cancelled)]
    [InlineData(RouteUnlockStatus.Revoked)]
    public async Task CancelAsync_NotPending_ThrowsInvalidState(RouteUnlockStatus status)
    {
        Stored(status, validTo: DateTime.UtcNow.AddHours(2));

        var ex = await Build().Invoking(s => s.CancelAsync(500, new CancelRouteUnlockRequest { RowVersion = 5 }, RepId))
            .Should().ThrowAsync<RouteUnlockConflictException>();

        ex.Which.ErrorCode.Should().Be("ROUTE_UNLOCK_INVALID_STATE");
    }

    // ── Scope ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingCountAsync_Supervisor_ScopesToDirectReports()
    {
        _lines.Setup(l => l.GetDirectReportsAsync(SupervisorId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[] { new UserReportingLine { UserId = RepId }, new UserReportingLine { UserId = 11 } });
        _repo.Setup(r => r.CountPendingAsync(
                 It.Is<IReadOnlyCollection<int>?>(s => s != null && s.SequenceEqual(new[] { RepId, 11 })),
                 SriLankaTime.Today, It.IsAny<CancellationToken>()))
             .ReturnsAsync(2);

        var result = await Build().GetPendingCountAsync(SupervisorId, UserRole.Supervisor);

        result.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetPendingCountAsync_Admin_IsUnscoped()
    {
        _repo.Setup(r => r.CountPendingAsync((IReadOnlyCollection<int>?)null, SriLankaTime.Today, It.IsAny<CancellationToken>()))
             .ReturnsAsync(9);

        (await Build().GetPendingCountAsync(AdminId, UserRole.Admin)).Count.Should().Be(9);
    }

    [Fact]
    public async Task GetPendingCountAsync_SalesRep_ThrowsAuthorization()
    {
        await Build().Invoking(s => s.GetPendingCountAsync(RepId, UserRole.SalesRep))
            .Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task GetDetailAsync_RepViewingOthersRequest_ThrowsAuthorization()
    {
        Stored();

        await Build().Invoking(s => s.GetDetailAsync(500, RepId + 1, UserRole.SalesRep))
            .Should().ThrowAsync<AuthorizationException>();
    }

    // ── EffectiveStatusOf ────────────────────────────────────────────────

    [Fact]
    public void EffectiveStatusOf_PendingFromPastDay_IsExpired()
    {
        var e = new RouteUnlockRequest { Status = RouteUnlockStatus.Pending, BusinessDate = new DateOnly(2026, 9, 29) };

        RouteUnlockRequestService.EffectiveStatusOf(e, new DateOnly(2026, 9, 30), DateTime.UtcNow)
            .Should().Be("Expired");
    }

    [Fact]
    public void EffectiveStatusOf_PendingToday_IsPending()
    {
        var e = new RouteUnlockRequest { Status = RouteUnlockStatus.Pending, BusinessDate = new DateOnly(2026, 9, 30) };

        RouteUnlockRequestService.EffectiveStatusOf(e, new DateOnly(2026, 9, 30), DateTime.UtcNow)
            .Should().Be("Pending");
    }

    [Fact]
    public void EffectiveStatusOf_ApprovedAtValidTo_IsExpired()
    {
        var now = new DateTime(2026, 9, 30, 19, 0, 0, DateTimeKind.Utc);
        var e = new RouteUnlockRequest { Status = RouteUnlockStatus.Approved, ValidTo = now };

        // ValidTo is exclusive: at the boundary instant it has already ended.
        RouteUnlockRequestService.EffectiveStatusOf(e, new DateOnly(2026, 9, 30), now).Should().Be("Expired");
    }

    [Fact]
    public void EffectiveStatusOf_ApprovedBeforeValidTo_IsApproved()
    {
        var now = new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
        var e = new RouteUnlockRequest { Status = RouteUnlockStatus.Approved, ValidTo = now.AddHours(1) };

        RouteUnlockRequestService.EffectiveStatusOf(e, new DateOnly(2026, 9, 30), now).Should().Be("Approved");
    }

    [Theory]
    [InlineData(RouteUnlockStatus.Rejected)]
    [InlineData(RouteUnlockStatus.Cancelled)]
    [InlineData(RouteUnlockStatus.Revoked)]
    public void EffectiveStatusOf_TerminalStatuses_AreReportedAsStored(RouteUnlockStatus status)
    {
        var e = new RouteUnlockRequest { Status = status, BusinessDate = new DateOnly(2026, 1, 1) };

        RouteUnlockRequestService.EffectiveStatusOf(e, new DateOnly(2026, 9, 30), DateTime.UtcNow)
            .Should().Be(status.ToString());
    }
}
