using FluentValidation;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Validators;

public class BinCardQueryValidator : AbstractValidator<BinCardQuery>
{
    // Cap the window so a single report can't scan an unbounded ledger range.
    private const int MaxRangeDays = 366;

    public BinCardQueryValidator()
    {
        RuleFor(x => x.DistributorId)
            .GreaterThan(0).WithMessage("DistributorId must be a positive integer.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("To date must be on or after the From date.");

        RuleFor(x => x)
            .Must(q => q.To.DayNumber - q.From.DayNumber <= MaxRangeDays)
            .WithMessage($"Date range cannot exceed {MaxRangeDays} days.")
            .When(q => q.To >= q.From);
    }
}
