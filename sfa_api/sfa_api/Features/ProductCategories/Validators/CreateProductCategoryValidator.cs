using FluentValidation;
using sfa_api.Features.ProductCategories.Requests;

namespace sfa_api.Features.ProductCategories.Validators;

public class CreateProductCategoryValidator : AbstractValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
