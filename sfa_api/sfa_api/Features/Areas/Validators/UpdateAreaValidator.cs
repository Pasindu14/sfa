using FluentValidation;
using sfa_api.Features.Areas.Requests;

namespace sfa_api.Features.Areas.Validators;

public class UpdateAreaValidator : AbstractValidator<UpdateAreaRequest>
{
    public UpdateAreaValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.RegionId)
            .GreaterThan(0).WithMessage("RegionId must be a valid region.");

        RuleFor(x => x.RowVersion)
            .NotEqual(0u).WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
