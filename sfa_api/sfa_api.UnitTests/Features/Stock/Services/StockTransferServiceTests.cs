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

public class StockTransferServiceTests
{
    private readonly Mock<IStockTransferRepository> _repoMock      = new();
    private readonly Mock<IStockRepository>         _stockRepoMock = new();
    private readonly Mock<IDistributedLockService>  _lockServiceMock = new();
    private readonly Mock<IDbContextTransaction>    _txMock        = new();
    private readonly Mock<IAsyncDisposable>         _lockMock      = new();
    private readonly AppDbContext _dbContext;
    private readonly StockTransferService _sut;

    private const int CallerId      = 42;
    private const int SourceId      = 10;
    private const int TargetId      = 20;
    private const int ProductA      = 5;
    private const int ProductB      = 6;
    private const int NewTransferId = 123;

    public StockTransferServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        // EnsureCreated() omitted — the context is used only for CreateExecutionStrategy().

        _sut = new StockTransferService(_repoMock.Object, _stockRepoMock.Object, _lockServiceMock.Object, _dbContext);

        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _repoMock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_txMock.Object);

        SetupDistributor(SourceId, "Closed DB", isActive: false);
        SetupDistributor(TargetId, "New DB", isActive: true);

        _repoMock
            .Setup(r => r.GetProductsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Product>
            {
                [ProductA] = new() { Id = ProductA, Code = "PA", ItemDescription = "Product A", PiecesPerPack = 12 },
                [ProductB] = new() { Id = ProductB, Code = "PB", ItemDescription = "Product B", PiecesPerPack = 24 },
            });

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new StockTransferDto(
                id, $"ST-{id:D6}", SourceId, "Closed DB", TargetId, "New DB", null, "Admin",
                DateTime.UtcNow, 0, 0m, []));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SetupDistributor(int id, string name, bool isActive) =>
        _repoMock
            .Setup(r => r.GetDistributorAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Distributor { Id = id, Name = name, IsActive = isActive });

    private void SetupSourceStock(params (int ProductId, StockType Type, decimal Qty)[] rows) =>
        _stockRepoMock
            .Setup(r => r.LockStocksForUpdateAsync(It.IsAny<IEnumerable<StockKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToDictionary(
                r => new StockKey(SourceId, r.ProductId, r.Type),
                r => new DistributorStock
                {
                    DistributorId = SourceId, ProductId = r.ProductId, StockType = r.Type, QuantityOnHand = r.Qty,
                }));

    /// <summary>Captures the added header and assigns its Id on the first save, as the DB would.</summary>
    private Func<StockTransfer?> CaptureInsert()
    {
        StockTransfer? captured = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransfer, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);
        _repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { if (captured is not null && captured.Id == 0) captured.Id = NewTransferId; })
            .Returns(Task.CompletedTask);
        return () => captured;
    }

    private static CreateStockTransferRequest Request(
        int source = SourceId, int target = TargetId, params CreateStockTransferLineRequest[] lines) =>
        new(source, target, "Area closed", lines.Length > 0
            ? lines.ToList()
            : [new CreateStockTransferLineRequest(ProductA, StockType.Normal, 10m)]);

    // ── Business rules ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ActiveSource_ThrowsSourceDistributorActive()
    {
        SetupDistributor(SourceId, "Still Open DB", isActive: true);

        var act = () => _sut.CreateAsync(Request(), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("SOURCE_DISTRIBUTOR_ACTIVE");
        _repoMock.Verify(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InactiveTarget_ThrowsTargetDistributorInactive()
    {
        SetupDistributor(TargetId, "Closed Target", isActive: false);

        var act = () => _sut.CreateAsync(Request(), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("TARGET_DISTRIBUTOR_INACTIVE");
        _stockRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_SameSourceAndTarget_ThrowsSameDistributor()
    {
        var act = () => _sut.CreateAsync(Request(SourceId, SourceId), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("SAME_DISTRIBUTOR");
        _lockServiceMock.Verify(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnknownSource_ThrowsDistributorNotFound()
    {
        _repoMock
            .Setup(r => r.GetDistributorAsync(SourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Distributor?)null);

        var act = () => _sut.CreateAsync(Request(), CallerId);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("DISTRIBUTOR_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_LockBusy_ThrowsStockTransferInProgress()
    {
        _lockServiceMock
            .Setup(l => l.AcquireAsync($"stock-transfer:{SourceId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var act = () => _sut.CreateAsync(Request(), CallerId);

        var ex = await act.Should().ThrowAsync<StockTransferInProgressException>();
        ex.Which.ErrorCode.Should().Be("STOCK_TRANSFER_IN_PROGRESS");
        _repoMock.Verify(r => r.GetDistributorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_QuantityAboveBalance_ThrowsInsufficientStock_AndRollsBack()
    {
        SetupSourceStock((ProductA, StockType.Normal, 8m));
        CaptureInsert();

        var act = () => _sut.CreateAsync(Request(lines: new CreateStockTransferLineRequest(ProductA, StockType.Normal, 10m)), CallerId);

        var ex = await act.Should().ThrowAsync<InsufficientStockException>();
        ex.Which.ErrorCode.Should().Be("INSUFFICIENT_STOCK");
        ex.Which.Shortages.Should().ContainSingle(s => s.ProductId == ProductA && s.Requested == 10m && s.Available == 8m);
        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<StockTransfer>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockRepoMock.Verify(r => r.DeductStockAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockType>(),
            It.IsAny<StockTransactionType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NoSourceStockRow_ThrowsInsufficientStock()
    {
        SetupSourceStock();   // source holds nothing for this product/pool

        var act = () => _sut.CreateAsync(Request(lines: new CreateStockTransferLineRequest(ProductA, StockType.FreeIssue, 1m)), CallerId);

        var ex = await act.Should().ThrowAsync<InsufficientStockException>();
        ex.Which.Shortages.Should().ContainSingle(s => s.Available == 0m);
    }

    // ── Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Success_WritesTransferOutAndInLedgerRows_WithReferences()
    {
        SetupSourceStock((ProductA, StockType.Normal, 50m), (ProductB, StockType.FreeIssue, 7m));
        var captured = CaptureInsert();

        var request = Request(lines:
        [
            new CreateStockTransferLineRequest(ProductA, StockType.Normal, 30m),
            new CreateStockTransferLineRequest(ProductB, StockType.FreeIssue, 7m),
        ]);

        var result = await _sut.CreateAsync(request, CallerId);

        result.Id.Should().Be(NewTransferId);

        var transfer = captured();
        transfer.Should().NotBeNull();
        transfer!.TransferNumber.Should().Be("ST-000123");
        transfer.SourceDistributorId.Should().Be(SourceId);
        transfer.TargetDistributorId.Should().Be(TargetId);
        transfer.TransferredBy.Should().Be(CallerId);
        transfer.Lines.Should().HaveCount(2);

        foreach (var (productId, type, qty) in new[] { (ProductA, StockType.Normal, 30m), (ProductB, StockType.FreeIssue, 7m) })
        {
            _stockRepoMock.Verify(r => r.DeductStockAsync(
                SourceId, productId, qty, type, StockTransactionType.TransferOut,
                "StockTransfer", NewTransferId, CallerId,
                It.Is<string?>(n => n!.Contains("New DB") && n.Contains("ST-000123")),
                It.IsAny<CancellationToken>()), Times.Once);

            _stockRepoMock.Verify(r => r.CreditStockAsync(
                TargetId, productId, qty, type, StockTransactionType.TransferIn,
                "StockTransfer", NewTransferId, CallerId,
                It.Is<string?>(n => n!.Contains("Closed DB") && n.Contains("ST-000123")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // Source and target rows are locked together in one call.
        _stockRepoMock.Verify(r => r.LockStocksForUpdateAsync(
            It.Is<IEnumerable<StockKey>>(k =>
                k.Contains(new StockKey(SourceId, ProductA, StockType.Normal)) &&
                k.Contains(new StockKey(TargetId, ProductA, StockType.Normal)) &&
                k.Contains(new StockKey(SourceId, ProductB, StockType.FreeIssue)) &&
                k.Contains(new StockKey(TargetId, ProductB, StockType.FreeIssue))),
            It.IsAny<CancellationToken>()), Times.Once);

        _stockRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ThrowsNotFound()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransferDto?)null);

        var act = () => _sut.GetByIdAsync(99);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.ErrorCode.Should().Be("STOCKTRANSFER_NOT_FOUND");
    }

    // ── Validator ──────────────────────────────────────────────────────────

    private readonly CreateStockTransferValidator _validator = new();

    [Fact]
    public void Validator_SameDistributor_Fails()
    {
        var result = _validator.Validate(Request(SourceId, SourceId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetDistributorId" && e.ErrorCode == "SAME_DISTRIBUTOR");
    }

    [Fact]
    public void Validator_EmptyLines_Fails()
    {
        var result = _validator.Validate(new CreateStockTransferRequest(SourceId, TargetId, null, []));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validator_NonPositiveQuantity_Fails()
    {
        var result = _validator.Validate(Request(lines: new CreateStockTransferLineRequest(ProductA, StockType.Normal, 0m)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines[0].Quantity");
    }

    [Fact]
    public void Validator_DuplicateProductAndStockType_Fails()
    {
        var result = _validator.Validate(Request(lines:
        [
            new CreateStockTransferLineRequest(ProductA, StockType.Normal, 1m),
            new CreateStockTransferLineRequest(ProductA, StockType.Normal, 2m),
        ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Validator_SameProductInBothPools_Passes()
    {
        var result = _validator.Validate(Request(lines:
        [
            new CreateStockTransferLineRequest(ProductA, StockType.Normal, 1m),
            new CreateStockTransferLineRequest(ProductA, StockType.FreeIssue, 2m),
        ]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_TooManyLines_Fails()
    {
        var lines = Enumerable.Range(1, CreateStockTransferValidator.MaxLines + 1)
            .Select(i => new CreateStockTransferLineRequest(i, StockType.Normal, 1m))
            .ToArray();

        var result = _validator.Validate(Request(lines: lines));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }
}
