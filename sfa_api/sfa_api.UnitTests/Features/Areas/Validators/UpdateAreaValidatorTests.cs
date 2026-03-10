using FluentValidation.TestHelper;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.Areas.Validators;

namespace sfa_api.UnitTests.Features.Areas.Validators;

public class UpdateAreaValidatorTests
{
    private readonly UpdateAreaValidator _validator = new();

    private static UpdateAreaRequest ValidRequest() => new()
    {
        Name = "South Area",
        RegionId = 2
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
    // RegionId
    // ─────────────────────────────────────────────────

    [Fact]
    public void RegionId_Zero_Fails()
    {
        var req = ValidRequest();
        req.RegionId = 0;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RegionId)
              .WithErrorMessage("RegionId must be a valid region.");
    }

    [Fact]
    public void RegionId_Negative_Fails()
    {
        var req = ValidRequest();
        req.RegionId = -1;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RegionId)
              .WithErrorMessage("RegionId must be a valid region.");
    }

    [Fact]
    public void RegionId_Positive_Passes()
    {
        var req = ValidRequest();
        req.RegionId = 3;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.RegionId);
    }
}
