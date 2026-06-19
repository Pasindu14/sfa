using FluentValidation;
using sfa_api.Features.LocationPings.Requests;

namespace sfa_api.Features.LocationPings.Validators;

public class CreateLocationPingsValidator : AbstractValidator<CreateLocationPingsRequest>
{
    public CreateLocationPingsValidator()
    {
        RuleFor(x => x.Pings)
            .NotEmpty().WithMessage("At least one ping is required.")
            .Must(p => p.Count <= 500).WithMessage("Maximum 500 pings per request.");

        RuleForEach(x => x.Pings).ChildRules(ping =>
        {
            ping.RuleFor(p => p.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            ping.RuleFor(p => p.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

            ping.RuleFor(p => p.Accuracy)
                .GreaterThan(0).WithMessage("Accuracy must be positive.");

            ping.RuleFor(p => p.RecordedAt)
                .NotEmpty().WithMessage("RecordedAt is required.");
        });
    }
}
