using FluentValidation.TestHelper;
using sfa_api.Features.Divisions.Requests;
using sfa_api.Features.Divisions.Validators;

namespace sfa_api.UnitTests.Features.Divisions.Validators;

public class UpdateDivisionValidatorTests
{
    private readonly UpdateDivisionValidator _validator = new();

    private static UpdateDivisionRequest ValidRequest() => new()
    {
        Name = "Updated Division",
        TerritoryId = 1
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
        req.Name = new string('D', 101);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void Name_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Name = new string('D', 100);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ─────────────────────────────────────────────────
    // TerritoryId
    // ─────────────────────────────────────────────────

    [Fact]
    public void TerritoryId_Zero_Fails()
    {
        var req = ValidRequest();
        req.TerritoryId = 0;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TerritoryId)
              .WithErrorMessage("TerritoryId must be a valid territory.");
    }

    [Fact]
    public void TerritoryId_Negative_Fails()
    {
        var req = ValidRequest();
        req.TerritoryId = -5;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TerritoryId)
              .WithErrorMessage("TerritoryId must be a valid territory.");
    }

    [Fact]
    public void TerritoryId_Positive_Passes()
    {
        var req = ValidRequest();
        req.TerritoryId = 1;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.TerritoryId);
    }
}
