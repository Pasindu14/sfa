using FluentValidation;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Requests;

namespace sfa_api.Features.Users.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^[0-9+\-\s()]+$").WithMessage("Phone number can only contain digits, +, -, spaces, and parentheses.")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 characters.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => Enum.TryParse<UserRole>(role, out _)).WithMessage("Invalid role.");

        RuleFor(x => x.DeviceId)
            .NotEmpty().When(x => x.Role.Equals("SalesRep", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Device ID is required for Sales Reps.");
    }
}
