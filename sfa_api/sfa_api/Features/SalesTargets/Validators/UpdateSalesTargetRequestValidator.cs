using FluentValidation;
using sfa_api.Features.SalesTargets.Requests;

namespace sfa_api.Features.SalesTargets.Validators;

public class UpdateSalesTargetRequestValidator : AbstractValidator<UpdateSalesTargetRequest>
{
    public UpdateSalesTargetRequestValidator()
    {
        RuleFor(x => x.TargetQuantity).GreaterThanOrEqualTo(0);
    }
}
