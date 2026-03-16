using FluentValidation;
using sfa_api.Features.PricingStructures.Requests;

namespace sfa_api.Features.PricingStructures.Validators;

public class BulkUpdateItemsValidator : AbstractValidator<BulkUpdateItemsRequest>
{
    public BulkUpdateItemsValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Items must not be empty.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.DealerPackPrice)
                .GreaterThanOrEqualTo(0).When(x => x.DealerPackPrice.HasValue)
                .WithMessage("DealerPackPrice must be greater than or equal to 0.");

            item.RuleFor(x => x.DealerCasePrice)
                .GreaterThanOrEqualTo(0).When(x => x.DealerCasePrice.HasValue)
                .WithMessage("DealerCasePrice must be greater than or equal to 0.");

            item.RuleFor(x => x.PromotionalPrice)
                .GreaterThanOrEqualTo(0).When(x => x.PromotionalPrice.HasValue)
                .WithMessage("PromotionalPrice must be greater than or equal to 0.");
        });
    }
}
