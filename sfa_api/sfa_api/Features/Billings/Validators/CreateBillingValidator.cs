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

        // Offline-sync guardrails: mobile may have created the bill up to 7 days ago.
        // Reject future dates (clock skew / rogue client) and anything older than 7 days
        // (likely indicates a corrupted outbox that should be escalated manually).
        RuleFor(x => x.BillingDate!.Value)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("BillingDate cannot be in the future.")
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)))
                .WithMessage("BillingDate cannot be more than 7 days in the past.")
            .When(x => x.BillingDate.HasValue);

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
