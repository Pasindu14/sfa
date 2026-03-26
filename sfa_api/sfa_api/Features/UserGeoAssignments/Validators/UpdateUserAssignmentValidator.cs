using FluentValidation;
using sfa_api.Features.UserGeoAssignments.Requests;

namespace sfa_api.Features.UserGeoAssignments.Validators;

public class UpdateUserAssignmentValidator : AbstractValidator<UpdateUserAssignmentRequest>
{
    public UpdateUserAssignmentValidator()
    {
        RuleFor(x => x.ReportsToUserId)
            .GreaterThan(0).WithMessage("ReportsToUserId must be a valid user ID.");

        RuleFor(x => x.DivisionId)
            .GreaterThan(0).WithMessage("DivisionId must be a valid division ID.")
            .When(x => x.DivisionId.HasValue);

        RuleFor(x => x.EffectiveFrom)
            .NotEqual(DateOnly.MinValue).WithMessage("EffectiveFrom is required.");
    }
}
