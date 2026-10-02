using FluentAssertions;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Services;

namespace sfa_api.UnitTests.Features.Billings;

/// <summary>
/// Item-wise achievement: sold = approved sale packs minus outlet returns (floored at 0), pending is
/// reported separately, free issue is informational, and the % comes from the unrounded case figure.
/// </summary>
public class RepMonthlySalesItemwiseTests
{
    private static readonly Dictionary<int, (string Code, string Name, int PacksPerCase)> Names = new()
    {
        [1] = ("RC125", "Real Cream Cracker 125g", 48),
        [2] = ("CC100", "Chocolate Cream 100g", 60),
        [3] = ("MS200", "Milk Sorties 200g", 4),
    };

    private static RepProductSalesRow Row(int pid, decimal sale = 0m, decimal ret = 0m, decimal free = 0m,
        decimal pending = 0m, decimal amount = 0m) => new(pid, sale, ret, free, pending, amount);

    private static RepMonthlySalesItemwiseDto Build(
        IEnumerable<RepProductSalesRow> rows, Dictionary<int, decimal>? targets = null)
        => BillingService.BuildRepMonthlySalesItemwise(2026, 10, rows, targets ?? [], Names);

    [Fact]
    public void Sold_IsSalePacksMinusOutletReturns_InCases()
    {
        // 96 packs sold, 48 returned → 48 net packs = 1 case.
        var dto = Build([Row(1, sale: 96m, ret: 48m, amount: 500m)], new() { [1] = 4m });

        var item = dto.Items.Single();
        item.SoldQuantityPacks.Should().Be(48m);
        item.SoldQuantity.Should().Be(1m);
        item.ReturnQuantityPacks.Should().Be(48m);
        item.AchievementPercent.Should().Be(25m);
        item.HasTarget.Should().BeTrue();
    }

    [Fact]
    public void ReturnsLargerThanSales_FloorAtZero_NeverNegative()
    {
        var dto = Build([Row(2, sale: 5m, ret: 20m, amount: -300m)], new() { [2] = 2m });

        var item = dto.Items.Single();
        item.SoldQuantityPacks.Should().Be(0m);
        item.SoldQuantity.Should().Be(0m);
        item.SoldAmount.Should().Be(0m);
        item.AchievementPercent.Should().Be(0m);
    }

    [Fact]
    public void Achievement_UsesUnroundedCases_NotTheRoundedDisplayFigure()
    {
        // 7 packs of 48 = 0.1458 cases (shown as 0.1); against a 3-case target that is 4.9%, not 3.3%.
        var dto = Build([Row(1, sale: 7m)], new() { [1] = 3m });

        var item = dto.Items.Single();
        item.SoldQuantity.Should().Be(0.1m);
        item.AchievementPercent.Should().Be(4.9m);
    }

    [Fact]
    public void Pending_IsReportedSeparately_AndNotCountedAsSold()
    {
        var dto = Build([Row(3, sale: 8m, pending: 16m)], new() { [3] = 10m });

        var item = dto.Items.Single();
        item.SoldQuantityPacks.Should().Be(8m);
        item.PendingQuantityPacks.Should().Be(16m);
        item.PendingQuantity.Should().Be(4m);
        item.AchievementPercent.Should().Be(20m, "8 packs = 2 cases of a 10-case target; pending does not count");
        dto.TotalPendingQuantityPacks.Should().Be(16m);
    }

    [Fact]
    public void FreeIssue_IsInformationalOnly()
    {
        var dto = Build([Row(3, sale: 20m, free: 8m)], new() { [3] = 10m });

        var item = dto.Items.Single();
        item.FreeIssueQuantityPacks.Should().Be(8m);
        item.SoldQuantityPacks.Should().Be(20m, "free issue is never counted as sold");
    }

    [Fact]
    public void TargetedItems_ComeFirstLaggardsFirst_ThenUntargetedBestSellersFirst()
    {
        var dto = Build(
            [Row(1, sale: 96m), Row(2, sale: 60m), Row(3, sale: 40m)],
            new() { [1] = 4m, [2] = 1m });   // 1: 50%, 2: 100%, 3: no target

        dto.Items.Select(i => i.ProductId).Should().Equal(1, 2, 3);
        dto.Items[2].HasTarget.Should().BeFalse();
        dto.Items[2].AchievementPercent.Should().Be(0m);
    }

    [Fact]
    public void TargetWithNoSales_StillAppears_AtZero()
    {
        var dto = Build([], new() { [2] = 5m });

        var item = dto.Items.Single();
        item.ProductId.Should().Be(2);
        item.SoldQuantity.Should().Be(0m);
        item.AchievementPercent.Should().Be(0m);
        dto.TotalTargetQuantity.Should().Be(5m);
    }

    [Fact]
    public void OverallAchievement_ComesFromTargetedItemsOnly()
    {
        // Item 1: 2 of 4 cases, item 2: 1 of 1 case → 3/5 = 60%. Untargeted item 3 is ignored.
        var dto = Build(
            [Row(1, sale: 96m), Row(2, sale: 60m), Row(3, sale: 400m)],
            new() { [1] = 4m, [2] = 1m });

        dto.OverallAchievementPercent.Should().Be(60m);
    }

    [Fact]
    public void NothingAtAll_IsEmpty_AndZeroPercent()
    {
        var dto = Build([]);

        dto.Items.Should().BeEmpty();
        dto.OverallAchievementPercent.Should().Be(0m);
    }
}
