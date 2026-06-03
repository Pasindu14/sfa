using FluentValidation;
using sfa_api.Features.StockTaking.Requests;

namespace sfa_api.Features.StockTaking.Validators;

public class UpsertSubmissionValidator : AbstractValidator<UpsertSubmissionRequest>
{
    private static readonly string[] ValidStockTypes = ["Normal", "FreeIssue"];

    public UpsertSubmissionValidator()
    {
        RuleFor(x => x.PeriodId)
            .GreaterThan(0).WithMessage("PeriodId is required.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithMessage("ProductId is required.");

            line.RuleFor(l => l.StockType)
                .NotEmpty().WithMessage("StockType is required.")
                .Must(t => ValidStockTypes.Contains(t))
                .WithMessage("StockType must be 'Normal' or 'FreeIssue'.");

            line.RuleFor(l => l.CountedQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("CountedQuantity cannot be negative.");
        });
    }
}
