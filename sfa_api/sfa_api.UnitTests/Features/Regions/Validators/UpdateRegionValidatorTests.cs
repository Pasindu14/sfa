using FluentValidation.TestHelper;
using sfa_api.Features.Regions.Requests;
using sfa_api.Features.Regions.Validators;

namespace sfa_api.UnitTests.Features.Regions.Validators;

public class UpdateRegionValidatorTests
{
    private readonly UpdateRegionValidator _validator = new();

    private static UpdateRegionRequest ValidRequest() => new()
    {
        Name = "South Region"
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
}
