using FluentValidation;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Validators;

public class CreateStockAdjustmentValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public const int MaxLines = 1000;

    public CreateStockAdjustmentValidator()
    {
        RuleFor(x => x.DistributorId)
            .GreaterThan(0).WithMessage("DistributorId must be a positive integer.");

        RuleFor(x => x.Reason).IsInEnum();

        RuleFor(x => x.Notes).MaximumLength(500);

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Notes are required when the reason is Other.")
            .When(x => x.Reason == StockAdjustmentReason.Other);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one line is required.");

        RuleFor(x => x.Lines)
            .Must(l => l.Count <= MaxLines).WithMessage($"An adjustment cannot have more than {MaxLines} lines.")
            .Must(l => l.GroupBy(x => new { x.ProductId, x.StockType }).All(g => g.Count() == 1))
            .WithMessage("Each product and stock type may appear only once.")
            .When(x => x.Lines is not null);

        RuleForEach(x => x.Lines).SetValidator(new CreateStockAdjustmentLineValidator());
    }
}

public class CreateStockAdjustmentLineValidator : AbstractValidator<CreateStockAdjustmentLineRequest>
{
    public CreateStockAdjustmentLineValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StockType).IsInEnum();
        RuleFor(x => x.ExpectedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0);
    }
}
