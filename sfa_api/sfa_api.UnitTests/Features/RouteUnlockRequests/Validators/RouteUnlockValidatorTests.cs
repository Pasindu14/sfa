using FluentAssertions;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.RouteUnlockRequests.Validators;

namespace sfa_api.UnitTests.Features.RouteUnlockRequests.Validators;

public class CreateRouteUnlockRequestValidatorTests
{
    private readonly CreateRouteUnlockRequestValidator _v = new();

    private static CreateRouteUnlockRequest Valid() => new() { Reason = "GPS not accurate" };

    [Fact]
    public void Accepts_MinimalRequest() => _v.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Accepts_FullRequestWithLocation()
    {
        var r = Valid();
        r.Latitude = 7.29; r.Longitude = 80.63; r.GpsAccuracyMeters = 0;
        _v.Validate(r).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("  a ")]
    public void Rejects_MissingOrTooShortReason(string reason)
    {
        var r = Valid(); r.Reason = reason;
        _v.Validate(r).Errors.Should().Contain(e => e.PropertyName == nameof(r.Reason));
    }

    [Fact]
    public void Accepts_ReasonAt500_RejectsAt501()
    {
        var r = Valid(); r.Reason = new string('x', 500);
        _v.Validate(r).IsValid.Should().BeTrue();
        r.Reason = new string('x', 501);
        _v.Validate(r).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Rejects_LatitudeOutOfRange(double lat)
    {
        var r = Valid(); r.Latitude = lat;
        _v.Validate(r).Errors.Should().Contain(e => e.PropertyName == nameof(r.Latitude));
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Rejects_LongitudeOutOfRange(double lng)
    {
        var r = Valid(); r.Longitude = lng;
        _v.Validate(r).Errors.Should().Contain(e => e.PropertyName == nameof(r.Longitude));
    }

    [Fact]
    public void Rejects_NegativeGpsAccuracy()
    {
        var r = Valid(); r.GpsAccuracyMeters = -1;
        _v.Validate(r).Errors.Should().Contain(e => e.PropertyName == nameof(r.GpsAccuracyMeters));
    }
}

public class ApproveRouteUnlockRequestValidatorTests
{
    private readonly ApproveRouteUnlockRequestValidator _v = new();

    [Fact]
    public void Accepts_RowVersionWithNoNote()
        => _v.Validate(new ApproveRouteUnlockRequest { RowVersion = 1 }).IsValid.Should().BeTrue();

    [Fact]
    public void Rejects_ZeroRowVersion()
        => _v.Validate(new ApproveRouteUnlockRequest { RowVersion = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void Rejects_NoteOver500()
        => _v.Validate(new ApproveRouteUnlockRequest { RowVersion = 1, Note = new string('n', 501) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void Accepts_NoteAt500()
        => _v.Validate(new ApproveRouteUnlockRequest { RowVersion = 1, Note = new string('n', 500) })
            .IsValid.Should().BeTrue();
}

public class ReasonedRouteUnlockRequestValidatorTests
{
    private readonly ReasonedRouteUnlockRequestValidator _v = new();

    [Fact]
    public void Accepts_ValidReasonAndRowVersion()
        => _v.Validate(new ReasonedRouteUnlockRequest { RowVersion = 2, Reason = "Not needed" }).IsValid.Should().BeTrue();

    [Fact]
    public void Rejects_ZeroRowVersion()
        => _v.Validate(new ReasonedRouteUnlockRequest { RowVersion = 0, Reason = "Not needed" }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("no")]
    public void Rejects_MissingOrShortReason(string reason)
        => _v.Validate(new ReasonedRouteUnlockRequest { RowVersion = 2, Reason = reason }).IsValid.Should().BeFalse();

    [Fact]
    public void Rejects_ReasonOver500()
        => _v.Validate(new ReasonedRouteUnlockRequest { RowVersion = 2, Reason = new string('r', 501) })
            .IsValid.Should().BeFalse();
}

public class CancelRouteUnlockRequestValidatorTests
{
    private readonly CancelRouteUnlockRequestValidator _v = new();

    [Fact]
    public void Accepts_PositiveRowVersion()
        => _v.Validate(new CancelRouteUnlockRequest { RowVersion = 9 }).IsValid.Should().BeTrue();

    [Fact]
    public void Rejects_ZeroRowVersion()
        => _v.Validate(new CancelRouteUnlockRequest { RowVersion = 0 }).IsValid.Should().BeFalse();
}
