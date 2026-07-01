using FluentValidation;
using sfa_api.Features.Routes.Requests;

namespace sfa_api.Features.Routes.Validators;

public class CreateRouteValidator : AbstractValidator<CreateRouteRequest>
{
    public CreateRouteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        // Optional: a unique colour is auto-assigned server-side when blank or duplicate.
        RuleFor(x => x.PinColor)
            .MaximumLength(50).WithMessage("PinColor must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.DivisionId)
            .GreaterThan(0).WithMessage("DivisionId must be a valid division.");
    }
}
