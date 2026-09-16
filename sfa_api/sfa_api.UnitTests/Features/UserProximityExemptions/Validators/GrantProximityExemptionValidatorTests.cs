using FluentAssertions;
using Microsoft.Extensions.Options;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.UserProximityExemptions.Requests;
using sfa_api.Features.UserProximityExemptions.Validators;

namespace sfa_api.UnitTests.Features.UserProximityExemptions.Validators;

public class GrantProximityExemptionValidatorTests
{
    private static GrantProximityExemptionValidator Build(int maxDays = 30)
        => new(Options.Create(new BillingGeoOptions { MaxExemptionDays = maxDays }));

    private static GrantProximityExemptionRequest Valid() => new()
    {
        ValidUntil = SriLankaTime.Today.AddDays(5),
        Reason = nameof(ProximityExemptionReason.ManagementApproval)
    };

    [Fact]
    public void Accepts_AWellFormedGrant()
    {
        Build().Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_TodayAsTheLastDay()
    {
        // A one-day grant is the common case: the rep is standing at the outlet now.
        var request = Valid();
        request.ValidUntil = SriLankaTime.Today;

        Build().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_APastDate()
    {
        var request = Valid();
        request.ValidUntil = SriLankaTime.Today.AddDays(-1);

        var result = Build().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ValidUntil));
    }

    [Fact]
    public void Rejects_ADurationBeyondTheCap()
    {
        // The cap is what stops an exemption quietly becoming permanent. Without
        // it the obvious admin move is a date years out, and nobody revisits it.
        var request = Valid();
        request.ValidUntil = SriLankaTime.Today.AddDays(31);

        var result = Build(maxDays: 30).Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("30 days"));
    }

    [Fact]
    public void Accepts_ExactlyTheCap()
    {
        var request = Valid();
        request.ValidUntil = SriLankaTime.Today.AddDays(30);

        Build(maxDays: 30).Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_AnEmptyReason()
    {
        var request = Valid();
        request.Reason = "";

        Build().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_AReasonOutsideTheEnum()
    {
        // The service parses this with Enum.Parse, which would throw a raw
        // ArgumentException (a 500) rather than a field error, so the validator
        // is the only thing keeping a typo from looking like a server fault.
        var request = Valid();
        request.Reason = "BecauseISaidSo";

        Build().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_AReasonInTheWrongCase()
    {
        // Parsing is case-sensitive on purpose — accepting 'devicegpsfault' here
        // would let a client depend on coercion the enum does not guarantee.
        var request = Valid();
        request.Reason = "devicegpsfault";

        Build().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_NotesOverFiveHundredCharacters()
    {
        var request = Valid();
        request.Notes = new string('x', 501);

        Build().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_NullNotes()
    {
        var request = Valid();
        request.Notes = null;

        Build().Validate(request).IsValid.Should().BeTrue();
    }
}

public class RevokeProximityExemptionValidatorTests
{
    [Fact]
    public void Rejects_AMissingRowVersion()
    {
        // Revoking without the token would let two admins overwrite each other.
        new RevokeProximityExemptionValidator()
            .Validate(new RevokeProximityExemptionRequest { RowVersion = 0 })
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_APresentRowVersion()
    {
        new RevokeProximityExemptionValidator()
            .Validate(new RevokeProximityExemptionRequest { RowVersion = 12 })
            .IsValid.Should().BeTrue();
    }
}
