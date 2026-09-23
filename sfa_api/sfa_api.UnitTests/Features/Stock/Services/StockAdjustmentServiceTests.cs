using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;
using sfa_api.Features.Stock.Validators;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.Stock.Services;

public class StockAdjustmentServiceTests
{
    private readonly Mock<IStockAdjustmentRepository> _repoMock        = new();
    private readonly Mock<IStockRepository>           _stockRepoMock   = new();
    private readonly Mock<IDistributedLockService>    _lockServiceMock = new();
    private readonly Mock<IDbContextTransaction>      _txMock          = new();
    private readonly Mock<IAsyncDisposable>           _lockMock        = new();
    private readonly AppDbContext _dbContext;
    private readonly StockAdjustmentService _sut;

    private const int CallerId        = 42;
    private const int DistributorId   = 10;
    private const int ProductA        = 5;
    private const int ProductB        = 6;
    private const int NewAdjustmentId = 123;

    public StockAdjustmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        // EnsureCreated() omitted — the context is used only for CreateExecutionStrategy().

        _sut = new StockAdjustmentService(_repoMock.Object, _stockRepoMock.Object, _lockServiceMock.Object, _dbContext);

        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _repoMock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_txMock.Object);

        SetupDistributor(isActive: true);

        _repoMock
            .Setup(r => r.GetProductsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Product>
            {
                [ProductA] = new() { Id = ProductA, Code = "PA", ItemDescription = "Product A", PiecesPerPack = 12 },
                [ProductB] = new() { Id = ProductB, Code = "PB", ItemDescription = "Product B", PiecesPerPack = 24 },
            });

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new StockAdjustmentDto(
                id, $"SA-{id:D6}", DistributorId, "DB", "CountCorrection", null, "Admin",
                DateTime.UtcNow, 0, 0m, 0m, []));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SetupDistributor(bool isActive) =>
        _repoMock
            .Setup(r => r.GetDistributorAsync(DistributorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Distributor { Id = DistributorId, Name = "DB", IsActive = isActive });

    private void SetupStock(params (int ProductId, StockType Type, decimal Qty)[] rows) =>
        _stockRepoMock
            .Setup(r => r.LockStocksForUpdateAsync(It.IsAny<IEnumerable<StockKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToDictionary(
                r => new StockKey(DistributorId, r.ProductId, r.Type),
                r => new DistributorStock
                {
                    DistributorId = DistributorId, ProductId = r.ProductId, StockType = r.Type, QuantityOnHand = r.Qty,
                }));

    /// <summary>Captures the added header and assigns its Id on the first save, as the DB would.</summary>
    private Func<StockAdjustment?> CaptureInsert()
    {
        StockAdjustment? captured = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<StockAdjustment>(), It.IsAny<CancellationToken>()))
            .Callback<StockAdjustment, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);
        _repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { if (captured is not null && captured.Id == 0) captured.Id = NewAdjustmentId; })
            .Returns(Task.CompletedTask);
        return () => captured;
    }

    private static CreateStockAdjustmentLineRequest Line(int productId, decimal expected, decimal newQty,
        StockType type = StockType.Normal) => new(productId, type, expected, newQty);

    private static CreateStockAdjustmentRequest Request(params CreateStockAdjustmentLineRequest[] lines) =>
        new(DistributorId, StockAdjustmentReason.CountCorrection, "Monthly count", lines.ToList());

    private void VerifyNoLedgerWrites()
    {
        _stockRepoMock.Verify(r => r.CreditStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockType>(),
            It.IsAny<StockTransactionType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockRepoMock.Verify(r => r.DeductStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockType>(),
            It.IsAny<StockTransactionType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<StockAdjustment>(), It.IsAny<CancellationToken>()), Times.Never);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Ledger writes ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Increase_CreditsCorrection_WithReference()
    {
        SetupStock((ProductA, StockType.Normal, 10m));
        var captured = CaptureInsert();

        var result = await _sut.CreateAsync(Request(Line(ProductA, 10m, 25m)), CallerId);

        result.Id.Should().Be(NewAdjustmentId);
        var adjustment = captured()!;
        adjustment.AdjustmentNumber.Should().Be("SA-000123");
        adjustment.Reason.Should().Be(StockAdjustmentReason.CountCorrection);
        adjustment.AdjustedBy.Should().Be(CallerId);
        adjustment.Lines.Should().ContainSingle(l =>
            l.ProductId == ProductA && l.QuantityBefore == 10m && l.NewQuantity == 25m && l.Difference == 15m);

        _stockRepoMock.Verify(r => r.CreditStockAsync(
            DistributorId, ProductA, 15m, StockType.Normal, StockTransactionType.Correction,
            "StockAdjustment", NewAdjustmentId, CallerId,
            It.Is<string?>(n => n!.Contains("SA-000123") && n.Contains("CountCorrection")),
            It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Decrease_DeductsCorrection()
    {
        SetupStock((ProductA, StockType.FreeIssue, 40m));
        var captured = CaptureInsert();

        await _sut.CreateAsync(Request(Line(ProductA, 40m, 0m, StockType.FreeIssue)), CallerId);

        captured()!.Lines.Single().Difference.Should().Be(-40m);
        _stockRepoMock.Verify(r => r.DeductStockAsync(
            DistributorId, ProductA, 40m, StockType.FreeIssue, StockTransactionType.Correction,
            "StockAdjustment", NewAdjustmentId, CallerId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ProductWithNoStockRow_TreatedAsZero_AndCredited()
    {
        SetupStock();   // no rows at all
        var captured = CaptureInsert();

        await _sut.CreateAsync(Request(Line(ProductB, 0m, 12m)), CallerId);

        captured()!.Lines.Single().QuantityBefore.Should().Be(0m);
        _stockRepoMock.Verify(r => r.CreditStockAsync(
            DistributorId, ProductB, 12m, StockType.Normal, StockTransactionType.Correction,
            "StockAdjustment", NewAdjustmentId, CallerId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnchangedLines_AreDropped()
    {
        SetupStock((ProductA, StockType.Normal, 10m), (ProductB, StockType.Normal, 7m));
        var captured = CaptureInsert();

        await _sut.CreateAsync(Request(Line(ProductA, 10m, 10m), Line(ProductB, 7m, 5m)), CallerId);

        captured()!.Lines.Should().ContainSingle(l => l.ProductId == ProductB && l.Difference == -2m);
        _stockRepoMock.Verify(r => r.CreditStockAsync(
            It.IsAny<int>(), ProductA, It.IsAny<decimal>(), It.IsAny<StockType>(),
            It.IsAny<StockTransactionType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ClosedDistributor_IsAllowed()
    {
        SetupDistributor(isActive: false);
        SetupStock((ProductA, StockType.Normal, 10m));
        CaptureInsert();

        var act = () => _sut.CreateAsync(Request(Line(ProductA, 10m, 3m)), CallerId);

        await act.Should().NotThrowAsync();
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Rejections ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_StaleExpectedQuantity_ThrowsStockChanged_AndWritesNothing()
    {
        SetupStock((ProductA, StockType.Normal, 8m), (ProductB, StockType.Normal, 3m));
        CaptureInsert();

        var act = () => _sut.CreateAsync(Request(Line(ProductA, 10m, 20m), Line(ProductB, 3m, 1m)), CallerId);

        var ex = await act.Should().ThrowAsync<StockChangedException>();
        ex.Which.ErrorCode.Should().Be("STOCK_CHANGED");
        ex.Which.Changes.Should().ContainSingle(c => c.ProductId == ProductA && c.Expected == 10m && c.Current == 8m);
        ex.Which.Fields.Should().ContainKey($"product:{ProductA}").And.HaveCount(1);
        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoLedgerWrites();
    }

    [Fact]
    public async Task CreateAsync_AllDifferencesZero_ThrowsNoChanges()
    {
        SetupStock((ProductA, StockType.Normal, 10m));

        var act = () => _sut.CreateAsync(Request(Line(ProductA, 10m, 10m), Line(ProductB, 0m, 0m)), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("NO_CHANGES");
        VerifyNoLedgerWrites();
    }

    [Fact]
    public async Task CreateAsync_LockBusy_ThrowsStockAdjustmentInProgress()
    {
        _lockServiceMock
            .Setup(l => l.AcquireAsync($"stock-adjustment:{DistributorId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var act = () => _sut.CreateAsync(Request(Line(ProductA, 10m, 5m)), CallerId);

        var ex = await act.Should().ThrowAsync<StockAdjustmentInProgressException>();
        ex.Which.ErrorCode.Should().Be("STOCK_ADJUSTMENT_IN_PROGRESS");
        _repoMock.Verify(r => r.GetDistributorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownDistributor_ThrowsDistributorNotFound()
    {
        _repoMock
            .Setup(r => r.GetDistributorAsync(DistributorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Distributor?)null);

        var act = () => _sut.CreateAsync(Request(Line(ProductA, 10m, 5m)), CallerId);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DISTRIBUTOR_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_UnknownProduct_ThrowsProductNotFound()
    {
        var act = () => _sut.CreateAsync(Request(Line(999, 0m, 5m)), CallerId);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        _repoMock.Verify(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ThrowsNotFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockAdjustmentDto?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("STOCKADJUSTMENT_NOT_FOUND");
    }

    // ── Validator ──────────────────────────────────────────────────────────

    private readonly CreateStockAdjustmentValidator _validator = new();

    [Fact]
    public void Validator_ValidRequest_Passes()
        => _validator.Validate(Request(Line(ProductA, 10m, 0m))).IsValid.Should().BeTrue();

    [Fact]
    public void Validator_EmptyLines_Fails()
    {
        var result = _validator.Validate(Request());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validator_NegativeNewQuantity_Fails()
    {
        var result = _validator.Validate(Request(Line(ProductA, 10m, -1m)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines[0].NewQuantity");
    }

    [Fact]
    public void Validator_DuplicateProductAndStockType_Fails()
    {
        var result = _validator.Validate(Request(Line(ProductA, 1m, 2m), Line(ProductA, 1m, 3m)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validator_SameProductInBothPools_Passes()
        => _validator.Validate(Request(Line(ProductA, 1m, 2m), Line(ProductA, 1m, 3m, StockType.FreeIssue)))
                     .IsValid.Should().BeTrue();

    [Fact]
    public void Validator_TooManyLines_Fails()
    {
        var lines = Enumerable.Range(1, CreateStockAdjustmentValidator.MaxLines + 1)
            .Select(i => Line(i, 0m, 1m))
            .ToArray();

        var result = _validator.Validate(Request(lines));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validator_InvalidReason_Fails()
    {
        var request = Request(Line(ProductA, 1m, 2m)) with { Reason = (StockAdjustmentReason)99 };

        _validator.Validate(request).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validator_ReasonOther_RequiresNotes(string? notes)
    {
        var request = Request(Line(ProductA, 1m, 2m)) with { Reason = StockAdjustmentReason.Other, Notes = notes };

        _validator.Validate(request).Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Validator_ReasonOther_WithNotes_Passes()
    {
        var request = Request(Line(ProductA, 1m, 2m)) with { Reason = StockAdjustmentReason.Other, Notes = "Flood" };

        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
