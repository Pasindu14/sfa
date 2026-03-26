using FluentValidation;
using sfa_api.Features.UserReportingLines.Requests;

namespace sfa_api.Features.UserReportingLines.Validators;

public class CreateUserReportingLineValidator : AbstractValidator<CreateUserReportingLineRequest>
{
    public CreateUserReportingLineValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User is required.");

        RuleFor(x => x.ReportsToUserId)
            .GreaterThan(0).WithMessage("Reporting manager is required.")
            .NotEqual(x => x.UserId).WithMessage("A user cannot report to themselves.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");
    }
}
