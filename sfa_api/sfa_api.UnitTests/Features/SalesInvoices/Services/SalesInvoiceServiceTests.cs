using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.SalesInvoices.DTOs;
using sfa_api.Features.SalesInvoices.Entities;
using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.SalesInvoices.Repositories;
using sfa_api.Features.SalesInvoices.Requests;
using Microsoft.Extensions.Logging.Abstractions;
using sfa_api.Features.SalesInvoices.Services;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.SalesInvoices.Services;

public class SalesInvoiceServiceTests
{
    private readonly Mock<ISalesInvoiceRepository> _repoMock;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserGeoAssignmentRepository> _geoRepoMock = new();
    private readonly Mock<IDistributorRepository> _distributorRepoMock = new();
    private readonly AppDbContext _dbContext;
    private readonly SalesInvoiceService _sut;

    private const int CallerId       = 42;
    private const int DistributorId  = 10;
    private const int DistributorAlias = 1001;
    private const int ProductId      = 5;
    private const string ErpCode     = "CF01";
    private const string VchBillNo   = "BIS/25/0001";
    private const string FileName    = "import_2026_01.xlsx";

    public SalesInvoiceServiceTests()
    {
        _repoMock = new Mock<ISalesInvoiceRepository>();

        // SQLite in-memory context — used only for CreateExecutionStrategy() +
        // BeginTransactionAsync() that wrap the import; all data access is via the mock.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();

        _sut = new SalesInvoiceService(
            _repoMock.Object, _dbContext,
            _userRepoMock.Object, _geoRepoMock.Object, _distributorRepoMock.Object,
            NullLogger<SalesInvoiceService>.Instance);
    }

    // ── Factory helpers ────────────────────────────────────────────────────

    private static ImportSalesInvoiceItemRequest ValidItem(string erpCode = ErpCode) => new(
        ItemErpCode:    erpCode,
        ItemDescription: "Test product description",
        Quantity:       10m,
        Unit:           "CTN",
        UnitPrice:      100m,
        TotalPrice:     1000m,
        IsFreeIssue:    false,
        LineNumber:     1
    );

    private static ImportSalesInvoiceRequest ValidInvoice(
        string vchBillNo      = VchBillNo,
        int distributorAlias  = DistributorAlias,
        string? sfaPoNumber   = null,
        string erpCode        = ErpCode) => new(
        VchBillNo:          vchBillNo,
        BusyOrderRequestNo: null,
        SfaPoNumber:        sfaPoNumber,
        DistributorAlias:   distributorAlias,
        InvoiceDate:        new DateOnly(2026, 1, 15),
        InvoiceType:        "Regular",
        TotalAmount:        1000m,
        Items:              [ValidItem(erpCode)]
    );

    private static ImportSalesInvoicesRequest SingleInvoiceRequest(
        ImportSalesInvoiceRequest? invoice = null) => new(
        FileName: FileName,
        Invoices: [invoice ?? ValidInvoice()]
    );

