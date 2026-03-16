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
            item.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be greater than or equal to 0.");

            item.RuleFor(x => x.PackPrice)
                .GreaterThanOrEqualTo(0).When(x => x.PackPrice.HasValue)
                .WithMessage("PackPrice must be greater than or equal to 0.");
        });
    }
}
