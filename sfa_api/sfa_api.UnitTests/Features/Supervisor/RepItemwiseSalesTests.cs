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
/// Supervisor Item-wise Sales: per-product packs and values over approved + pending bills.
/// Net = gross − (item + allocated bill discount) − good return − market return.
/// </summary>
public class RepItemwiseSalesTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 29);

    private static readonly Dictionary<int, (string Code, string Name)> Names = new()
    {
        [1] = ("P001", "Tea 100g"),
        [2] = ("P002", "Biscuit"),
        [3] = ("P003", "Soap"),
    };

    private static RepItemSalesAgg Agg(
        int productId, decimal saleQty = 0m, decimal freeQty = 0m, decimal gross = 0m,
        decimal itemDisc = 0m, decimal billDisc = 0m,
        decimal goodQty = 0m, decimal goodVal = 0m, decimal mktQty = 0m, decimal mktVal = 0m)
        => new(productId, saleQty, freeQty, gross, itemDisc, billDisc, goodQty, goodVal, mktQty, mktVal);

    [Fact]
    public void Build_ComputesNetPerItem_AndTotals()
    {
        var dto = SupervisorService.BuildRepItemwiseSales(From, To,
        [
            Agg(1, saleQty: 10m, gross: 1000m, itemDisc: 50m, billDisc: 9.5m, goodQty: 1m, goodVal: 100m),
            Agg(2, saleQty: 4m, freeQty: 1m, gross: 200m, mktQty: 2m, mktVal: 40m),
        ], Names, headerDiscount: 59.5m);

        var tea = dto.Items.Single(i => i.ProductId == 1);
        tea.ItemCode.Should().Be("P001");
        tea.ItemName.Should().Be("Tea 100g");
        tea.Discount.Should().Be(59.5m, "item-wise + allocated bill discount");
        tea.NetValue.Should().Be(1000m - 59.5m - 100m);

        var biscuit = dto.Items.Single(i => i.ProductId == 2);
        biscuit.FreeIssueQty.Should().Be(1m);
        biscuit.NetValue.Should().Be(160m);

        dto.TotalSaleQty.Should().Be(14m);
        dto.TotalFreeIssueQty.Should().Be(1m);
        dto.TotalGoodReturnQty.Should().Be(1m);
        dto.TotalMarketReturnQty.Should().Be(2m);
        dto.TotalGrossValue.Should().Be(1200m);
        dto.TotalDiscount.Should().Be(59.5m);
        dto.TotalGoodReturnValue.Should().Be(100m);
        dto.TotalMarketReturnValue.Should().Be(40m);
        dto.TotalNetValue.Should().Be(1000.5m);
        dto.TotalNetValue.Should().Be(
            dto.TotalGrossValue - dto.TotalDiscount - dto.TotalGoodReturnValue - dto.TotalMarketReturnValue);
        dto.From.Should().Be(From);
        dto.To.Should().Be(To);
    }

    [Fact]
    public void Build_TotalDiscount_ComesFromBillHeaders_NotPerItemShares()
    {
        // Three allocated shares of .005 each round to 0.00 per item (banker's), but the bill
        // header — what the Sales Summary sums — says 0.02. Totals must follow the header.
        var dto = SupervisorService.BuildRepItemwiseSales(From, To,
        [
            Agg(1, saleQty: 1m, gross: 10m, billDisc: 0.005m),
            Agg(2, saleQty: 1m, gross: 10m, billDisc: 0.005m),
            Agg(3, saleQty: 1m, gross: 10m, billDisc: 0.005m),
        ], Names, headerDiscount: 0.02m);

        dto.Items.Sum(i => i.Discount).Should().Be(0m);
        dto.TotalDiscount.Should().Be(0.02m);
        dto.TotalNetValue.Should().Be(29.98m);
    }

    [Fact]
    public void Build_OrdersBestSellersFirst()
    {
        var dto = SupervisorService.BuildRepItemwiseSales(From, To,
        [
            Agg(3, saleQty: 1m, gross: 50m),
            Agg(1, saleQty: 2m, gross: 500m),
            Agg(2, goodQty: 3m, goodVal: 30m),
        ], Names, 0m);

        dto.Items.Select(i => i.ProductId).Should().Equal(1, 3, 2);
    }

    [Fact]
    public void Build_DropsRowsWithNoQuantity_LikeDistributorReturnOnly()
    {
        var dto = SupervisorService.BuildRepItemwiseSales(From, To,
            [Agg(1, saleQty: 1m, gross: 10m), Agg(2)], Names, 0m);

        dto.Items.Should().ContainSingle().Which.ProductId.Should().Be(1);
    }

    [Fact]
    public void Build_UnknownProduct_GetsPlaceholderLabel()
    {
        var dto = SupervisorService.BuildRepItemwiseSales(From, To,
            [Agg(99, saleQty: 1m, gross: 10m)], Names, 0m);

        dto.Items[0].ItemCode.Should().Be("#99");
        dto.Items[0].ItemName.Should().Be("Product 99");
    }

    [Fact]
    public void Build_NoSales_ReturnsEmptyWithZeroTotals()
    {
        var dto = SupervisorService.BuildRepItemwiseSales(From, To, [], Names, 0m);

        dto.Items.Should().BeEmpty();
        dto.TotalNetValue.Should().Be(0m);
        dto.TotalSaleQty.Should().Be(0m);
    }

    [Fact]
    public async Task Get_RangeOver92Days_Throws()
    {
        var repo = new Mock<ISupervisorRepository>(MockBehavior.Strict);
        var sut = new SupervisorService(repo.Object, Mock.Of<ICacheService>());

        var act = () => sut.GetRepItemwiseSalesAsync(7, From, From.AddDays(92));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Get_ToBeforeFrom_Throws()
    {
        var repo = new Mock<ISupervisorRepository>(MockBehavior.Strict);
        var sut = new SupervisorService(repo.Object, Mock.Of<ICacheService>());

        var act = () => sut.GetRepItemwiseSalesAsync(7, To, From);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Get_LooksUpNamesForReturnedProducts()
    {
        var repo = new Mock<ISupervisorRepository>();
        repo.Setup(r => r.GetRepItemSalesAsync(7, From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Agg(2, saleQty: 3m, gross: 90m)]);
        repo.Setup(r => r.GetProductNamesAsync(
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 2 })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, (string, string)> { [2] = ("P002", "Biscuit") });
        repo.Setup(r => r.GetRepBillingStatusGroupsAsync(7, From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new(RepBillingStatus.Submitted, DistributorBillingStatus.Approved, 1, 60m, 3m),
                new(RepBillingStatus.Submitted, DistributorBillingStatus.Pending,  1, 25m, 2m),
                new(RepBillingStatus.Submitted, DistributorBillingStatus.Rejected, 1, 40m, 7m),
                new(RepBillingStatus.Cancelled, DistributorBillingStatus.Approved, 1, 50m, 9m),
            ]);
        var sut = new SupervisorService(repo.Object, Mock.Of<ICacheService>());

        var dto = await sut.GetRepItemwiseSalesAsync(7, From, To);

        dto.Items.Should().ContainSingle().Which.ItemName.Should().Be("Biscuit");
        dto.TotalDiscount.Should().Be(5m, "cancelled and rejected bill discounts are excluded");
        dto.TotalNetValue.Should().Be(85m);
    }
}
