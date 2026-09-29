using FluentAssertions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Supervisor.DTOs;
using sfa_api.Features.Supervisor.Repositories;
using sfa_api.Features.Supervisor.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Supervisor;

/// <summary>
/// Supervisor Sales Summary: a bill is a sale only once the distributor approved it and the rep did
/// not cancel it; money figures cover approved + pending, rejected/cancelled are counts only.
/// </summary>
public class RepBillingSummaryTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 29);

    private static RepBillingStatusGroupRow Row(
        RepBillingStatus rep, DistributorBillingStatus dist, int count, decimal total, decimal discount = 0m)
        => new(rep, dist, count, total, discount);

    [Fact]
    public void Build_BucketsByStatus_AndSumsOnlyLiveBills()
    {
        var groups = new[]
        {
            Row(RepBillingStatus.Submitted, DistributorBillingStatus.Approved, 3, 1000m, 50m),
            Row(RepBillingStatus.Submitted, DistributorBillingStatus.Pending,  2,  400m, 20m),
            Row(RepBillingStatus.Submitted, DistributorBillingStatus.Rejected, 1,  300m, 99m),
            Row(RepBillingStatus.Cancelled, DistributorBillingStatus.Pending,  1,  200m, 99m),
        };

        var dto = SupervisorService.BuildRepBillingSummary(
            From, To, groups, new RepBillingReturnTotals(30m, 12.5m));

        dto.TotalBills.Should().Be(7);
        dto.ApprovedCount.Should().Be(3);
        dto.PendingCount.Should().Be(2);
        dto.RejectedCount.Should().Be(1);
        dto.CancelledCount.Should().Be(1);
        dto.ApprovedSales.Should().Be(1000m);
        dto.PendingValue.Should().Be(400m);
        dto.TotalBilled.Should().Be(1400m);
        dto.TotalDiscount.Should().Be(70m, "rejected and cancelled discounts are excluded");
        dto.GoodReturn.Should().Be(30m);
        dto.MarketReturn.Should().Be(12.5m);
        dto.From.Should().Be(From);
        dto.To.Should().Be(To);
    }

    [Fact]
    public void Build_ApprovedThenCancelled_CountsAsCancelled_NotAsSale()
    {
        var dto = SupervisorService.BuildRepBillingSummary(From, To,
            [Row(RepBillingStatus.Cancelled, DistributorBillingStatus.Approved, 1, 900m, 10m)],
            new RepBillingReturnTotals(0m, 0m));

        dto.CancelledCount.Should().Be(1);
        dto.ApprovedCount.Should().Be(0);
        dto.ApprovedSales.Should().Be(0m);
        dto.TotalBilled.Should().Be(0m);
        dto.TotalDiscount.Should().Be(0m);
    }

    [Fact]
    public void Build_NoBills_IsAllZero()
    {
        var dto = SupervisorService.BuildRepBillingSummary(From, To, [], new RepBillingReturnTotals(0m, 0m));

        dto.TotalBills.Should().Be(0);
        dto.TotalBilled.Should().Be(0m);
    }

    [Theory]
    [InlineData("2026-09-10", "2026-09-09")] // to before from
    [InlineData("2026-01-01", "2026-04-30")] // 120 days > 92
    public async Task GetRepBillingSummary_InvalidRange_ThrowsValidation_WithoutQuerying(string from, string to)
    {
        var repo = new Mock<ISupervisorRepository>(MockBehavior.Strict);
        var sut = new SupervisorService(repo.Object, Mock.Of<ICacheService>());

        var act = () => sut.GetRepBillingSummaryAsync(7, DateOnly.Parse(from), DateOnly.Parse(to));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetRepBillingSummary_ExactlyMaxRange_IsAllowed()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = from.AddDays(SupervisorService.MaxSummaryRangeDays - 1);
        var repo = new Mock<ISupervisorRepository>();
        repo.Setup(r => r.GetRepBillingStatusGroupsAsync(7, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repo.Setup(r => r.GetRepReturnTotalsAsync(7, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepBillingReturnTotals(0m, 0m));
        var sut = new SupervisorService(repo.Object, Mock.Of<ICacheService>());

        var dto = await sut.GetRepBillingSummaryAsync(7, from, to);

        dto.TotalBills.Should().Be(0);
    }
}
