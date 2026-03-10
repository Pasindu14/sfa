using FluentValidation;
using sfa_api.Features.Divisions.Requests;

namespace sfa_api.Features.Divisions.Validators;

public class CreateDivisionValidator : AbstractValidator<CreateDivisionRequest>
{
    public CreateDivisionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.TerritoryId)
            .GreaterThan(0).WithMessage("TerritoryId must be a valid territory.");
    }
}
