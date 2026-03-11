using FluentAssertions;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Features.Outlets.Validators;

namespace sfa_api.UnitTests.Features.Outlets.Validators;

public class UpdateOutletValidatorTests
{
    private readonly UpdateOutletValidator _validator = new();

    private static UpdateOutletRequest ValidRequest() => new()
    {
        Name = "Updated Pharmacy",
        Address = "456 Updated Street, Colombo",
        Tel = "0779876543",
        NicNo = "851234567V",
        CreditLimit = 500,
        Latitude = 7.0,
        Longitude = 80.0,
        OutletType = "Large",
        OutletCategory = "SMMT",
        ProvinceCode = 2,
        DistrictCode = 22,
        RouteId = 1
    };

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_PassesValidation()
    {
        var request = ValidRequest();
        request.Email = null;
        request.ContactPerson = null;
        request.VatNo = null;
        request.OwnerDOB = null;
        request.Remarks = null;
        request.Image = null;
        request.BillingPriceType = null;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ── Name ───────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyName_FailsWithNameError(string? name)
    {
        var request = ValidRequest();
        request.Name = name!;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_FailsWithNameError()
    {
        var request = ValidRequest();
        request.Name = new string('N', 201);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Tel ────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyTel_FailsWithTelError(string? tel)
    {
        var request = ValidRequest();
        request.Tel = tel!;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tel");
    }

    // ── NicNo ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyNicNo_FailsWithNicNoError(string? nicNo)
    {
        var request = ValidRequest();
        request.NicNo = nicNo!;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NicNo");
    }

    // ── Email ─────────────────────────────────────────

    [Fact]
    public void Validate_InvalidEmail_FailsWithEmailError()
    {
        var request = ValidRequest();
        request.Email = "bad-email-format";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // ── CreditLimit ───────────────────────────────────

    [Fact]
    public void Validate_NegativeCreditLimit_FailsWithCreditLimitError()
    {
        var request = ValidRequest();
        request.CreditLimit = -100;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }

    // ── Latitude / Longitude ──────────────────────────

    [Theory]
    [InlineData(-91.0)]
    [InlineData(91.0)]
    public void Validate_LatitudeOutOfRange_FailsWithLatitudeError(double latitude)
    {
        var request = ValidRequest();
        request.Latitude = latitude;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Latitude");
    }

    [Theory]
    [InlineData(-181.0)]
    [InlineData(181.0)]
    public void Validate_LongitudeOutOfRange_FailsWithLongitudeError(double longitude)
    {
        var request = ValidRequest();
        request.Longitude = longitude;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Longitude");
    }

    // ── OutletType ────────────────────────────────────

    [Fact]
    public void Validate_InvalidOutletType_FailsWithOutletTypeError()
    {
        var request = ValidRequest();
        request.OutletType = "Tiny";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OutletType");
    }

    // ── OutletCategory ────────────────────────────────

    [Fact]
    public void Validate_InvalidOutletCategory_FailsWithOutletCategoryError()
    {
        var request = ValidRequest();
        request.OutletCategory = "Supermarket";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OutletCategory");
    }

    // ── BillingPriceType ──────────────────────────────

    [Fact]
    public void Validate_InvalidBillingPriceType_FailsWithBillingPriceTypeError()
    {
        var request = ValidRequest();
        request.BillingPriceType = "FlatRate";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BillingPriceType");
    }

    // ── ProvinceCode / DistrictCode / RouteId ─────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidProvinceCode_FailsWithProvinceCodeError(int provinceCode)
    {
        var request = ValidRequest();
        request.ProvinceCode = provinceCode;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProvinceCode");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidRouteId_FailsWithRouteIdError(int routeId)
    {
        var request = ValidRequest();
        request.RouteId = routeId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RouteId");
    }
}
