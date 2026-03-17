using FluentValidation;
using sfa_api.Features.Distributors.Requests;

namespace sfa_api.Features.Distributors.Validators;

public class UpdateDistributorValidator : AbstractValidator<UpdateDistributorRequest>
{
    public UpdateDistributorValidator()
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
            .GreaterThan(0).WithMessage("Alias must be greater than 0.");

        RuleFor(x => x.TradeDiscount)
            .GreaterThanOrEqualTo(0).WithMessage("Trade discount is required and cannot be negative.")
            .LessThanOrEqualTo(100).WithMessage("Trade discount cannot exceed 100%.");

        RuleFor(x => x.Commission)
            .GreaterThanOrEqualTo(0).WithMessage("Commission is required and cannot be negative.")
            .LessThanOrEqualTo(100).WithMessage("Commission cannot exceed 100%.");

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

        RuleFor(x => x.TerritoryId)
            .GreaterThan(0).WithMessage("TerritoryId must be greater than 0.")
            .When(x => x.TerritoryId.HasValue);
    }
}
