using FluentValidation;
using sfa_api.Features.RouteUnlockRequests.Requests;

namespace sfa_api.Features.RouteUnlockRequests.Validators;

public class CreateRouteUnlockRequestValidator : AbstractValidator<CreateRouteUnlockRequest>
{
    public CreateRouteUnlockRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r) && r.Trim().Length >= 3)
                .WithMessage("Please give a reason (at least 3 characters).")
            .MaximumLength(500).WithMessage("Reason may not exceed 500 characters.");

        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.GpsAccuracyMeters).GreaterThanOrEqualTo(0).When(x => x.GpsAccuracyMeters.HasValue);
    }
}

public class ApproveRouteUnlockRequestValidator : AbstractValidator<ApproveRouteUnlockRequest>
{
    public ApproveRouteUnlockRequestValidator()
    {
        RuleFor(x => x.RowVersion).GreaterThan(0u).WithMessage("RowVersion is required.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("Note may not exceed 500 characters.");
    }
}

public class ReasonedRouteUnlockRequestValidator : AbstractValidator<ReasonedRouteUnlockRequest>
{
    public ReasonedRouteUnlockRequestValidator()
    {
        RuleFor(x => x.RowVersion).GreaterThan(0u).WithMessage("RowVersion is required.");
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r) && r.Trim().Length >= 3)
                .WithMessage("Please give a reason (at least 3 characters).")
            .MaximumLength(500).WithMessage("Reason may not exceed 500 characters.");
    }
}

public class CancelRouteUnlockRequestValidator : AbstractValidator<CancelRouteUnlockRequest>
{
    public CancelRouteUnlockRequestValidator()
    {
        RuleFor(x => x.RowVersion).GreaterThan(0u).WithMessage("RowVersion is required.");
    }
}
