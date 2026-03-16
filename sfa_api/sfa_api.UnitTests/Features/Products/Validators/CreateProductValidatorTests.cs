using FluentValidation.TestHelper;
using sfa_api.Features.Products.Requests;
using sfa_api.Features.Products.Validators;

namespace sfa_api.UnitTests.Features.Products.Validators;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    private static CreateProductRequest ValidRequest() => new()
    {
        Code = "PROD-001",
        ItemDescription = "Test Product Full Description",
        PrintDescription = null,
        PiecesPerPack = 12,
        ImageUrl = null,
        Remarks = null
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
        req.PrintDescription = "FULL PRINT LABEL";
        req.ImageUrl = "https://example.com/image.png";
        req.Remarks = "Some useful remark";

        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─────────────────────────────────────────────────
    // Code
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Code_Empty_Fails(string? code)
    {
        var req = ValidRequest();
        req.Code = code!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("Code is required.");
    }

    [Fact]
    public void Code_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.Code = new string('A', 51);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("Code must not exceed 50 characters.");
    }

    [Fact]
    public void Code_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.Code = new string('A', 50);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    // ─────────────────────────────────────────────────
    // ItemDescription
    // ─────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ItemDescription_Empty_Fails(string? desc)
    {
        var req = ValidRequest();
        req.ItemDescription = desc!;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.ItemDescription)
              .WithErrorMessage("Item description is required.");
    }

    [Fact]
    public void ItemDescription_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.ItemDescription = new string('A', 256);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.ItemDescription)
              .WithErrorMessage("Item description must not exceed 255 characters.");
    }

    [Fact]
    public void ItemDescription_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.ItemDescription = new string('A', 255);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.ItemDescription);
    }

    // ─────────────────────────────────────────────────
    // PrintDescription (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void PrintDescription_Null_Passes()
    {
        var req = ValidRequest();
        req.PrintDescription = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.PrintDescription);
    }

    [Fact]
    public void PrintDescription_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.PrintDescription = new string('A', 256);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.PrintDescription)
              .WithErrorMessage("Print description must not exceed 255 characters.");
    }

    [Fact]
    public void PrintDescription_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.PrintDescription = new string('A', 255);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.PrintDescription);
    }

    // ─────────────────────────────────────────────────
    // PiecesPerPack
    // ─────────────────────────────────────────────────

    [Fact]
    public void PiecesPerPack_Zero_Passes()
    {
        var req = ValidRequest();
        req.PiecesPerPack = 0;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.PiecesPerPack);
    }

    [Fact]
    public void PiecesPerPack_Negative_Fails()
    {
        var req = ValidRequest();
        req.PiecesPerPack = -1;
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.PiecesPerPack)
              .WithErrorMessage("Pieces per pack must be 0 or greater.");
    }

    [Fact]
    public void PiecesPerPack_PositiveValue_Passes()
    {
        var req = ValidRequest();
        req.PiecesPerPack = 100;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.PiecesPerPack);
    }

    // ─────────────────────────────────────────────────
    // ImageUrl (optional)
    // ─────────────────────────────────────────────────

    [Fact]
    public void ImageUrl_Null_Passes()
    {
        var req = ValidRequest();
        req.ImageUrl = null;
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void ImageUrl_ExceedsMaxLength_Fails()
    {
        var req = ValidRequest();
        req.ImageUrl = new string('a', 501);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
              .WithErrorMessage("Image URL must not exceed 500 characters.");
    }

    [Fact]
    public void ImageUrl_ExactlyMaxLength_Passes()
    {
        var req = ValidRequest();
        req.ImageUrl = new string('a', 500);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }
}
