using FluentAssertions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Billings.Services;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.UnitTests.Features.Billings;

public class DistributorBillingDashboardServiceTests
{
    private static readonly DateOnly D1 = new(2026, 9, 10);

    private static DistributorBillingDashboardGroupRow Row(
        DateOnly date, DistributorBillingStatus ds, RepBillingStatus rs, int count, decimal total)
        => new(date, ds, rs, count, total);

    [Fact]
    public void Aggregate_MatchesFormerClientSideFormulas_WithCancelledExcludedFromRevenue()
    {
        var rows = new[]
        {
            Row(D1, DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 2, 300m),
            Row(D1, DistributorBillingStatus.Approved, RepBillingStatus.Cancelled, 1, 40m),
            Row(D1, DistributorBillingStatus.Pending,  RepBillingStatus.Submitted, 3, 90m),
            Row(D1, DistributorBillingStatus.Pending,  RepBillingStatus.Cancelled, 1, 7m),
            Row(D1, DistributorBillingStatus.Rejected, RepBillingStatus.Submitted, 1, 11m),
            Row(D1, DistributorBillingStatus.Rejected, RepBillingStatus.Cancelled, 1, 13m),
        };

        var result = DistributorBillingDashboardService.Aggregate(D1, D1, rows);

        result.TotalCount.Should().Be(9);             // every bill issued, like the list's totalCount
        result.TotalRevenue.Should().Be(390m);        // 300 + 90 (cancelled and rejected excluded)
        result.ApprovedCount.Should().Be(3);          // count by distributor status, as before
        result.ApprovedRevenue.Should().Be(300m);
        result.PendingCount.Should().Be(4);
        result.PendingRevenue.Should().Be(90m);
        result.Days.Should().ContainSingle();
    }

    [Fact]
    public void Aggregate_ZeroFillsEveryDateInRange_OldestFirst()
    {
        var from = D1.AddDays(-6);
        var rows = new[] { Row(D1.AddDays(-3), DistributorBillingStatus.Pending, RepBillingStatus.Submitted, 1, 5m) };

        var result = DistributorBillingDashboardService.Aggregate(from, D1, rows);

        result.Days.Select(d => d.Date).Should().Equal(Enumerable.Range(0, 7).Select(i => from.AddDays(i)));
        result.Days.Single(d => d.Date == D1.AddDays(-3)).PendingRevenue.Should().Be(5m);
        result.Days.Where(d => d.Date != D1.AddDays(-3)).Should().OnlyContain(d => d.TotalCount == 0 && d.TotalRevenue == 0m);
        result.PendingRevenue.Should().Be(5m);
    }

    [Fact]
    public void Aggregate_IgnoresRowsOutsideRange()
    {
        var rows = new[] { Row(D1.AddDays(1), DistributorBillingStatus.Approved, RepBillingStatus.Submitted, 1, 5m) };

        var result = DistributorBillingDashboardService.Aggregate(D1, D1, rows);

        result.TotalCount.Should().Be(0);
        result.ApprovedRevenue.Should().Be(0m);
    }

    [Fact]
    public async Task GetSummaryAsync_ResolvesDistributorFromCaller_AndDefaultsToToday()
    {
        var repo = new Mock<IDistributorBillingDashboardRepository>();
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetUserByIdAsync(42, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new User { Id = 42, Role = UserRole.Distributor, DistributorId = 7 });
        repo.Setup(r => r.GetGroupedAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DistributorBillingDashboardGroupRow>());

        var result = await new DistributorBillingDashboardService(repo.Object, users.Object)
            .GetSummaryAsync(42, null, null);

        var today = sfa_api.Common.Extensions.SriLankaTime.Today;
        result.DateFrom.Should().Be(today);
        result.DateTo.Should().Be(today);
        repo.Verify(r => r.GetGroupedAsync(7, today, today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_UserWithoutDistributor_ThrowsBusinessRule()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new User { Id = 1, Role = UserRole.Distributor, DistributorId = null });

        var act = () => new DistributorBillingDashboardService(Mock.Of<IDistributorBillingDashboardRepository>(), users.Object)
            .GetSummaryAsync(1, null, null);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.ErrorCode.Should().Be("NO_DISTRIBUTOR_LINKED");
    }

    [Theory]
    [InlineData("2026-09-10", "2026-09-09")]
    [InlineData("2026-01-01", "2026-09-01")]
    public async Task GetSummaryAsync_InvalidRange_ThrowsValidation(string from, string to)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new User { Id = 1, Role = UserRole.Distributor, DistributorId = 3 });

        var act = () => new DistributorBillingDashboardService(Mock.Of<IDistributorBillingDashboardRepository>(), users.Object)
            .GetSummaryAsync(1, DateOnly.Parse(from), DateOnly.Parse(to));

        await act.Should().ThrowAsync<ValidationException>();
    }
}
