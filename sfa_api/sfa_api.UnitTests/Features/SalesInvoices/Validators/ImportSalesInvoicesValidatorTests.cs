using FluentAssertions;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.SalesInvoices.Validators;

namespace sfa_api.UnitTests.Features.SalesInvoices.Validators;

public class ImportSalesInvoicesValidatorTests
{
    private readonly ImportSalesInvoicesValidator _validator = new();

    private static ImportSalesInvoiceItemRequest ValidItem() => new(
        ItemErpCode:     "CF01",
        ItemDescription: "Test product",
        Quantity:        5m,
        Unit:            "CTN",
        UnitPrice:       100m,
        TotalPrice:      500m,
        IsFreeIssue:     false,
        LineNumber:      1
    );

    private static ImportSalesInvoiceRequest ValidInvoice() => new(
        VchBillNo:          "BIS/25/0001",
        BusyOrderRequestNo: null,
        SfaPoNumber:        null,
        DistributorAlias:   1001,
        InvoiceDate:        new DateOnly(2026, 1, 15),
        InvoiceType:        "Regular",
        TotalAmount:        500m,
        Items:              [ValidItem()]
    );

    private static ImportSalesInvoicesRequest ValidRequest() => new(
        FileName: "import_2026_01.xlsx",
        Invoices: [ValidInvoice()]
    );

    // ── FileName ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyFileName_Fails()
    {
        var request = ValidRequest() with { FileName = "" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_FileNameExceeds500Chars_Fails()
    {
        var request = ValidRequest() with { FileName = new string('x', 501) };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    // ── Invoices list ─────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyInvoicesList_Fails()
    {
        var request = ValidRequest() with { Invoices = [] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Invoices");
    }

    // ── Invoice-level rules ───────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyVchBillNo_Fails()
    {
        var inv = ValidInvoice() with { VchBillNo = "" };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DistributorAliasZero_Fails()
    {
        var inv = ValidInvoice() with { DistributorAlias = 0 };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_InvalidInvoiceType_Fails()
    {
        var inv = ValidInvoice() with { InvoiceType = "Unknown" };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Regular")]
    [InlineData("FreeIssue")]
    public async Task Validate_ValidInvoiceType_Passes(string invoiceType)
    {
        var inv = ValidInvoice() with { InvoiceType = invoiceType };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyItemsList_Fails()
    {
        var inv = ValidInvoice() with { Items = [] };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ── Item-level rules ──────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ItemQuantityZero_Fails()
    {
        var item = ValidItem() with { Quantity = 0m };
        var inv = ValidInvoice() with { Items = [item] };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ItemLineNumberZero_Fails()
    {
        var item = ValidItem() with { LineNumber = 0 };
        var inv = ValidInvoice() with { Items = [item] };
        var request = ValidRequest() with { Invoices = [inv] };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        result.IsValid.Should().BeTrue();
    }
}
