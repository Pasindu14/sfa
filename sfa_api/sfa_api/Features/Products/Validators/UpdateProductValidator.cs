using FluentValidation;
using sfa_api.Features.Products.Requests;

namespace sfa_api.Features.Products.Validators;

public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

        RuleFor(x => x.ItemDescription)
            .NotEmpty().WithMessage("Item description is required.")
            .MaximumLength(255).WithMessage("Item description must not exceed 255 characters.");

        RuleFor(x => x.PrintDescription)
            .MaximumLength(255).WithMessage("Print description must not exceed 255 characters.")
            .When(x => x.PrintDescription != null);

        RuleFor(x => x.PiecesPerPack)
            .GreaterThanOrEqualTo(0).WithMessage("Pieces per pack must be 0 or greater.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.DealerPackPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Dealer pack price must be 0 or greater.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Dealer pack price must not exceed 1,000,000.");

        RuleFor(x => x.DealerCasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Dealer case price must be 0 or greater.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Dealer case price must not exceed 1,000,000.");

        RuleFor(x => x.Mrp)
            .GreaterThanOrEqualTo(0).WithMessage("MRP must be 0 or greater.")
            .LessThanOrEqualTo(1_000_000).WithMessage("MRP must not exceed 1,000,000.");
    }
}
