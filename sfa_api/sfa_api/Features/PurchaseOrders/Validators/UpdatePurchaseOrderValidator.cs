using FluentValidation;
using sfa_api.Features.PurchaseOrders.Requests;

namespace sfa_api.Features.PurchaseOrders.Validators;

public class UpdatePurchaseOrderValidator : AbstractValidator<UpdatePurchaseOrderRequest>
{
    public UpdatePurchaseOrderValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes != null);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("UnitPrice cannot be negative.");

            item.RuleFor(i => i.Discount)
                .InclusiveBetween(0, 100).WithMessage("Discount must be between 0 and 100.");
        });
    }
}
