using FluentValidation;
using sfa_api.Features.StockTaking.Requests;

namespace sfa_api.Features.StockTaking.Validators;

public class CreatePeriodValidator : AbstractValidator<CreatePeriodRequest>
{
    public CreatePeriodValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");
    }
}
