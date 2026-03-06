using FluentValidation;
using sfa_api.Features.Distributors.Requests;

namespace sfa_api.Features.Distributors.Validators;

public class CreateDistributorValidator : AbstractValidator<CreateDistributorRequest>
{
    public CreateDistributorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^[0-9+\-\s()]+$").WithMessage("Phone number can only contain digits, +, -, spaces, and parentheses.")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 characters.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Alias)
            .NotEmpty().WithMessage("Alias is required.")
            .MaximumLength(100).WithMessage("Alias must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Code can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.TradeDiscount)
            .InclusiveBetween(0, 100).WithMessage("Trade discount must be between 0 and 100.");

        RuleFor(x => x.Commission)
            .InclusiveBetween(0, 100).WithMessage("Commission must be between 0 and 100.");

        RuleFor(x => x.Remark)
            .MaximumLength(1000).WithMessage("Remark must not exceed 1000 characters.")
            .When(x => x.Remark != null);

        RuleFor(x => x.VatRegNo)
            .MaximumLength(50).WithMessage("VAT registration number must not exceed 50 characters.")
            .When(x => x.VatRegNo != null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);
    }
}
