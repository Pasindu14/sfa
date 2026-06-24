using FluentValidation.TestHelper;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Distributors.Validators;

namespace sfa_api.UnitTests.Features.Distributors.Validators;

public class UpdateDistributorValidatorTests
{
    private readonly UpdateDistributorValidator _validator = new();

    private static UpdateDistributorRequest ValidRequest() => new()
    {
        Name = "Beta Distributors",
        Address = "99 Industrial Avenue, Kandy",
        Phone = "0812345678",
        Email = "beta@distributors.com",
        Alias = 200,
        TradeDiscount = 12.50m,
        Commission = 6.00m,
        Remark = null,
        VatRegNo = null,
        Latitude = null,
        Longitude = null,
        RowVersion = 1
    };

    // ─────────────────────────────────────────────────
    // RowVersion (optimistic concurrency)
    // ─────────────────────────────────────────────────

    [Fact]
    public void RowVersion_Zero_Fails()
    {
        var req = ValidRequest();
        req.RowVersion = 0;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion)
              .WithErrorMessage("RowVersion is required for optimistic concurrency.");
    }

    [Fact]
    public void RowVersion_NonZero_Passes()
    {
        var req = ValidRequest();
        req.RowVersion = 42;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.RowVersion);
    }

    // ─────────────────────────────────────────────────
    // Valid request — baseline
    // ─────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_PassesAllRules()
    {
        var result = _validator.TestValidate(ValidRequest());
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
        req.Phone = "123456789"; // 9 characters
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number must be at least 10 characters.");
    }

    [Theory]
    [InlineData("12345abcde")]
    [InlineData("077-xxx-@#$")]
    public void Phone_InvalidCharacters_Fails(string phone)
    {
        var req = ValidRequest();
        req.Phone = phone;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number can only contain digits, +, -, spaces, and parentheses.");
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
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign")]
    [InlineData("@nodomain")]
    public void Email_InvalidFormat_Fails(string email)
    {
        var req = ValidRequest();
        req.Email = email;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Invalid email format.");
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
        req.Alias = -5;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Alias)
              .WithErrorMessage("Alias must be greater than 0.");
    }

    // ─────────────────────────────────────────────────
    // TradeDiscount
    // ─────────────────────────────────────────────────

    [Fact]
    public void TradeDiscount_Negative_Fails()
    {
        var req = ValidRequest();
        req.TradeDiscount = -1m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TradeDiscount)
              .WithErrorMessage("Trade discount is required and cannot be negative.");
    }

    [Fact]
    public void TradeDiscount_ExceedsOneHundred_Fails()
    {
        var req = ValidRequest();
        req.TradeDiscount = 101m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TradeDiscount)
              .WithErrorMessage("Trade discount cannot exceed 100%.");
    }

    [Fact]
    public void TradeDiscount_Zero_Passes()
    {
        var req = ValidRequest();
        req.TradeDiscount = 0m;
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
        req.Commission = -1m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Commission)
              .WithErrorMessage("Commission is required and cannot be negative.");
    }

    [Fact]
    public void Commission_ExceedsOneHundred_Fails()
    {
        var req = ValidRequest();
        req.Commission = 101m;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Commission)
              .WithErrorMessage("Commission cannot exceed 100%.");
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
