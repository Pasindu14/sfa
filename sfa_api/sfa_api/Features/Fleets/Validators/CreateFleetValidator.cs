using FluentValidation;
using sfa_api.Features.Fleets.Requests;

namespace sfa_api.Features.Fleets.Validators;

public class CreateFleetValidator : AbstractValidator<CreateFleetRequest>
{
    public CreateFleetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
