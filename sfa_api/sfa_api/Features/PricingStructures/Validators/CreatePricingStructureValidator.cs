using FluentValidation;
using sfa_api.Features.PricingStructures.Requests;

namespace sfa_api.Features.PricingStructures.Validators;

public class CreatePricingStructureValidator : AbstractValidator<CreatePricingStructureRequest>
{
    public CreatePricingStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
