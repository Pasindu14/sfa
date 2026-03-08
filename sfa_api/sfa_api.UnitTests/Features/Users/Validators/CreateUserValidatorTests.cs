using FluentAssertions;
using FluentValidation.TestHelper;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Validators;

namespace sfa_api.UnitTests.Features.Users.Validators;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    private static CreateUserRequest ValidRequest() => new()
    {
        Name = "John Doe",
        Username = "johndoe",
        Email = "john@example.com",
        Phone = "1234567890",
        Password = "Str0ng@Pass1",
        Role = "Admin"
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

    [Fact]
    public void Name_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Name = new string('A', 100);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ─────────────────────────────────────────────────
    // Username
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Username_Empty_Fails(string? username)
    {
        var req = ValidRequest();
        req.Username = username!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

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
    [InlineData("user-name")]
    public void Username_InvalidCharacters_Fails(string username)
    {
        var req = ValidRequest();
        req.Username = username;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username can only contain letters, numbers, and underscores.");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("user_123")]
    [InlineData("Admin")]
    public void Username_ValidFormats_Pass(string username)
    {
        var req = ValidRequest();
        req.Username = username;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
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
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-email")]
    [InlineData("missing@")]
    [InlineData("@missing.com")]
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Phone_Empty_Fails(string? phone)
    {
        var req = ValidRequest();
        req.Phone = phone!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

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
        result.ShouldHaveValidationErrorFor(x => x.Phone)
              .WithErrorMessage("Phone number can only contain digits, +, -, spaces, and parentheses.");
    }

    [Theory]
    [InlineData("1234567890")]
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
    // Password
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Password_Empty_Fails(string? password)
    {
        var req = ValidRequest();
        req.Password = password!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_TooShort_Fails()
    {
        var req = ValidRequest();
        req.Password = "Ab1@";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Password_NoUppercase_Fails()
    {
        var req = ValidRequest();
        req.Password = "lowercase1@pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void Password_NoLowercase_Fails()
    {
        var req = ValidRequest();
        req.Password = "UPPERCASE1@PASS";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void Password_NoDigit_Fails()
    {
        var req = ValidRequest();
        req.Password = "NoDigit@Pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one digit.");
    }

    [Fact]
    public void Password_NoSpecialChar_Fails()
    {
        var req = ValidRequest();
        req.Password = "NoSpecial1Pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one special character.");
    }

    [Fact]
    public void Password_MeetsAllCriteria_Passes()
    {
        var req = ValidRequest();
        req.Password = "Valid1@Password";
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // ─────────────────────────────────────────────────
    // Role
    // ─────────────────────────────────────────────────

    [Fact]
    public void Role_Empty_Fails()
    {
        var req = ValidRequest();
        req.Role = "";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
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
    [InlineData("Manager")]
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
    public void DeviceId_SalesRepWithEmptyDeviceId_Fails()
    {
        var req = ValidRequest();
        req.Role = "SalesRep";
        req.DeviceId = "";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    [Fact]
    public void DeviceId_SalesRepWithDeviceId_Passes()
    {
        var req = ValidRequest();
        req.Role = "SalesRep";
        req.DeviceId = "device-abc-123";
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
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

    [Fact]
    public void DeviceId_ManagerWithoutDeviceId_Passes()
    {
        var req = ValidRequest();
        req.Role = "Manager";
        req.DeviceId = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
    }
}
