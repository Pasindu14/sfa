using FluentValidation;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Validators;

public class StockActivityQueryValidator : AbstractValidator<StockActivityQuery>
{
    // Cap the window so a single request can't page through an unbounded slice of the ledger.
    public const int MaxRangeDays = 93;
    public const int MaxPageSize  = 1000;

    public StockActivityQueryValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("From date is required.");
        RuleFor(x => x.To).NotNull().WithMessage("To date is required.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("To date must be on or after the From date.")
            .When(x => x.From.HasValue && x.To.HasValue);

        RuleFor(x => x)
            .Must(q => q.To!.Value.DayNumber - q.From!.Value.DayNumber <= MaxRangeDays)
            .OverridePropertyName("To")
            .WithMessage($"Date range cannot exceed {MaxRangeDays} days.")
            .When(q => q.From.HasValue && q.To.HasValue && q.To >= q.From);

        RuleFor(x => x.DistributorId).GreaterThan(0).When(x => x.DistributorId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);

        RuleFor(x => x.TransactionType)
            .Must(t => Enum.TryParse<StockTransactionType>(t, ignoreCase: true, out var v) && Enum.IsDefined(v) && !int.TryParse(t, out _))
            .WithMessage($"TransactionType must be one of: {string.Join(", ", Enum.GetNames<StockTransactionType>())}.")
            .When(x => !string.IsNullOrWhiteSpace(x.TransactionType));

        RuleFor(x => x.Direction)
            .Must(d => Enum.TryParse<StockTransactionDirection>(d, ignoreCase: true, out var v) && Enum.IsDefined(v) && !int.TryParse(d, out _))
            .WithMessage($"Direction must be one of: {string.Join(", ", Enum.GetNames<StockTransactionDirection>())}.")
            .When(x => !string.IsNullOrWhiteSpace(x.Direction));

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
