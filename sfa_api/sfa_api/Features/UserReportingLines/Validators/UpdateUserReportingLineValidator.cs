using FluentValidation;
using sfa_api.Features.UserReportingLines.Requests;

namespace sfa_api.Features.UserReportingLines.Validators;

public class UpdateUserReportingLineValidator : AbstractValidator<UpdateUserReportingLineRequest>
{
    public UpdateUserReportingLineValidator()
    {
        RuleFor(x => x.ReportsToUserId)
            .GreaterThan(0).WithMessage("Reporting manager is required.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");
    }
}
