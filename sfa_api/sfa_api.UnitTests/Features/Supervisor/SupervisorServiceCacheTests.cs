using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using sfa_api.Features.Supervisor;
using sfa_api.Features.Supervisor.Repositories;
using sfa_api.Features.Supervisor.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Supervisor;

/// <summary>
/// The supervisor dashboard summary is cached per supervisor + date (60s) and evicted per supervisor
/// by the bill / not-billing / route-assignment write paths. Uses the real DistributedCacheService over
/// an in-memory store so the key ↔ prefix alignment is exercised, not mocked.
/// </summary>
public class SupervisorServiceCacheTests
{
    private readonly MemoryDistributedCache _store = new(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly Mock<ISupervisorRepository> _repo = new();
    private static readonly DateOnly Day = new(2026, 9, 17);

    private DistributedCacheService NewCache() => new(_store, NullLogger<DistributedCacheService>.Instance);
    private SupervisorService NewSut() => new(_repo.Object, NewCache());

    private void SetupRepo(int supervisorId, int totalReps)
    {
        _repo.Setup(r => r.CountRepsByReportsToAsync(supervisorId, It.IsAny<CancellationToken>())).ReturnsAsync(totalReps);
        _repo.Setup(r => r.CountAssignedRepsTodayAsync(supervisorId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _repo.Setup(r => r.CountAndSumBillsTodayAsync(supervisorId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync((3, 150.25m));
        _repo.Setup(r => r.CountNonBillingsTodayBySupervisorAsync(supervisorId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(4);
    }

    [Fact]
    public async Task GetSummary_SecondCall_IsServedFromCache_WithIdenticalValues()
    {
        SetupRepo(7, totalReps: 5);

        var first = await NewSut().GetSummaryAsync(7, Day);
        var second = await NewSut().GetSummaryAsync(7, Day);

        second.Should().Be(first);
        first.TotalReps.Should().Be(5);
        first.TotalSalesToday.Should().Be(150.25m);
        _repo.Verify(r => r.CountRepsByReportsToAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSummary_IsKeyedPerSupervisorAndDate()
    {
        SetupRepo(7, totalReps: 5);
        SetupRepo(8, totalReps: 9);

        (await NewSut().GetSummaryAsync(7, Day)).TotalReps.Should().Be(5);
        (await NewSut().GetSummaryAsync(8, Day)).TotalReps.Should().Be(9);
        await NewSut().GetSummaryAsync(7, Day.AddDays(-1));

        _repo.Verify(r => r.CountRepsByReportsToAsync(7, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvictingOneSupervisor_RecomputesOnlyThatSupervisor()
    {
        SetupRepo(7, totalReps: 5);
        SetupRepo(8, totalReps: 9);
        await NewSut().GetSummaryAsync(7, Day);
        await NewSut().GetSummaryAsync(8, Day);

        // What BillingService / NotBillingService / DailyRouteAssignmentService do on a write.
        await NewCache().RemoveByPrefixAsync(SupervisorSummaryCacheKeys.ForSupervisor(7));
        SetupRepo(7, totalReps: 6);

        (await NewSut().GetSummaryAsync(7, Day)).TotalReps.Should().Be(6);
        (await NewSut().GetSummaryAsync(8, Day)).TotalReps.Should().Be(9);
        _repo.Verify(r => r.CountRepsByReportsToAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Keys_EvictionPrefixCoversTheSummaryKeyNamespace()
    {
        var key = SupervisorSummaryCacheKeys.Summary(7, Day);
        key.Should().Be("supervisor-summary:7:2026-09-17");
        DistributedCacheService.KeyNamespaces(key)
            .Should().Contain(DistributedCacheService.PrefixNamespace(SupervisorSummaryCacheKeys.ForSupervisor(7)));
        DistributedCacheService.KeyNamespaces(SupervisorSummaryCacheKeys.Summary(71, Day))
            .Should().NotContain(DistributedCacheService.PrefixNamespace(SupervisorSummaryCacheKeys.ForSupervisor(7)));
    }
}
