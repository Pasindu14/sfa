using FluentValidation;
using sfa_api.Features.Reports.Requests;

namespace sfa_api.Features.Reports.Validators;

public class SalesSummaryQueryValidator : AbstractValidator<SalesSummaryQuery>
{
    // Cap the window so one report cannot scan an unbounded slice of BillingItems.
    // Also bounds the per-month target loop to at most 13 queries.
    private const int MaxRangeDays = 366;

    public SalesSummaryQueryValidator()
    {
        RuleFor(x => x.GroupBy)
            .IsInEnum().WithMessage("GroupBy is not a recognised grouping dimension.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("To date must be on or after the From date.");

        RuleFor(x => x)
            .Must(q => q.To.DayNumber - q.From.DayNumber <= MaxRangeDays)
            .WithMessage($"Date range cannot exceed {MaxRangeDays} days.")
            .When(q => q.To >= q.From);

        RuleFor(x => x.RegionId).GreaterThan(0).When(x => x.RegionId.HasValue);
        RuleFor(x => x.AreaId).GreaterThan(0).When(x => x.AreaId.HasValue);
        RuleFor(x => x.TerritoryId).GreaterThan(0).When(x => x.TerritoryId.HasValue);
        RuleFor(x => x.DivisionId).GreaterThan(0).When(x => x.DivisionId.HasValue);
        RuleFor(x => x.RouteId).GreaterThan(0).When(x => x.RouteId.HasValue);
        RuleFor(x => x.DistributorId).GreaterThan(0).When(x => x.DistributorId.HasValue);
        RuleFor(x => x.SalesRepId).GreaterThan(0).When(x => x.SalesRepId.HasValue);
        RuleFor(x => x.SupervisorId).GreaterThan(0).When(x => x.SupervisorId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
    }
}
