using FluentValidation;
using sfa_api.Features.ProductCategoryPricings.Requests;

namespace sfa_api.Features.ProductCategoryPricings.Validators;

public class BulkUpsertPricingValidator : AbstractValidator<BulkUpsertPricingRequest>
{
    private const int MaxItems = 5000;

    public BulkUpsertPricingValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one pricing row is required.")
            .Must(items => items.Count() <= MaxItems)
            .WithMessage($"A bulk upsert may contain at most {MaxItems} pricing rows.");

        RuleForEach(x => x.Items).ChildRules(row =>
        {
            row.RuleFor(r => r.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

            row.RuleFor(r => r.PriceA)
                .GreaterThanOrEqualTo(0).WithMessage("Price A cannot be negative.");

            row.RuleFor(r => r.PriceB)
                .GreaterThanOrEqualTo(0).WithMessage("Price B cannot be negative.");

            row.RuleFor(r => r.PriceC)
                .GreaterThanOrEqualTo(0).WithMessage("Price C cannot be negative.");

            row.RuleFor(r => r.PriceD)
                .GreaterThanOrEqualTo(0).WithMessage("Price D cannot be negative.");
        });
    }
}
