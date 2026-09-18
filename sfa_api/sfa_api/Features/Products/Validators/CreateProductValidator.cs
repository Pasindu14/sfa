using FluentValidation;
using sfa_api.Features.Products.Requests;

namespace sfa_api.Features.Products.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
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
    }
}