    /// <summary>Sets up the four lookup dictionaries with one valid mapping each.</summary>
    private void SetupHappyPathLookups(
        string? existingVchBillNo = null,
        int? purchaseOrderId = null,
        string? sfaPoNumber = "PO-2026-00001")
    {
        _repoMock
            .Setup(r => r.GetNextBatchNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        _repoMock
            .Setup(r => r.AddBatchAsync(It.IsAny<SalesInvoiceImportBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repoMock
            .Setup(r => r.GetDistributorAliasDictionaryAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [DistributorAlias] = DistributorId });

        _repoMock
            .Setup(r => r.GetProductErpCodeDictionaryAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { [ErpCode] = ProductId });

        var poMap = new Dictionary<string, (int Id, PurchaseOrderStatus Status)>();
        if (purchaseOrderId.HasValue && sfaPoNumber is not null)
            poMap[sfaPoNumber] = (purchaseOrderId.Value, PurchaseOrderStatus.Finalized);

        _repoMock
            .Setup(r => r.GetPurchaseOrderNumberDictionaryAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(poMap);

        var existing = new HashSet<string>();
        if (existingVchBillNo is not null)
            existing.Add(existingVchBillNo);

        _repoMock
            .Setup(r => r.GetExistingVchBillNosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.AddInvoicesAsync(It.IsAny<IEnumerable<SalesInvoice>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ── All invoices imported → Completed ─────────────────────────────────

    [Fact]
    public async Task ImportAsync_AllInvoicesValid_ReturnsBatchStatusCompleted()
    {
        SetupHappyPathLookups();
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.Status.Should().Be("Completed");
        result.ImportedInvoices.Should().Be(1);
        result.SkippedInvoices.Should().Be(0);
        result.TotalInvoices.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_AllInvoicesValid_ReturnsTotalAmountSum()
    {
        SetupHappyPathLookups();
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.TotalAmount.Should().Be(1000m);
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_AllInvoicesValid_ReturnsBatchNumberWithImpPrefix()
    {
        SetupHappyPathLookups();
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.BatchNumber.Should().StartWith("IMP-");
        result.BatchNumber.Should().Contain(DateTime.UtcNow.Year.ToString());
    }

    // ── Duplicate VchBillNo already in DB ─────────────────────────────────

    [Fact]
    public async Task ImportAsync_InvoiceAlreadyImported_SkipsInvoiceAndAddsError()
    {
        SetupHappyPathLookups(existingVchBillNo: VchBillNo);
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.SkippedInvoices.Should().Be(1);
        result.ImportedInvoices.Should().Be(0);
        result.Errors.Should().ContainSingle(e =>
            e.VchBillNo == VchBillNo && e.Reason.Contains("Already imported"));
    }

    [Fact]
    public async Task ImportAsync_InvoiceAlreadyImported_ReturnsBatchStatusFailed()
    {
        SetupHappyPathLookups(existingVchBillNo: VchBillNo);
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.Status.Should().Be("Failed");
    }

    // ── Duplicate VchBillNo within same file ──────────────────────────────

    [Fact]
    public async Task ImportAsync_DuplicateVchBillNoWithinSameFile_SecondOccurrenceSkipped()
    {
        SetupHappyPathLookups();
        var request = new ImportSalesInvoicesRequest(
            FileName: FileName,
            Invoices:
            [
                ValidInvoice(vchBillNo: VchBillNo),
                ValidInvoice(vchBillNo: VchBillNo)   // duplicate
            ]
        );

        var result = await _sut.ImportAsync(request, CallerId);

        result.ImportedInvoices.Should().Be(1);
        result.SkippedInvoices.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.VchBillNo == VchBillNo);
    }

    // ── Distributor alias not found ───────────────────────────────────────

    [Fact]
    public async Task ImportAsync_DistributorAliasNotFound_SkipsInvoiceAndAddsError()
    {
        SetupHappyPathLookups();
        // Override alias map to be empty
        _repoMock
            .Setup(r => r.GetDistributorAliasDictionaryAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.ImportedInvoices.Should().Be(0);
        result.SkippedInvoices.Should().Be(1);
        result.Errors.Should().ContainSingle(e =>
            e.VchBillNo == VchBillNo && e.Reason.Contains(DistributorAlias.ToString()));
    }

    // ── Product ERP code not found ────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_ProductErpCodeNotFound_SkipsInvoiceAndAddsError()
    {
        SetupHappyPathLookups();
        _repoMock
            .Setup(r => r.GetProductErpCodeDictionaryAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.ImportedInvoices.Should().Be(0);
        result.SkippedInvoices.Should().Be(1);
        result.Errors.Should().ContainSingle(e =>
            e.VchBillNo == VchBillNo && e.Reason.Contains(ErpCode));
    }

    // ── All skipped → Failed ──────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_AllInvoicesSkipped_ReturnsBatchStatusFailed()
    {
        SetupHappyPathLookups(existingVchBillNo: VchBillNo);
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.Status.Should().Be("Failed");
        result.ImportedInvoices.Should().Be(0);
    }

    // ── Some skipped → PartialFailed ─────────────────────────────────────

    [Fact]
    public async Task ImportAsync_SomeInvoicesSkipped_ReturnsBatchStatusPartialFailed()
    {
        SetupHappyPathLookups(existingVchBillNo: VchBillNo);
        // Second invoice has a different VchBillNo, not in existing set
        var request = new ImportSalesInvoicesRequest(
            FileName: FileName,
            Invoices:
            [
                ValidInvoice(vchBillNo: VchBillNo),               // skipped — already imported
                ValidInvoice(vchBillNo: "BIS/25/0002")            // succeeds
            ]
        );

        var result = await _sut.ImportAsync(request, CallerId);

        result.Status.Should().Be("PartialFailed");
        result.ImportedInvoices.Should().Be(1);
        result.SkippedInvoices.Should().Be(1);
    }

    // ── PO number resolves → purchaseOrderId set ──────────────────────────

    [Fact]
    public async Task ImportAsync_SfaPoNumberResolved_InvoiceImportedWithPurchaseOrderId()
    {
        const string poNumber = "PO-2026-00001";
        const int poId = 99;
        SetupHappyPathLookups(sfaPoNumber: poNumber, purchaseOrderId: poId);

        SalesInvoice? capturedInvoice = null;
        _repoMock
            .Setup(r => r.AddInvoicesAsync(It.IsAny<IEnumerable<SalesInvoice>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<SalesInvoice>, CancellationToken>((invoices, _) =>
                capturedInvoice = invoices.FirstOrDefault())
            .Returns(Task.CompletedTask);

        var request = SingleInvoiceRequest(ValidInvoice(sfaPoNumber: poNumber));

        await _sut.ImportAsync(request, CallerId);

        capturedInvoice.Should().NotBeNull();
        capturedInvoice!.PurchaseOrderId.Should().Be(poId);
    }

    // ── PO number not found → invoice is skipped with an error ───────────

    [Fact]
    public async Task ImportAsync_SfaPoNumberNotInMap_InvoiceSkippedWithError()
    {
        SetupHappyPathLookups();   // PO map is empty by default

        var request = SingleInvoiceRequest(ValidInvoice(sfaPoNumber: "PO-MISSING"));

        var result = await _sut.ImportAsync(request, CallerId);

        result.ImportedInvoices.Should().Be(0, "invoice with unknown SFA PO number must be skipped");
        result.Errors.Should().HaveCount(1);
    }

    // ── SaveChangesAsync call count ───────────────────────────────────────

    [Fact]
    public async Task ImportAsync_ValidInvoices_CallsSaveChangesThreeTimes()
    {
        // First call: flush batch creation
        // Second call: flush invoices
        // Third call: finalize batch status
        SetupHappyPathLookups();
        var request = SingleInvoiceRequest();

        await _sut.ImportAsync(request, CallerId);

        _repoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ImportAsync_AllInvoicesSkipped_CallsSaveChangesTwice()
    {
        // First call: flush batch; no second call for invoices (skipped); third for finalize
        // But: second flush only happens when invoicesToAdd.Count > 0 → so only twice
        SetupHappyPathLookups(existingVchBillNo: VchBillNo);
        var request = SingleInvoiceRequest();

        await _sut.ImportAsync(request, CallerId);

        _repoMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ── Errors list ───────────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_AllInvoicesValid_ReturnsEmptyErrorsList()
    {
        SetupHappyPathLookups();
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_DistributorNotFound_ErrorListContainsOneEntryWithCorrectVchBillNo()
    {
        SetupHappyPathLookups();
        _repoMock
            .Setup(r => r.GetDistributorAliasDictionaryAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        var request = SingleInvoiceRequest();

        var result = await _sut.ImportAsync(request, CallerId);

        result.Errors.Should().HaveCount(1);
        result.Errors[0].VchBillNo.Should().Be(VchBillNo);
    }

    // ── GetDetailAsync — object-level scoping (audit finding #4) ──────────────

    [Fact]
    public async Task GetDetailAsync_AsDistributor_ForeignInvoice_ThrowsAuthorization()
    {
        _repoMock.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new SalesInvoice { Id = 1, DistributorId = 10 });
        _userRepoMock.Setup(r => r.GetUserByIdAsync(CallerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new User { Id = CallerId, DistributorId = 20 });

        var act = async () => await _sut.GetDetailAsync(1, CallerId, UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>(
            "a distributor must not read another distributor's invoice");
    }

    [Fact]
    public async Task GetDetailAsync_AsDistributor_OwnInvoice_ReturnsIt()
    {
        _repoMock.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new SalesInvoice { Id = 1, DistributorId = 10 });
        _userRepoMock.Setup(r => r.GetUserByIdAsync(CallerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new User { Id = CallerId, DistributorId = 10 });

        var result = await _sut.GetDetailAsync(1, CallerId, UserRole.Distributor);

        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetDetailAsync_AsAdmin_ForeignInvoice_ReturnsIt()
    {
        _repoMock.Setup(r => r.GetDetailAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new SalesInvoice { Id = 1, DistributorId = 10 });

        var result = await _sut.GetDetailAsync(1, CallerId, UserRole.Admin);

        result.Id.Should().Be(1, "Admin reads are unrestricted");
    }
}
