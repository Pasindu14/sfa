using FluentValidation;
using sfa_api.Features.GRNs.Requests;

namespace sfa_api.Features.GRNs.Validators;

public class CreateGrnValidator : AbstractValidator<CreateGrnRequest>
{
    public CreateGrnValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .GreaterThan(0).WithMessage("SalesInvoiceId must be a positive integer.");
    }
}
