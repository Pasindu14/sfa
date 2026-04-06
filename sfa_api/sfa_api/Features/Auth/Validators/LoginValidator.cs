using FluentValidation;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        // DeviceId is optional for Admin/Manager, required for Sales Reps
        // Cannot validate here since we don't know the user's role yet
        // This is validated in AuthService after fetching the user
    }
}
