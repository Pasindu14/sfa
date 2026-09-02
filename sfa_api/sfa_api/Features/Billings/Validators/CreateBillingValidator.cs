using FluentValidation;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;

namespace sfa_api.Features.Billings.Validators;

public class CreateBillingValidator : AbstractValidator<CreateBillingRequest>
{
    public CreateBillingValidator()
    {
        RuleFor(x => x.OutletId)
            .GreaterThan(0).WithMessage("OutletId must be a positive integer.");

        RuleFor(x => x.BillDiscountRate)
            .InclusiveBetween(0, 100).WithMessage("BillDiscountRate must be between 0 and 100.");

        // Rep remarks captured when the bill is closed. Mirrors the 1000-char
        // cap configured on Billing.Notes so an over-long note fails as a 400
        // rather than blowing up against the column limit.
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes != null);

        RuleFor(x => x.BillingDate!.Value)
            .LessThanOrEqualTo(_ => SriLankaTime.Today)
                .WithMessage("BillingDate cannot be in the future.")
            .GreaterThanOrEqualTo(_ => SriLankaTime.Today.AddDays(-7))
                .WithMessage("BillingDate cannot be more than 7 days in the past.")
            .When(x => x.BillingDate.HasValue);

        // Rep coordinates are mandatory. Without them the proximity gate in
        // BillingService silently does nothing, so an omitted coordinate is an
        // opt-out of the geofence rather than a missing nicety.
        RuleFor(x => x.Latitude)
            .NotNull().WithMessage("Latitude is required — location must be enabled to bill.");

        RuleFor(x => x.Longitude)
            .NotNull().WithMessage("Longitude is required — location must be enabled to bill.");

        RuleFor(x => x.Latitude)
            .Must(v => v is >= -90 and <= 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .Must(v => v is >= -180 and <= 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        // (0,0) is GeoMath's "no coordinate stored" sentinel. Accepting it as a
        // rep position would disable the proximity check for that bill, which is
        // exactly the bypass this rule exists to close.
        RuleFor(x => x.Latitude)
            .Must((req, _) => !(req.Latitude == 0 && req.Longitude == 0))
                .WithMessage("A valid device location is required — (0, 0) is not an acceptable position.")
            .When(x => x.Latitude.HasValue && x.Longitude.HasValue);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one billing item is required.")
            .Must(items => items.Count <= 100).WithMessage("A billing may not have more than 100 items.")
            .Must(items => items.Any(i => i.BillingItemType == BillingItemType.Sale))
                .WithMessage("A bill must contain at least one Sale item.")
            .Must(items =>
            {
                var saleTotal = items
                    .Where(i => i.BillingItemType == BillingItemType.Sale)
                    .Sum(i => i.Quantity * i.UnitPrice * (1 - i.DiscountRate / 100m));
                var returnTotal = items
                    .Where(i => i.BillingItemType == BillingItemType.Return
                             && i.ReturnType == ReturnType.MarketResell)
                    .Sum(i => i.Quantity * i.UnitPrice);
                return returnTotal <= saleTotal;
            })
            .WithMessage("Total market resell return value cannot exceed total sale value.")
            .When(x => x.Items.Any(i => i.BillingItemType == BillingItemType.Return
                                     && i.ReturnType == ReturnType.MarketResell));

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be a positive integer.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Quantity must not exceed 1,000,000.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be zero or greater.")
                .LessThanOrEqualTo(1_000_000).WithMessage("UnitPrice must not exceed 1,000,000.");

            item.RuleFor(i => i.DiscountRate)
                .InclusiveBetween(0, 100).WithMessage("DiscountRate must be between 0 and 100.");

            // Free-issue items must carry the real selling price (it's stored for FOC valuation)
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("UnitPrice must be greater than zero for free-issue items.")
                .When(i => i.BillingItemType == BillingItemType.FreeIssue);

            // Free-issue items cannot also carry a discount — they're already free
            item.RuleFor(i => i.DiscountRate)
                .Equal(0).WithMessage("DiscountRate must be 0 for free-issue items.")
                .When(i => i.BillingItemType == BillingItemType.FreeIssue);

            // Return items must specify a return reason
            item.RuleFor(i => i.ReturnType)
                .NotNull().WithMessage("ReturnType is required for return items.")
                .When(i => i.BillingItemType == BillingItemType.Return);

            // Non-return items must not have a return reason
            item.RuleFor(i => i.ReturnType)
                .Null().WithMessage("ReturnType must be null for non-return items.")
                .When(i => i.BillingItemType != BillingItemType.Return);

            // Expire items must include an expire date (must not be in the future — it's already expired)
            item.RuleFor(i => i.ExpireDate)
                .NotNull().WithMessage("ExpireDate is required when ReturnType is Expire.")
                .LessThanOrEqualTo(_ => SriLankaTime.Today)
                    .WithMessage("ExpireDate must not be in the future.")
                .When(i => i.ReturnType == ReturnType.Expire);

            // Non-expire items must not carry an expire date
            item.RuleFor(i => i.ExpireDate)
                .Null().WithMessage("ExpireDate must be null when ReturnType is not Expire.")
                .When(i => i.ReturnType != ReturnType.Expire);

            // Free-issue items must specify a funding source (Company = FOC stock pool, Distributor = Normal stock pool)
            item.RuleFor(i => i.FreeIssueSource)
                .NotNull().WithMessage("FreeIssueSource is required for free-issue items.")
                .IsInEnum().WithMessage("FreeIssueSource must be Company or Distributor.")
                .When(i => i.BillingItemType == BillingItemType.FreeIssue);

            // Non-free-issue items must not carry a funding source
            item.RuleFor(i => i.FreeIssueSource)
                .Null().WithMessage("FreeIssueSource must be null for non-free-issue items.")
                .When(i => i.BillingItemType != BillingItemType.FreeIssue);
        });
    }
}
