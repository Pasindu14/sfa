using FluentValidation;
using sfa_api.Features.SalesInvoices.Requests;

namespace sfa_api.Features.SalesInvoices.Validators;

public class ImportSalesInvoicesValidator : AbstractValidator<ImportSalesInvoicesRequest>
{
    public ImportSalesInvoicesValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Invoices).NotEmpty().WithMessage("At least one invoice is required.");
        RuleForEach(x => x.Invoices).SetValidator(new ImportSalesInvoiceValidator());
    }
}

public class ImportSalesInvoiceValidator : AbstractValidator<ImportSalesInvoiceRequest>
{
    private static readonly HashSet<string> ValidInvoiceTypes = ["Regular", "FreeIssue"];

    public ImportSalesInvoiceValidator()
    {
        RuleFor(x => x.VchBillNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DistributorAlias).GreaterThan(0);
        RuleFor(x => x.InvoiceType).Must(t => ValidInvoiceTypes.Contains(t))
            .WithMessage("InvoiceType must be 'Regular' or 'FreeIssue'.");
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Invoice must have at least one item.");
        RuleForEach(x => x.Items).SetValidator(new ImportSalesInvoiceItemValidator());
    }
}

public class ImportSalesInvoiceItemValidator : AbstractValidator<ImportSalesInvoiceItemRequest>
{
    public ImportSalesInvoiceItemValidator()
    {
        RuleFor(x => x.ItemErpCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LineNumber).GreaterThan(0);
    }
}
