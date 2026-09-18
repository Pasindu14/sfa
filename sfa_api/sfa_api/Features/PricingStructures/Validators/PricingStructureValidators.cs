using FluentValidation;
using sfa_api.Features.PricingStructures.Requests;

namespace sfa_api.Features.PricingStructures.Validators;

public class CreatePricingStructureValidator : AbstractValidator<CreatePricingStructureRequest>
{
    public CreatePricingStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdatePricingStructureValidator : AbstractValidator<UpdatePricingStructureRequest>
{
    public UpdatePricingStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        RuleFor(x => x.RowVersion)
            .NotEqual(0u).WithMessage("RowVersion is required for optimistic concurrency.");
    }
}

public class DuplicatePricingStructureValidator : AbstractValidator<DuplicatePricingStructureRequest>
{
    public DuplicatePricingStructureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class SetDefaultPricingStructureValidator : AbstractValidator<SetDefaultPricingStructureRequest>
{
    public SetDefaultPricingStructureValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEqual(0u).WithMessage("RowVersion is required for optimistic concurrency.");
    }
}

public class BulkUpsertPricingStructureItemsValidator : AbstractValidator<BulkUpsertPricingStructureItemsRequest>
{
    private const decimal MaxPrice = 1_000_000m;

    public BulkUpsertPricingStructureItemsValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items are required.")
            .Must(i => i is { Count: > 0 and <= 5000 }).WithMessage("Send between 1 and 5000 items.")
            .Must(i => i is null || i.Select(r => r.ProductId).Distinct().Count() == i.Count)
            .WithMessage("Each product may appear only once.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(r => r.ProductId).GreaterThan(0);
            item.RuleFor(r => r.DealerPackPrice)
                .InclusiveBetween(0.01m, MaxPrice).When(r => r.DealerPackPrice.HasValue)
                .WithMessage("Pack price must be between 0.01 and 1,000,000.");
            item.RuleFor(r => r.DealerCasePrice)
                .InclusiveBetween(0.01m, MaxPrice).When(r => r.DealerCasePrice.HasValue)
                .WithMessage("Case price must be between 0.01 and 1,000,000.");
            item.RuleFor(r => r.Mrp)
                .InclusiveBetween(0.01m, MaxPrice).When(r => r.Mrp.HasValue)
                .WithMessage("MRP must be between 0.01 and 1,000,000.");
            // A case price or MRP on an unpriced product is meaningless — the phone can't bill it.
            item.RuleFor(r => r.DealerPackPrice)
                .NotNull().When(r => r.DealerCasePrice.HasValue || r.Mrp.HasValue)
                .WithMessage("Set a pack price before setting a case price or MRP.");
        });
    }
}
