using FluentValidation;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Outlets.Requests;

namespace sfa_api.Features.Outlets.Validators;

public class UpdateOutletValidator : AbstractValidator<UpdateOutletRequest>
{
    public UpdateOutletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Tel)
            .NotEmpty().WithMessage("Tel is required.")
            .MaximumLength(20).WithMessage("Tel must not exceed 20 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            .When(x => x.Email != null);

        RuleFor(x => x.ContactPerson)
            .MaximumLength(200).WithMessage("ContactPerson must not exceed 200 characters.")
            .When(x => x.ContactPerson != null);

        RuleFor(x => x.NicNo)
            .NotEmpty().WithMessage("NicNo is required.")
            .MaximumLength(20).WithMessage("NicNo must not exceed 20 characters.");

        RuleFor(x => x.VatNo)
            .MaximumLength(50).WithMessage("VatNo must not exceed 50 characters.")
            .When(x => x.VatNo != null);

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("CreditLimit must be zero or greater.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Remarks)
            .MaximumLength(1000).WithMessage("Remarks must not exceed 1000 characters.")
            .When(x => x.Remarks != null);

        RuleFor(x => x.OutletType)
            .NotEmpty().WithMessage("OutletType is required.")
            .Must(v => Enum.TryParse<OutletType>(v, out _))
            .WithMessage("Invalid OutletType. Valid values: Small, Medium, Large.");

        RuleFor(x => x.OutletCategory)
            .NotEmpty().WithMessage("OutletCategory is required.")
            .Must(v => Enum.TryParse<OutletCategory>(v, out _))
            .WithMessage("Invalid OutletCategory. Valid values: Wholesale, SMMT.");

        RuleFor(x => x.ProvinceCode)
            .GreaterThan(0).WithMessage("ProvinceCode must be greater than 0.")
            .When(x => x.ProvinceCode.HasValue);

        RuleFor(x => x.DistrictCode)
            .GreaterThan(0).WithMessage("DistrictCode must be greater than 0.")
            .When(x => x.DistrictCode.HasValue);

        RuleFor(x => x.RouteId)
            .GreaterThan(0).WithMessage("RouteId must be a valid route.");

        RuleFor(x => x.RowVersion)
            .NotEqual(0u).WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
