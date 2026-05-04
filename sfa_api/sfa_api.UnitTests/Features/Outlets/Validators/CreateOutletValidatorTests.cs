using FluentAssertions;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Features.Outlets.Validators;

namespace sfa_api.UnitTests.Features.Outlets.Validators;

public class CreateOutletValidatorTests
{
    private readonly CreateOutletValidator _validator = new();

    private static CreateOutletRequest ValidRequest() => new()
    {
        Name = "Sunrise Pharmacy",
        Address = "123 Main Street, Colombo",
        Tel = "0771234567",
        NicNo = "901234567V",
        CreditLimit = 0,
        Latitude = 6.9271,
        Longitude = 79.8612,
        OutletType = "Medium",
        OutletCategory = "Wholesale",
        ProvinceCode = 1,
        DistrictCode = 11,
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

    [Fact]
    public void Validate_NameAtMaxLength_PassesValidation()
    {
        var request = ValidRequest();
        request.Name = new string('N', 200);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ── Address ────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyAddress_FailsWithAddressError(string? address)
    {
        var request = ValidRequest();
        request.Address = address!;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public void Validate_AddressExceedsMaxLength_FailsWithAddressError()
    {
        var request = ValidRequest();
        request.Address = new string('A', 501);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
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
        request.Email = "not-an-email";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ValidEmail_PassesValidation()
    {
        var request = ValidRequest();
        request.Email = "outlet@example.com";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ── CreditLimit ───────────────────────────────────

    [Fact]
    public void Validate_NegativeCreditLimit_FailsWithCreditLimitError()
    {
        var request = ValidRequest();
        request.CreditLimit = -1;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }

    [Fact]
    public void Validate_ZeroCreditLimit_PassesValidation()
    {
        var request = ValidRequest();
        request.CreditLimit = 0;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
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

    [Theory]
    [InlineData(-90.0)]
    [InlineData(90.0)]
    [InlineData(6.9271)]
    public void Validate_LatitudeAtBoundary_PassesValidation(double latitude)
    {
        var request = ValidRequest();
        request.Latitude = latitude;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ── OutletType ────────────────────────────────────

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    public void Validate_ValidOutletType_PassesValidation(string outletType)
    {
        var request = ValidRequest();
        request.OutletType = outletType;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyOutletType_FailsWithOutletTypeError(string? outletType)
    {
        var request = ValidRequest();
        request.OutletType = outletType!;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OutletType");
    }

    [Fact]
    public void Validate_InvalidOutletType_FailsWithOutletTypeError()
    {
        var request = ValidRequest();
        request.OutletType = "Huge";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OutletType");
    }

    // ── OutletCategory ────────────────────────────────

    [Theory]
    [InlineData("Wholesale")]
    [InlineData("SMMT")]
    public void Validate_ValidOutletCategory_PassesValidation(string outletCategory)
    {
        var request = ValidRequest();
        request.OutletCategory = outletCategory;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidOutletCategory_FailsWithOutletCategoryError()
    {
        var request = ValidRequest();
        request.OutletCategory = "Retail";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OutletCategory");
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
    public void Validate_InvalidDistrictCode_FailsWithDistrictCodeError(int districtCode)
    {
        var request = ValidRequest();
        request.DistrictCode = districtCode;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DistrictCode");
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
