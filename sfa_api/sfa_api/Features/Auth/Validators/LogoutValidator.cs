using FluentValidation;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Validators;

public class LogoutValidator : AbstractValidator<LogoutRequest>
{
    public LogoutValidator()
    {
        // RefreshToken is intentionally optional — an empty value is a graceful no-op logout.
        // We only enforce a max length to prevent oversized payloads.
        RuleFor(x => x.RefreshToken)
            .MaximumLength(500).WithMessage("Refresh token is invalid.")
            .When(x => !string.IsNullOrEmpty(x.RefreshToken));
    }
}
