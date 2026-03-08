using FluentValidation.TestHelper;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Distributors.Validators;

namespace sfa_api.UnitTests.Features.Distributors.Validators;

public class CreateDistributorValidatorTests
{
    private readonly CreateDistributorValidator _validator = new();

    private static CreateDistributorRequest ValidRequest() => new()
    {
        Name = "Alpha Distributors",
        Address = "42 Commerce Street, Colombo 01",
        Phone = "0771234567",
        Email = "alpha@distributors.com",
        Alias = 100,
        TradeDiscount = 10.00m,
        Commission = 5.00m,
        Remark = null,
        VatRegNo = null,
        Latitude = null,
        Longitude = null
    };

    // ─────────────────────────────────────────────────
    // Valid request — baseline
    // ─────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_PassesAllRules()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRequest_WithAllOptionalFields_PassesAllRules()
    {
        var req = ValidRequest();
        req.Remark = "Important distributor";
        req.VatRegNo = "VAT1234567890";
        req.Latitude = 6.9271;
        req.Longitude = 79.8612;

        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─────────────────────────────────────────────────
    // Name
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_Empty_Fails(string? name)
    {
        var req = ValidRequest();
        req.Name = name!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Name_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Name = new string('A', 201);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 200 characters.");
    }

    [Fact]
    public void Name_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Name = new string('A', 200);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ─────────────────────────────────────────────────
    // Address
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Address_Empty_Fails(string? address)
    {
        var req = ValidRequest();
        req.Address = address!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Address_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Address = new string('A', 501);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address must not exceed 500 characters.");
    }

    [Fact]
    public void Address_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Address = new string('A', 500);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    // ─────────────────────────────────────────────────
    // Phone
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Phone_Empty_Fails(string? phone)
    {
        var req = ValidRequest();
        req.Phone = phone!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone is required.");
    }

    [Fact]
    public void Phone_TooShort_Fails()
    {
        var req = ValidRequest();
        req.Phone = "123456789"; // 9 characters, below min of 10
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number must be at least 10 characters.");
    }

    [Fact]
    public void Phone_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Phone = new string('1', 21);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number must not exceed 20 characters.");
    }

    [Theory]
    [InlineData("12345abcde")]
    [InlineData("123-456-@#$")]
    public void Phone_InvalidCharacters_Fails(string phone)
    {
        var req = ValidRequest();
        req.Phone = phone;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number can only contain digits, +, -, spaces, and parentheses.");
    }

    [Theory]
    [InlineData("0771234567")]
    [InlineData("+1 (234) 567-8901")]
    [InlineData("+94-771234567")]
    public void Phone_ValidFormats_Pass(string phone)
    {
        var req = ValidRequest();
        req.Phone = phone;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    // ─────────────────────────────────────────────────
    // Email
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Email_Empty_Fails(string? email)
    {
        var req = ValidRequest();
        req.Email = email!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("not-email")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    public void Email_InvalidFormat_Fails(string email)
    {
        var req = ValidRequest();
        req.Email = email;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Email_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Email = new string('a', 250) + "@b.com"; // over 255
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must not exceed 255 characters.");
    }

    // ─────────────────────────────────────────────────
    // Alias
    // ─────────────────────────────────────────────────

    [Fact]
    public void Alias_Zero_Fails()
    {
        var req = ValidRequest();
        req.Alias = 0;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Alias)
              .WithErrorMessage("Alias must be greater than 0.");
    }

    [Fact]
    public void Alias_Negative_Fails()
    {
        var req = ValidRequest();
        req.Alias = -1;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Alias)
              .WithErrorMessage("Alias must be greater than 0.");
    }

    [Fact]
    public void Alias_PositiveValue_Passes()
    {
        var req = ValidRequest();
        req.Alias = 1;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Alias);
    }

    // ─────────────────────────────────────────────────
    // TradeDiscount
    // ─────────────────────────────────────────────────

    [Fact]
    public void TradeDiscount_Negative_Fails()
    {
        var req = ValidRequest();
        req.TradeDiscount = -0.01m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TradeDiscount)
              .WithErrorMessage("Trade discount is required and cannot be negative.");
    }

    [Fact]
    public void TradeDiscount_ExceedsOneHundred_Fails()
    {
        var req = ValidRequest();
        req.TradeDiscount = 100.01m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TradeDiscount)
              .WithErrorMessage("Trade discount cannot exceed 100%.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void TradeDiscount_BoundaryValues_Pass(decimal discount)
    {
        var req = ValidRequest();
        req.TradeDiscount = discount;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.TradeDiscount);
    }

    // ─────────────────────────────────────────────────
    // Commission
    // ─────────────────────────────────────────────────

    [Fact]
    public void Commission_Negative_Fails()
    {
        var req = ValidRequest();
        req.Commission = -0.01m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Commission)
              .WithErrorMessage("Commission is required and cannot be negative.");
    }

    [Fact]
    public void Commission_ExceedsOneHundred_Fails()
    {
        var req = ValidRequest();
        req.Commission = 100.01m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Commission)
              .WithErrorMessage("Commission cannot exceed 100%.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Commission_BoundaryValues_Pass(decimal commission)
    {
        var req = ValidRequest();
        req.Commission = commission;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Commission);
    }

    // ─────────────────────────────────────────────────
    // Remark (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void Remark_Null_Passes()
    {
        var req = ValidRequest();
        req.Remark = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Remark);
    }

    [Fact]
    public void Remark_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Remark = new string('R', 1001);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Remark)
              .WithErrorMessage("Remark must not exceed 1000 characters.");
    }

    [Fact]
    public void Remark_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Remark = new string('R', 1000);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Remark);
    }

    // ─────────────────────────────────────────────────
    // VatRegNo (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void VatRegNo_Null_Passes()
    {
        var req = ValidRequest();
        req.VatRegNo = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.VatRegNo);
    }

    [Fact]
    public void VatRegNo_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.VatRegNo = new string('V', 51);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.VatRegNo)
              .WithErrorMessage("VAT registration number must not exceed 50 characters.");
    }

    [Fact]
    public void VatRegNo_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.VatRegNo = new string('V', 50);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.VatRegNo);
    }

    // ─────────────────────────────────────────────────
    // Latitude (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void Latitude_Null_Passes()
    {
        var req = ValidRequest();
        req.Latitude = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void Latitude_ValidBoundaryValues_Pass(double latitude)
    {
        var req = ValidRequest();
        req.Latitude = latitude;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    public void Latitude_OutOfRange_Fails(double latitude)
    {
        var req = ValidRequest();
        req.Latitude = latitude;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Latitude)
              .WithErrorMessage("Latitude must be between -90 and 90.");
    }

    // ─────────────────────────────────────────────────
    // Longitude (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void Longitude_Null_Passes()
    {
        var req = ValidRequest();
        req.Longitude = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(180.0)]
    public void Longitude_ValidBoundaryValues_Pass(double longitude)
    {
        var req = ValidRequest();
        req.Longitude = longitude;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    public void Longitude_OutOfRange_Fails(double longitude)
    {
        var req = ValidRequest();
        req.Longitude = longitude;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Longitude)
              .WithErrorMessage("Longitude must be between -180 and 180.");
    }
}
