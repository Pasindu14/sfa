using FluentValidation;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        // DeviceId is optional for Admin/Manager, required for Sales Reps
        // Cannot validate here since we don't know the user's role yet
        // This is validated in AuthService after fetching the user
    }
}
