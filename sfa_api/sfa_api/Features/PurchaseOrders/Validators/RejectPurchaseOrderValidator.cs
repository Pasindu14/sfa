using FluentValidation;
using sfa_api.Features.PurchaseOrders.Requests;

namespace sfa_api.Features.PurchaseOrders.Validators;

public class RejectPurchaseOrderValidator : AbstractValidator<RejectPurchaseOrderRequest>
{
    public RejectPurchaseOrderValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
