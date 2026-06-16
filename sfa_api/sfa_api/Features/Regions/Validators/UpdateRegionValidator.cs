using FluentValidation;
using sfa_api.Features.Regions.Requests;

namespace sfa_api.Features.Regions.Validators;

public class UpdateRegionValidator : AbstractValidator<UpdateRegionRequest>
{
    public UpdateRegionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.RowVersion)
            .NotEqual(0u).WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
