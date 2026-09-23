using FluentValidation;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Validators;

public class CreateStockTransferValidator : AbstractValidator<CreateStockTransferRequest>
{
    public const int MaxLines = 1000;

    public CreateStockTransferValidator()
    {
        RuleFor(x => x.SourceDistributorId)
            .GreaterThan(0).WithMessage("SourceDistributorId must be a positive integer.");

        RuleFor(x => x.TargetDistributorId)
            .GreaterThan(0).WithMessage("TargetDistributorId must be a positive integer.")
            .NotEqual(x => x.SourceDistributorId)
            .WithErrorCode("SAME_DISTRIBUTOR")
            .WithMessage("Target distributor must be different from the source distributor.");

        RuleFor(x => x.Notes).MaximumLength(500);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("At least one line is required.");

        RuleFor(x => x.Lines)
            .Must(l => l.Count <= MaxLines).WithMessage($"A transfer cannot have more than {MaxLines} lines.")
            .Must(l => l.GroupBy(x => new { x.ProductId, x.StockType }).All(g => g.Count() == 1))
            .WithMessage("Each product and stock type may appear only once.")
            .When(x => x.Lines is not null);

        RuleForEach(x => x.Lines).SetValidator(new CreateStockTransferLineValidator());
    }
}

public class CreateStockTransferLineValidator : AbstractValidator<CreateStockTransferLineRequest>
{
    public CreateStockTransferLineValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StockType).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
