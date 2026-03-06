using FluentValidation;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Validators;

public class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");

        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device ID is required.");
    }
}
