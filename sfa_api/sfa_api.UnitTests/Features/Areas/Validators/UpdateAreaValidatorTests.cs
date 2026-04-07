using FluentValidation.TestHelper;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.Areas.Validators;

namespace sfa_api.UnitTests.Features.Areas.Validators;

public class UpdateAreaValidatorTests
{
    private readonly UpdateAreaValidator _validator = new();

    private static UpdateAreaRequest ValidRequest() => new("South Area", 2, 1u);

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
        var req = new UpdateAreaRequest(name!, 2, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Name_ExceedsMaxLength_Fails()
    {
        var req = new UpdateAreaRequest(new string('A', 101), 2, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void Name_ExactlyMaxLength_Passes()
    {
        var req = new UpdateAreaRequest(new string('A', 100), 2, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ─────────────────────────────────────────────────
    // RegionId
    // ─────────────────────────────────────────────────

    [Fact]
    public void RegionId_Zero_Fails()
    {
        var req = new UpdateAreaRequest("South Area", 0, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RegionId)
              .WithErrorMessage("RegionId must be a valid region.");
    }

    [Fact]
    public void RegionId_Negative_Fails()
    {
        var req = new UpdateAreaRequest("South Area", -1, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RegionId)
              .WithErrorMessage("RegionId must be a valid region.");
    }

    [Fact]
    public void RegionId_Positive_Passes()
    {
        var req = new UpdateAreaRequest("South Area", 3, 1u);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.RegionId);
    }

    // ─────────────────────────────────────────────────
    // RowVersion
    // ─────────────────────────────────────────────────

    [Fact]
    public void RowVersion_Zero_Fails()
    {
        var req = new UpdateAreaRequest("South Area", 2, 0u);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion)
              .WithErrorMessage("RowVersion is required for optimistic concurrency.");
    }

    [Fact]
    public void RowVersion_NonZero_Passes()
    {
        var req = new UpdateAreaRequest("South Area", 2, 42u);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.RowVersion);
    }
}
