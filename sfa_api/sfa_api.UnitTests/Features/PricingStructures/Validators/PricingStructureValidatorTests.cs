using FluentAssertions;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Validators;

namespace sfa_api.UnitTests.Features.PricingStructures.Validators;

public class PricingStructureValidatorTests
{
    private readonly BulkUpsertPricingStructureItemsValidator _items = new();

    [Fact]
    public void Items_CasePriceWithoutPackPrice_IsInvalid()
    {
        var result = _items.Validate(new BulkUpsertPricingStructureItemsRequest(
            [new PricingStructureItemUpsert(1, null, 500m, null)]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("pack price"));
    }

    [Fact]
    public void Items_AllNull_ClearsAndIsValid()
        => _items.Validate(new BulkUpsertPricingStructureItemsRequest(
            [new PricingStructureItemUpsert(1, null, null, null)])).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000.01)]
    public void Items_PackPriceOutOfRange_IsInvalid(decimal price)
        => _items.Validate(new BulkUpsertPricingStructureItemsRequest(
            [new PricingStructureItemUpsert(1, price, null, null)])).IsValid.Should().BeFalse();

    [Fact]
    public void Items_DuplicateProduct_IsInvalid()
        => _items.Validate(new BulkUpsertPricingStructureItemsRequest(
            [new PricingStructureItemUpsert(1, 1m, null, null), new PricingStructureItemUpsert(1, 2m, null, null)]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Items_Empty_IsInvalid()
        => _items.Validate(new BulkUpsertPricingStructureItemsRequest([])).IsValid.Should().BeFalse();

    [Fact]
    public void Create_NameRequired()
        => new CreatePricingStructureValidator().Validate(new CreatePricingStructureRequest("", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SetDefault_RowVersionRequired()
        => new SetDefaultPricingStructureValidator().Validate(new SetDefaultPricingStructureRequest(0))
            .IsValid.Should().BeFalse();
}
