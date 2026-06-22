using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.DailyRouteAssignments.Repositories;
using sfa_api.Features.DailyRouteAssignments.Services;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.UnitTests.Features.DailyRouteAssignments.Services;

/// <summary>
/// Guards the DELETE authorization fix (audit finding #3): a SalesRep must never reach the
/// unconditional direct-delete branch. Verifies the service-level allow-list that backs the
/// controller's [Authorize(Roles="Admin,NSM,RSM,Supervisor")] attribute.
/// </summary>
public class DailyRouteAssignmentServiceTests
{
    private readonly Mock<IDailyRouteAssignmentRepository> _repoMock = new();
    private readonly Mock<IUserReportingLineRepository> _reportingMock = new();
    private readonly Mock<IDistributedLockService> _lockMock = new();
    private readonly DailyRouteAssignmentService _sut;

    public DailyRouteAssignmentServiceTests()
    {
        _sut = new DailyRouteAssignmentService(
            _repoMock.Object, _reportingMock.Object, _lockMock.Object,
            NullLogger<DailyRouteAssignmentService>.Instance);
    }

    [Fact]
    public async Task DeleteAsync_AsSalesRep_ThrowsAuthorization_AndDoesNotDelete()
    {
        var assignment = new DailyRouteAssignment { Id = 7, UserId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(assignment);

        var act = async () => await _sut.DeleteAsync(7, callerId: 99, callerRole: "SalesRep", reason: null);

        await act.Should().ThrowAsync<AuthorizationException>();
        assignment.IsDeleted.Should().BeFalse("a SalesRep must never reach the direct-delete branch");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<DailyRouteAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AsAdmin_SoftDeletesDirectly()
    {
        var assignment = new DailyRouteAssignment { Id = 8, UserId = 99 };
        _repoMock.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(assignment);

        var result = await _sut.DeleteAsync(8, callerId: 1, callerRole: "Admin", reason: null);

        result.Should().BeNull("a direct soft-delete returns no DTO (204)");
        assignment.IsActive.Should().BeFalse();
        assignment.IsDeleted.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(assignment, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
