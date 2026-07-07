using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using sfa_api.Features.NotBillings.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.NotBillings.Repositories;
using sfa_api.Features.NotBillings.Requests;
using sfa_api.Features.NotBillings.Services;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserReportingLines.Repositories;

namespace sfa_api.UnitTests.Features.NotBillings.Services;

/// <summary>
/// Covers the durable idempotency behaviour added for finding #6 — an offline retry / replay of a
/// not-billing submission must never create a duplicate compliance record.
/// </summary>
public class NotBillingServiceTests
{
    private readonly Mock<INotBillingRepository> _repoMock = new();
    private readonly Mock<IUserGeoAssignmentRepository> _geoMock = new();
    private readonly Mock<IUserReportingLineRepository> _reportingMock = new();
    private readonly NotBillingService _sut;

    public NotBillingServiceTests()
        => _sut = new NotBillingService(_repoMock.Object, _geoMock.Object, _reportingMock.Object);

    private static CreateNotBillingRequest ValidRequest() => new()
    {
        OutletId = 10,
        Reason   = NotBillingReason.OutletClosed,
        Notes    = "Shutter down"
    };

    private static NotBilling FakeNotBilling(int id) => new()
    {
        Id               = id,
        NotBillingNumber = $"NBL-2026-{id:D5}",
        NotBillingDate   = new DateOnly(2026, 7, 7),
        Reason           = NotBillingReason.OutletClosed
    };

    [Fact]
    public async Task CreateAsync_WithAlreadyPersistedClientRecordId_ReturnsExistingAndDoesNotCreate()
    {
        const string key = "11111111-1111-1111-1111-111111111111";
        _repoMock.Setup(r => r.FindIdByClientRecordIdAsync(key, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(42);
        _repoMock.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(FakeNotBilling(42));

        var result = await _sut.CreateAsync(ValidRequest(), salesRepId: 200, clientRecordId: key);

        result.Id.Should().Be(42);
        // Fast path must short-circuit BEFORE any create-side work — no duplicate row, no burned
        // sequence value, no outlet lookup.
        _repoMock.Verify(r => r.AddAsync(It.IsAny<NotBilling>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.GetNextNotBillingNumberAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.GetOutletAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentReplayLosesInsertRace_ReturnsWinnerIdempotently()
    {
        const string key = "22222222-2222-2222-2222-222222222222";

        // Fast path misses (row not yet visible), then the unique index rejects our insert, then
        // the catch-path lookup finds the winner the concurrent request committed.
        _repoMock.SetupSequence(r => r.FindIdByClientRecordIdAsync(key, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((int?)null)
                 .ReturnsAsync(99);
        _repoMock.Setup(r => r.GetOutletAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Outlet { Id = 10, RouteId = 5, DivisionId = 3, IsActive = true });
        _repoMock.Setup(r => r.ExistsForOutletTodayAsync(200, 10, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _geoMock.Setup(g => g.GetActiveByUserIdAsync(200, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserGeoAssignment { TerritoryId = 7, AreaId = 4, RegionId = 2 });
        _repoMock.Setup(r => r.GetNextNotBillingNumberAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new DbUpdateException());
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(FakeNotBilling(99));

        var result = await _sut.CreateAsync(ValidRequest(), salesRepId: 200, clientRecordId: key);

        // The losing request resolves to the winner's record instead of surfacing a 409/duplicate.
        result.Id.Should().Be(99);
        _repoMock.Verify(r => r.FindIdByClientRecordIdAsync(key, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
