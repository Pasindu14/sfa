using FluentAssertions;
using FluentValidation.TestHelper;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Validators;

namespace sfa_api.UnitTests.Features.Users.Validators;

public class UpdateUserValidatorTests
{
    private readonly UpdateUserValidator _validator = new();

    private static UpdateUserRequest ValidRequest() => new()
    {
        Name = "Jane Doe",
        Username = "janedoe",
        Email = "jane@example.com",
        Phone = "1234567890",
        Role = "NSM",
        RowVersion = 1
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
        req.Name = new string('A', 101);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    // ─────────────────────────────────────────────────
    // Username
    // ─────────────────────────────────────────────────

    [Fact]
    public void Username_TooShort_Fails()
    {
        var req = ValidRequest();
        req.Username = "ab";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username must be at least 3 characters.");
    }

    [Fact]
    public void Username_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Username = new string('a', 51);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username must not exceed 50 characters.");
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user.name")]
    public void Username_InvalidCharacters_Fails(string username)
    {
        var req = ValidRequest();
        req.Username = username;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username can only contain letters, numbers, and underscores.");
    }

    // ─────────────────────────────────────────────────
    // Email
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("not-email")]
    [InlineData("missing@")]
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
        req.Email = new string('a', 250) + "@b.com";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must not exceed 255 characters.");
    }

    // ─────────────────────────────────────────────────
    // Phone
    // ─────────────────────────────────────────────────

    [Fact]
    public void Phone_TooShort_Fails()
    {
        var req = ValidRequest();
        req.Phone = "123";
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
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    // ─────────────────────────────────────────────────
    // Role
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    public void Role_InvalidValue_Fails(string role)
    {
        var req = ValidRequest();
        req.Role = role;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Role)
              .WithErrorMessage("Invalid role.");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("NSM")]
    [InlineData("SalesRep")]
    public void Role_ValidValues_Pass(string role)
    {
        var req = ValidRequest();
        req.Role = role;
        if (role == "SalesRep") req.DeviceId = "device-1";
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    // ─────────────────────────────────────────────────
    // DeviceId — conditional on SalesRep role
    // ─────────────────────────────────────────────────

    [Fact]
    public void DeviceId_SalesRepWithoutDeviceId_Fails()
    {
        var req = ValidRequest();
        req.Role = "SalesRep";
        req.DeviceId = null;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId)
              .WithErrorMessage("Device ID is required for Sales Reps.");
    }

    [Fact]
    public void DeviceId_AdminWithoutDeviceId_Passes()
    {
        var req = ValidRequest();
        req.Role = "Admin";
        req.DeviceId = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
    }

    // ─────────────────────────────────────────────────
    // RowVersion — optimistic concurrency token
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
    // No Password field in UpdateUserRequest
    // ─────────────────────────────────────────────────

    [Fact]
    public void UpdateRequest_HasNoPasswordValidation()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
