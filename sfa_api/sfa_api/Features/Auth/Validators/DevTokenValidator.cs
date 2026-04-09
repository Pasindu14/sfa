using FluentValidation;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Validators;

public class DevTokenValidator : AbstractValidator<DevTokenRequest>
{
    public DevTokenValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be a positive integer.");

        RuleFor(x => x.ExpiryDays)
            .InclusiveBetween(1, 3650).WithMessage("ExpiryDays must be between 1 and 3650.")
            .When(x => x.ExpiryDays.HasValue);
    }
}
