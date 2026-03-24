using FluentAssertions;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.GRNs.Validators;

namespace sfa_api.UnitTests.Features.GRNs.Validators;

public class CreateGrnValidatorTests
{
    private readonly CreateGrnValidator _validator = new();

    [Fact]
    public async Task Validate_SalesInvoiceIdIsZero_Fails()
    {
        var request = new CreateGrnRequest(SalesInvoiceId: 0);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SalesInvoiceId");
    }

    [Fact]
    public async Task Validate_SalesInvoiceIdIsNegative_Fails()
    {
        var request = new CreateGrnRequest(SalesInvoiceId: -1);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SalesInvoiceId");
    }

    [Fact]
    public async Task Validate_SalesInvoiceIdIsPositive_Passes()
    {
        var request = new CreateGrnRequest(SalesInvoiceId: 1);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
