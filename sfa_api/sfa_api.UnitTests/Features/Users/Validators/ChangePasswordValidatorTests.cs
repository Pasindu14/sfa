using FluentAssertions;
using FluentValidation.TestHelper;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Validators;

namespace sfa_api.UnitTests.Features.Users.Validators;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    private static ChangePasswordRequest ValidRequest() => new()
    {
        CurrentPassword = "OldStr0ng@Pass",
        NewPassword = "NewStr0ng@Pass1"
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
    // CurrentPassword
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CurrentPassword_Empty_Fails(string? password)
    {
        var req = ValidRequest();
        req.CurrentPassword = password!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
              .WithErrorMessage("Current password is required.");
    }

    // ─────────────────────────────────────────────────
    // NewPassword — strength rules
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void NewPassword_Empty_Fails(string? password)
    {
        var req = ValidRequest();
        req.NewPassword = password!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_Fails()
    {
        var req = ValidRequest();
        req.NewPassword = "Ab1@";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void NewPassword_NoUppercase_Fails()
    {
        var req = ValidRequest();
        req.NewPassword = "lowercase1@pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void NewPassword_NoLowercase_Fails()
    {
        var req = ValidRequest();
        req.NewPassword = "UPPERCASE1@PASS";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void NewPassword_NoDigit_Fails()
    {
        var req = ValidRequest();
        req.NewPassword = "NoDigit@Pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("Password must contain at least one digit.");
    }

    [Fact]
    public void NewPassword_NoSpecialChar_Fails()
    {
        var req = ValidRequest();
        req.NewPassword = "NoSpecial1Pass";
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("Password must contain at least one special character.");
    }

    // ─────────────────────────────────────────────────
    // NewPassword must differ from CurrentPassword
    // ─────────────────────────────────────────────────

    [Fact]
    public void NewPassword_SameAsCurrent_Fails()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "Str0ng@Pass1",
            NewPassword = "Str0ng@Pass1"
        };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage("New password must be different from current password.");
    }

    [Fact]
    public void NewPassword_DifferentFromCurrent_Passes()
    {
        var req = new ChangePasswordRequest
        {
            CurrentPassword = "Str0ng@Pass1",
            NewPassword = "Differ3nt@Pass"
        };
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
