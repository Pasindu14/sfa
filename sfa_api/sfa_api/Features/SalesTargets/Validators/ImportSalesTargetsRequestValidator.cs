using FluentValidation;
using sfa_api.Features.SalesTargets.Requests;

namespace sfa_api.Features.SalesTargets.Validators;

public class ImportSalesTargetsRequestValidator : AbstractValidator<ImportSalesTargetsRequest>
{
    public ImportSalesTargetsRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Year).InclusiveBetween(2020, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Rows).NotEmpty().WithMessage("At least one row is required.");
        RuleForEach(x => x.Rows).SetValidator(new TargetRowRequestValidator());
    }
}

public class TargetRowRequestValidator : AbstractValidator<TargetRowRequest>
{
    public TargetRowRequestValidator()
    {
        RuleFor(x => x.RepsCode).GreaterThan(0);
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TargetQty).GreaterThanOrEqualTo(0);
    }
}
