using FluentValidation;
using sfa_api.Features.DailyRouteAssignments.Requests;

namespace sfa_api.Features.DailyRouteAssignments.Validators;

public class CreateDailyRouteAssignmentValidator : AbstractValidator<CreateDailyRouteAssignmentRequest>
{
    public CreateDailyRouteAssignmentValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be a valid user ID.");

        RuleFor(x => x.RouteId)
            .GreaterThan(0).WithMessage("RouteId must be a valid route ID.");

        RuleFor(x => x.AssignedDate)
            .NotEqual(DateOnly.MinValue).WithMessage("AssignedDate is required.");
    }
}
