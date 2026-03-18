using FluentAssertions;
using FluentValidation.TestHelper;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.PurchaseOrders.Validators;

namespace sfa_api.UnitTests.Features.PurchaseOrders.Validators;

public class UpdatePurchaseOrderValidatorTests
{
    private readonly UpdatePurchaseOrderValidator _validator = new();

    private static UpdateSalesOrderRequest CreateValidRequest() => new()
    {
        Notes = null,
        Items =
        [
            new UpdateSalesOrderItemRequest
            {
                ProductId = 1,
                Quantity = 2,
                UnitPrice = 100m,
                Discount = 0m
            }
        ]
    };

    [Fact]
    public void ValidRequest_ShouldHaveNoValidationErrors()
    {
        var result = _validator.TestValidate(CreateValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRequestWithNotes_ShouldHaveNoValidationErrors()
    {
        var request = CreateValidRequest();
        request.Notes = "Some updated notes";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Notes_ExceedingMaxLength_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Notes = new string('x', 1001);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Notes)
              .WithErrorMessage("Notes must not exceed 1000 characters.");
    }

    [Fact]
    public void Notes_AtMaxLength_ShouldPass()
    {
        var request = CreateValidRequest();
        request.Notes = new string('x', 1000);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Items_Empty_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items = [];

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
              .WithErrorMessage("At least one item is required.");
    }

    [Fact]
    public void Item_ProductIdZero_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].ProductId = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
              .WithErrorMessage("ProductId must be greater than 0.");
    }

    [Fact]
    public void Item_ProductIdNegative_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].ProductId = -1;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    [Fact]
    public void Item_QuantityZero_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].Quantity = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
              .WithErrorMessage("Quantity must be greater than 0.");
    }

    [Fact]
    public void Item_QuantityNegative_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].Quantity = -1;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Item_UnitPriceNegative_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].UnitPrice = -0.01m;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
              .WithErrorMessage("UnitPrice cannot be negative.");
    }

    [Fact]
    public void Item_UnitPriceZero_ShouldPass()
    {
        var request = CreateValidRequest();
        request.Items[0].UnitPrice = 0m;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void Item_DiscountBelowZero_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].Discount = -1m;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Discount")
              .WithErrorMessage("Discount must be between 0 and 100.");
    }

    [Fact]
    public void Item_DiscountAbove100_ShouldFailValidation()
    {
        var request = CreateValidRequest();
        request.Items[0].Discount = 100.01m;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Discount")
              .WithErrorMessage("Discount must be between 0 and 100.");
    }

    [Fact]
    public void Item_DiscountAt100_ShouldPass()
    {
        var request = CreateValidRequest();
        request.Items[0].Discount = 100m;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Items[0].Discount");
    }
}
