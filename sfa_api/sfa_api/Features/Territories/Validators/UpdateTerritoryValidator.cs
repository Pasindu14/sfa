using FluentValidation;
using sfa_api.Features.Territories.Requests;

namespace sfa_api.Features.Territories.Validators;

public class UpdateTerritoryValidator : AbstractValidator<UpdateTerritoryRequest>
{
    public UpdateTerritoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.AreaId)
            .GreaterThan(0).WithMessage("AreaId must be a valid area.");
    }
}
