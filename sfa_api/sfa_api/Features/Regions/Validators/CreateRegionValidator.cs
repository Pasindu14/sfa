using FluentValidation;
using sfa_api.Features.Regions.Requests;

namespace sfa_api.Features.Regions.Validators;

public class CreateRegionValidator : AbstractValidator<CreateRegionRequest>
{
    public CreateRegionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
