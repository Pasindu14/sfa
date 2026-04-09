using FluentValidation;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;

namespace sfa_api.Features.Billings.Validators;

public class CreateBillingValidator : AbstractValidator<CreateBillingRequest>
{
    public CreateBillingValidator()
    {
        RuleFor(x => x.OutletId)
            .GreaterThan(0).WithMessage("OutletId must be a positive integer.");

        RuleFor(x => x.BillDiscountRate)
            .InclusiveBetween(0, 100).WithMessage("BillDiscountRate must be between 0 and 100.");

        RuleFor(x => x.ReturnType)
            .NotNull().WithMessage("ReturnType is required when BillingType is Return.")
            .When(x => x.BillingType == BillingType.Return);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one billing item is required.")
            .Must(items => items.Count <= 100).WithMessage("A billing may not have more than 100 items.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be a positive integer.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be zero or greater.");

            item.RuleFor(i => i.DiscountRate)
                .InclusiveBetween(0, 100).WithMessage("DiscountRate must be between 0 and 100.");

            // Free-issue items must have UnitPrice = 0
            item.RuleFor(i => i.UnitPrice)
                .Equal(0).WithMessage("UnitPrice must be 0 for free-issue items.")
                .When(i => i.IsFreeIssue);
        });
    }
}
