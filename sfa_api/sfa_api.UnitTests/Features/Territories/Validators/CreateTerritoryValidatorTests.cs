using FluentValidation.TestHelper;
using sfa_api.Features.Territories.Requests;
using sfa_api.Features.Territories.Validators;

namespace sfa_api.UnitTests.Features.Territories.Validators;

public class CreateTerritoryValidatorTests
{
    private readonly CreateTerritoryValidator _validator = new();

    private static CreateTerritoryRequest ValidRequest() => new()
    {
        Name = "North Territory",
        AreaId = 1
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
        req.Name = new string('T', 101);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void Name_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Name = new string('T', 100);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ─────────────────────────────────────────────────
    // AreaId
    // ─────────────────────────────────────────────────

    [Fact]
    public void AreaId_Zero_Fails()
    {
        var req = ValidRequest();
        req.AreaId = 0;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AreaId)
              .WithErrorMessage("AreaId must be a valid area.");
    }

    [Fact]
    public void AreaId_Negative_Fails()
    {
        var req = ValidRequest();
        req.AreaId = -5;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AreaId)
              .WithErrorMessage("AreaId must be a valid area.");
    }

    [Fact]
    public void AreaId_Positive_Passes()
    {
        var req = ValidRequest();
        req.AreaId = 1;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.AreaId);
    }
}
