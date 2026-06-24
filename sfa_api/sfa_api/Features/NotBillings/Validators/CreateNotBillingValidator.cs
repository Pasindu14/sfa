using FluentValidation;
using sfa_api.Common.Extensions;
using sfa_api.Features.NotBillings.Requests;

namespace sfa_api.Features.NotBillings.Validators;

public class CreateNotBillingValidator : AbstractValidator<CreateNotBillingRequest>
{
    public CreateNotBillingValidator()
    {
        RuleFor(x => x.OutletId)
            .GreaterThan(0).WithMessage("OutletId must be a positive integer.");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Reason must be a valid NotBillingReason value.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.NotBillingDate!.Value)
            .LessThanOrEqualTo(_ => SriLankaTime.Today)
                .WithMessage("NotBillingDate cannot be in the future.")
            .GreaterThanOrEqualTo(_ => SriLankaTime.Today.AddDays(-7))
                .WithMessage("NotBillingDate cannot be more than 7 days in the past.")
            .When(x => x.NotBillingDate.HasValue);
    }
}
