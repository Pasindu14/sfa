using FluentValidation;
using sfa_api.Features.StockTaking.Requests;

namespace sfa_api.Features.StockTaking.Validators;

public class AdjustLineValidator : AbstractValidator<AdjustLineRequest>
{
    public AdjustLineValidator()
    {
        RuleFor(x => x.AdjustedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Adjusted quantity cannot be negative.");
    }
}
