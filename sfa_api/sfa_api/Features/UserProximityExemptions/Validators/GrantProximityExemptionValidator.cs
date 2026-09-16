using FluentValidation;
using Microsoft.Extensions.Options;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.UserProximityExemptions.Requests;

namespace sfa_api.Features.UserProximityExemptions.Validators;

public class GrantProximityExemptionValidator : AbstractValidator<GrantProximityExemptionRequest>
{
    public GrantProximityExemptionValidator(IOptions<BillingGeoOptions> geoOptions)
    {
        var maxDays = geoOptions.Value.MaxExemptionDays;

        // Business dates, so "today" is the Sri Lanka day — an admin granting at
        // 01:00 Colombo must not be told today is yesterday.
        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(_ => SriLankaTime.Today)
                .WithMessage("The exemption must run through today or a later date.")
            .LessThanOrEqualTo(_ => SriLankaTime.Today.AddDays(maxDays))
                .WithMessage($"An exemption may not exceed {maxDays} days — grant a new one to extend it.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason code is required.")
            .Must(r => Enum.TryParse<ProximityExemptionReason>(r, ignoreCase: false, out _))
                .WithMessage("Reason must be one of: "
                    + string.Join(", ", Enum.GetNames<ProximityExemptionReason>()) + ".");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes may not exceed 500 characters.");
    }
}

public class RevokeProximityExemptionValidator : AbstractValidator<RevokeProximityExemptionRequest>
{
    public RevokeProximityExemptionValidator()
    {
        RuleFor(x => x.RowVersion)
            .GreaterThan(0u).WithMessage("RowVersion is required to revoke an exemption.");
    }
}
