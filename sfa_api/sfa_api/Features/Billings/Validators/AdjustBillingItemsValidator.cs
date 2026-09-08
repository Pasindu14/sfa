using FluentValidation;
using sfa_api.Features.Billings.Requests;

namespace sfa_api.Features.Billings.Validators;

public class AdjustBillingItemsValidator : AbstractValidator<AdjustBillingItemsRequest>
{
    public AdjustBillingItemsValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item adjustment is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.BillingItemId).Distinct().Count() == items.Count)
            .WithMessage("Each billing item may only appear once.")
            .When(x => x.Items is { Count: > 0 });

        // Mirrors the 1000-char cap on BillingAdjustment.Note so an over-long note is a 400,
        // not a column-length failure at write time.
        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note must not exceed 1000 characters.")
            .When(x => x.Note != null);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.BillingItemId)
                .GreaterThan(0).WithMessage("BillingItemId must be a positive integer.");

            // Zero is allowed — it soft-removes the line by returning the whole quantity.
            // The upper bound is the line's current quantity, checked in the service where
            // the persisted bill is available.
            item.RuleFor(i => i.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        });
    }
}
