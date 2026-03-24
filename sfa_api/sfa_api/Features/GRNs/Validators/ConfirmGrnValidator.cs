using FluentValidation;
using sfa_api.Features.GRNs.Requests;

namespace sfa_api.Features.GRNs.Validators;

public class ConfirmGrnValidator : AbstractValidator<ConfirmGrnRequest>
{
    public ConfirmGrnValidator()
    {
        RuleFor(x => x.ReceivedAt)
            .NotEmpty().WithMessage("ReceivedAt is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("ReceivedAt cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes is not null);
    }
}
