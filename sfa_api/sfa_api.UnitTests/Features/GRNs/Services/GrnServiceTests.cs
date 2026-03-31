using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.GRNs.Entities;
using sfa_api.Features.GRNs.Enums;
using sfa_api.Features.GRNs.Repositories;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.GRNs.Services;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.GRNs.Services;

public class GrnServiceTests
{
    private readonly Mock<IGrnRepository> _repoMock;
    private readonly Mock<IDistributedLockService> _lockServiceMock;
    private readonly Mock<IDbContextTransaction> _txMock;
    private readonly Mock<IAsyncDisposable> _lockMock;
    private readonly AppDbContext _dbContext;
    private readonly GrnService _sut;

    private const int CallerId      = 42;
    private const int SalesInvoiceId = 1;
    private const int GrnId         = 7;
    private const int DistributorId  = 10;
    private const int ProductId      = 5;

    public GrnServiceTests()
    {
        _repoMock        = new Mock<IGrnRepository>();
        _lockServiceMock = new Mock<IDistributedLockService>();
        _txMock          = new Mock<IDbContextTransaction>();
        _lockMock        = new Mock<IAsyncDisposable>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        // EnsureCreated() omitted — SQLite has no sequences; context is used only for
        // CreateExecutionStrategy() which works without schema creation.

        _sut = new GrnService(_repoMock.Object, _lockServiceMock.Object, _dbContext);

        // Default: lock is acquired successfully (individual tests can override to return null)
        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Default: transaction commits/rolls back cleanly
        _txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // ── Factory helpers ────────────────────────────────────────────────────

    private static SalesInvoice PendingInvoice(int id = SalesInvoiceId) => new()
    {
        Id           = id,
        VchBillNo    = $"BIS/25/{id:D4}",
        DistributorId = DistributorId,
        Status       = SalesInvoiceStatus.Pending,
        IsActive     = true,
        Items        = new List<SalesInvoiceItem>
        {
            new()
            {
                Id          = 1,
                ProductId   = ProductId,
                Quantity    = 10m,
                Unit        = "CTN",
                ItemErpCode = "CF01",
                ItemDescription = "Test product",
                UnitPrice   = 100m,
                TotalPrice  = 1000m,
                LineNumber  = 1
            }
        }
    };

    private static GRN PendingGrn(int id = GrnId) => new()
    {
        Id            = id,
        GrnNumber     = $"GRN-2026-{id:D5}",
        SalesInvoiceId = SalesInvoiceId,
        DistributorId  = DistributorId,
        Status         = GrnStatus.Pending,
        IsActive       = true,
        Items          = new List<GRNItem>
        {
            new()
            {
                Id        = 1,
                ProductId = ProductId,
                Quantity  = 10m,
                Unit      = "CTN"
            }
        }
    };

    private GRN ConfirmedGrnSnapshot() => new()
    {
        Id             = GrnId,
        GrnNumber      = "GRN-2026-00007",
        SalesInvoiceId = SalesInvoiceId,
        DistributorId  = DistributorId,
        Status         = GrnStatus.Confirmed,
        ConfirmedBy    = CallerId,
        ReceivedAt     = DateTime.UtcNow,
        IsActive       = true,
        Items          = new List<GRNItem>
        {
            new() { Id = 1, ProductId = ProductId, Quantity = 10m, Unit = "CTN" }
        }
    };

    // ────────────────────────────────────────────────────────────────────────
    // CreateAsync
    // ────────────────────────────────────────────────────────────────────────

    private void SetupCreateHappyPath(SalesInvoice invoice)
    {
        _repoMock
            .Setup(r => r.GetSalesInvoiceWithItemsAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        _repoMock
            .Setup(r => r.GrnExistsForInvoiceAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repoMock
            .Setup(r => r.GetNextGrnNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _repoMock
            .Setup(r => r.AddGrnAsync(It.IsAny<GRN>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PendingGrn());
    }

    [Fact]
    public async Task CreateAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        _repoMock
            .Setup(r => r.GetSalesInvoiceWithItemsAsync(SalesInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesInvoice?)null);

        var act = () => _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*SalesInvoice*");
    }

    [Fact]
    public async Task CreateAsync_InvoiceStatusIsGrnReceived_ThrowsBusinessRuleException()
    {
        var invoice = PendingInvoice();
        invoice.Status = SalesInvoiceStatus.GrnReceived;

        _repoMock
            .Setup(r => r.GetSalesInvoiceWithItemsAsync(SalesInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var act = () => _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("GRN_INVOICE_NOT_PENDING");
    }

    [Fact]
    public async Task CreateAsync_GrnAlreadyExistsForInvoice_ThrowsDuplicateResourceException()
    {
        var invoice = PendingInvoice();

        _repoMock
            .Setup(r => r.GetSalesInvoiceWithItemsAsync(SalesInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        _repoMock
            .Setup(r => r.GrnExistsForInvoiceAsync(SalesInvoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        await act.Should().ThrowAsync<DuplicateResourceException>();
    }

    [Fact]
    public async Task CreateAsync_ValidPendingInvoice_CreatesGrnWithCorrectGrnNumber()
    {
        var invoice = PendingInvoice();
        SetupCreateHappyPath(invoice);

        GRN? capturedGrn = null;
        _repoMock
            .Setup(r => r.AddGrnAsync(It.IsAny<GRN>(), It.IsAny<CancellationToken>()))
            .Callback<GRN, CancellationToken>((grn, _) => capturedGrn = grn)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        capturedGrn.Should().NotBeNull();
        capturedGrn!.GrnNumber.Should().MatchRegex(@"^GRN-\d{4}-\d{5}$");
    }

    [Fact]
    public async Task CreateAsync_ValidPendingInvoice_SetsDistributorIdFromInvoice()
    {
        var invoice = PendingInvoice();
        SetupCreateHappyPath(invoice);

        GRN? capturedGrn = null;
        _repoMock
            .Setup(r => r.AddGrnAsync(It.IsAny<GRN>(), It.IsAny<CancellationToken>()))
            .Callback<GRN, CancellationToken>((grn, _) => capturedGrn = grn)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        capturedGrn!.DistributorId.Should().Be(DistributorId);
        capturedGrn.SalesInvoiceId.Should().Be(SalesInvoiceId);
    }

    [Fact]
    public async Task CreateAsync_ValidPendingInvoice_CopiesInvoiceItemsAsGrnItems()
    {
        var invoice = PendingInvoice();
        SetupCreateHappyPath(invoice);

        GRN? capturedGrn = null;
        _repoMock
            .Setup(r => r.AddGrnAsync(It.IsAny<GRN>(), It.IsAny<CancellationToken>()))
            .Callback<GRN, CancellationToken>((grn, _) => capturedGrn = grn)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        capturedGrn!.Items.Should().HaveCount(invoice.Items.Count);
        capturedGrn.Items.First().ProductId.Should().Be(ProductId);
        capturedGrn.Items.First().Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task CreateAsync_ValidPendingInvoice_MarksInvoiceAsGrnReceived()
    {
        var invoice = PendingInvoice();
        SetupCreateHappyPath(invoice);

        await _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        invoice.Status.Should().Be(SalesInvoiceStatus.GrnReceived);
    }

    [Fact]
    public async Task CreateAsync_ValidPendingInvoice_CallsSaveChangesAsync()
    {
        var invoice = PendingInvoice();
        SetupCreateHappyPath(invoice);

        await _sut.CreateAsync(new CreateGrnRequest(SalesInvoiceId), CallerId);

        _repoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // ────────────────────────────────────────────────────────────────────────
    // ConfirmAsync
    // ────────────────────────────────────────────────────────────────────────

    private void SetupConfirmHappyPath(GRN grn, DistributorStock? existingStock = null)
    {
        // Advisory lock — acquired successfully
        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Transaction
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        // GRN load
        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(grn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grn);

        // Stock lock
        _repoMock
            .Setup(r => r.GetStockForUpdateAsync(DistributorId, ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        _repoMock
            .Setup(r => r.AddStockAsync(It.IsAny<DistributorStock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.AddStockTransactionAsync(It.IsAny<sfa_api.Features.Stock.Entities.StockTransaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupConfirmReloadAfterCommit(GRN grn)
    {
        // After the transaction completes, GetGrnWithItemsAsync is called again to reload the DTO.
        // We use a sequence: first call returns the pending grn (before update), second returns confirmed.
        int callCount = 0;
        var confirmedSnapshot = ConfirmedGrnSnapshot();
        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(grn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call: inside ConfirmAsync to load grn; second call: reload after commit
                return callCount == 1 ? grn : confirmedSnapshot;
            });
    }

    [Fact]
    public async Task ConfirmAsync_LockNotAcquired_ThrowsConcurrencyConflictException()
    {
        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var act = () => _sut.ConfirmAsync(GrnId, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task ConfirmAsync_GrnNotFound_ThrowsNotFoundException()
    {
        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(GrnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GRN?)null);

        var act = () => _sut.ConfirmAsync(GrnId, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*GRN*");
    }

    [Fact]
    public async Task ConfirmAsync_GrnAlreadyConfirmed_ThrowsBusinessRuleException()
    {
        var grn = PendingGrn();
        grn.Status = GrnStatus.Confirmed;

        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(GrnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grn);

        var act = () => _sut.ConfirmAsync(GrnId, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("GRN_NOT_PENDING");
    }

    [Fact]
    public async Task ConfirmAsync_ValidPendingGrn_SetsGrnStatusToConfirmed()
    {
        var grn = PendingGrn();
        SetupConfirmHappyPath(grn);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        grn.Status.Should().Be(GrnStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmAsync_ValidPendingGrn_SetsConfirmedByToCallerId()
    {
        var grn = PendingGrn();
        SetupConfirmHappyPath(grn);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        grn.ConfirmedBy.Should().Be(CallerId);
    }

    [Fact]
    public async Task ConfirmAsync_ValidPendingGrn_SetsReceivedAtFromRequest()
    {
        var grn = PendingGrn();
        SetupConfirmHappyPath(grn);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        var receivedAt = new DateTime(2026, 3, 24, 9, 0, 0, DateTimeKind.Utc);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(receivedAt), CallerId);

        grn.ReceivedAt.Should().Be(receivedAt);
    }

    [Fact]
    public async Task ConfirmAsync_ExistingStock_UpdatesQuantityOnHandCorrectly()
    {
        var grn = PendingGrn();
        var existingStock = new DistributorStock
        {
            Id             = 1,
            DistributorId  = DistributorId,
            ProductId      = ProductId,
            QuantityOnHand = 50m,
            LastUpdatedAt  = DateTime.UtcNow
        };
        SetupConfirmHappyPath(grn, existingStock);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        // GRN item has Quantity = 10m; existing stock was 50m; expected after = 60m
        existingStock.QuantityOnHand.Should().Be(60m);
    }

    [Fact]
    public async Task ConfirmAsync_NewStockEntry_CreatesDistributorStockAndAddsStockTransaction()
    {
        var grn = PendingGrn();
        SetupConfirmHappyPath(grn, existingStock: null);

        // After the first AddStockAsync + SaveChanges, the service re-locks the new row.
        // We simulate that the re-lock returns the newly created row.
        DistributorStock? capturedStock = null;
        _repoMock
            .Setup(r => r.AddStockAsync(It.IsAny<DistributorStock>(), It.IsAny<CancellationToken>()))
            .Callback<DistributorStock, CancellationToken>((s, _) => capturedStock = s)
            .Returns(Task.CompletedTask);

        // Re-lock after create returns the same stock object (simulates the re-fetch after flush)
        int stockLockCallCount = 0;
        _repoMock
            .Setup(r => r.GetStockForUpdateAsync(DistributorId, ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                stockLockCallCount++;
                return stockLockCallCount == 1 ? null : capturedStock;
            });

        // Capture the stock transaction to verify QuantityBefore = 0 (initial stock)
        sfa_api.Features.Stock.Entities.StockTransaction? capturedTx = null;
        _repoMock
            .Setup(r => r.AddStockTransactionAsync(It.IsAny<sfa_api.Features.Stock.Entities.StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<sfa_api.Features.Stock.Entities.StockTransaction, CancellationToken>((tx, _) => capturedTx = tx)
            .Returns(Task.CompletedTask);

        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        // AddStockAsync must have been called (new stock row created)
        _repoMock.Verify(
            r => r.AddStockAsync(It.IsAny<DistributorStock>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // The stock transaction records QuantityBefore = 0 — confirming initial stock was zero
        capturedTx.Should().NotBeNull();
        capturedTx!.QuantityBefore.Should().Be(0m);
        capturedTx.QuantityAfter.Should().Be(10m); // GRN item quantity = 10
    }

    [Fact]
    public async Task ConfirmAsync_ValidGrn_AddsOneStockTransactionPerItem()
    {
        var grn = PendingGrn();
        var existingStock = new DistributorStock
        {
            DistributorId  = DistributorId,
            ProductId      = ProductId,
            QuantityOnHand = 0m,
            LastUpdatedAt  = DateTime.UtcNow
        };
        SetupConfirmHappyPath(grn, existingStock);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        _repoMock.Verify(
            r => r.AddStockTransactionAsync(It.IsAny<sfa_api.Features.Stock.Entities.StockTransaction>(), It.IsAny<CancellationToken>()),
            Times.Exactly(grn.Items.Count));
    }

    [Fact]
    public async Task ConfirmAsync_ValidGrn_CommitsTransaction()
    {
        var grn = PendingGrn();
        var existingStock = new DistributorStock
        {
            DistributorId  = DistributorId,
            ProductId      = ProductId,
            QuantityOnHand = 0m,
            LastUpdatedAt  = DateTime.UtcNow
        };
        SetupConfirmHappyPath(grn, existingStock);
        SetupConfirmReloadAfterCommit(grn);
        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        await _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_ExceptionDuringStockUpdate_RollsBackTransaction()
    {
        var grn = PendingGrn();

        _lockServiceMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockMock.Object);
        _lockMock.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _repoMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txMock.Object);

        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(grn.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grn);

        // Simulate a failure inside the stock update loop
        _repoMock
            .Setup(r => r.GetStockForUpdateAsync(DistributorId, ProductId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        var act = () => _sut.ConfirmAsync(grn.Id, new ConfirmGrnRequest(DateTime.UtcNow), CallerId);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ────────────────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_GrnFound_ReturnsDto()
    {
        var grn = PendingGrn();
        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(GrnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grn);

        var result = await _sut.GetByIdAsync(GrnId);

        result.Should().NotBeNull();
        result.Id.Should().Be(GrnId);
    }

    [Fact]
    public async Task GetByIdAsync_GrnNotFound_ThrowsNotFoundException()
    {
        _repoMock
            .Setup(r => r.GetGrnWithItemsAsync(GrnId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GRN?)null);

        var act = () => _sut.GetByIdAsync(GrnId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*GRN*");
    }
}
