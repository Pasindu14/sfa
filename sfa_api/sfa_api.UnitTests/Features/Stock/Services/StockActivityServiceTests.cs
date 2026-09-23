using FluentAssertions;
using Moq;
using sfa_api.Common.Extensions;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;
using sfa_api.Features.Stock.Validators;

namespace sfa_api.UnitTests.Features.Stock.Services;

public class StockActivityServiceTests
{
    private readonly Mock<IStockActivityRepository> _repoMock = new();
    private readonly StockActivityService _sut;

    private static readonly DateOnly From = new(2026, 6, 1);
    private static readonly DateOnly To   = new(2026, 6, 15);

    public StockActivityServiceTests()
    {
        _sut = new StockActivityService(_repoMock.Object);
        _repoMock
            .Setup(r => r.GetReferenceNumbersAsync(
                It.IsAny<IEnumerable<(string, int)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(string ReferenceType, int ReferenceId), string>());
    }

    private static StockActivityQuery Query(
        DateOnly? from = null, DateOnly? to = null, string? type = null, string? direction = null,
        int page = 1, int pageSize = 50) =>
        new(from ?? From, to ?? To, null, null, null, type, direction, page, pageSize);

    private static StockActivityDto Row(int id, string referenceType, int referenceId) => new(
        id, DateTime.UtcNow, 1, "Admin", 10, "DB", 5, "P5", "Product 5", 12,
        "Normal", "Correction", "In", 1m, 0m, 1m, referenceType, referenceId, null, null);

    private void SetupPage(params StockActivityDto[] rows) =>
        _repoMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<StockTransactionType?>(), It.IsAny<StockTransactionDirection?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((rows.ToList(), rows.Length));

    // ── Service ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivityAsync_UsesSriLankaDayWindow_AndParsesFilters_CaseInsensitive()
    {
        SetupPage();

        await _sut.GetActivityAsync(Query(type: "transferin", direction: "out", page: 3, pageSize: 25));

        _repoMock.Verify(r => r.GetPagedAsync(
            SriLankaTime.StartOfDayUtc(From),
            SriLankaTime.StartOfDayUtc(To.AddDays(1)),
            null, null, null,
            StockTransactionType.TransferIn, StockTransactionDirection.Out,
            50, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActivityAsync_ResolvesReferenceNumbers_InOneBatch()
    {
        SetupPage(Row(1, "StockTransfer", 7), Row(2, "StockAdjustment", 3), Row(3, "StockTaking", 99));
        _repoMock
            .Setup(r => r.GetReferenceNumbersAsync(
                It.IsAny<IEnumerable<(string, int)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(string ReferenceType, int ReferenceId), string>
            {
                [("StockTransfer", 7)]   = "ST-000007",
                [("StockAdjustment", 3)] = "SA-000003",
            });

        var (items, total) = await _sut.GetActivityAsync(Query());

        total.Should().Be(3);
        items.Select(i => i.ReferenceNumber).Should().Equal("ST-000007", "SA-000003", null);
        _repoMock.Verify(r => r.GetReferenceNumbersAsync(
            It.IsAny<IEnumerable<(string, int)>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActivityAsync_EmptyPage_SkipsReferenceLookup()
    {
        SetupPage();

        var (items, _) = await _sut.GetActivityAsync(Query());

        items.Should().BeEmpty();
        _repoMock.Verify(r => r.GetReferenceNumbersAsync(
            It.IsAny<IEnumerable<(string, int)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    private readonly StockActivityQueryValidator _validator = new();

    [Fact]
    public void Validator_ValidQuery_Passes()
        => _validator.Validate(Query(type: "Sale", direction: "In")).IsValid.Should().BeTrue();

    [Fact]
    public void Validator_MissingDates_Fail()
    {
        var result = _validator.Validate(new StockActivityQuery(null, null, null, null, null, null, null));

        result.Errors.Select(e => e.PropertyName).Should().Contain(["From", "To"]);
    }

    [Fact]
    public void Validator_ToBeforeFrom_Fails()
        => _validator.Validate(Query(from: To, to: From)).Errors.Should().Contain(e => e.PropertyName == "To");

    [Fact]
    public void Validator_SameDay_Passes()
        => _validator.Validate(Query(from: From, to: From)).IsValid.Should().BeTrue();

    [Fact]
    public void Validator_RangeOf93Days_Passes()
        => _validator.Validate(Query(from: From, to: From.AddDays(93))).IsValid.Should().BeTrue();

    [Fact]
    public void Validator_RangeOver93Days_Fails()
    {
        var result = _validator.Validate(Query(from: From, to: From.AddDays(94)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "To" && e.ErrorMessage.Contains("93"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Validator_PageSizeOutOfRange_Fails(int pageSize)
        => _validator.Validate(Query(pageSize: pageSize)).Errors.Should().Contain(e => e.PropertyName == "PageSize");

    [Fact]
    public void Validator_PageSize1000_Passes()
        => _validator.Validate(Query(pageSize: 1000)).IsValid.Should().BeTrue();

    [Fact]
    public void Validator_PageZero_Fails()
        => _validator.Validate(Query(page: 0)).Errors.Should().Contain(e => e.PropertyName == "Page");

    [Theory]
    [InlineData("Bogus")]
    [InlineData("3")]      // numeric strings would parse as enum values — only names are accepted
    public void Validator_UnknownTransactionType_Fails(string type)
        => _validator.Validate(Query(type: type)).Errors.Should().Contain(e => e.PropertyName == "TransactionType");

    [Theory]
    [InlineData("Sideways")]
    [InlineData("0")]
    public void Validator_UnknownDirection_Fails(string direction)
        => _validator.Validate(Query(direction: direction)).Errors.Should().Contain(e => e.PropertyName == "Direction");
}
