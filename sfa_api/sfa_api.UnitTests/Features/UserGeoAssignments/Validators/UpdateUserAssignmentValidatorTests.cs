using FluentAssertions;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserGeoAssignments.Validators;

namespace sfa_api.UnitTests.Features.UserGeoAssignments.Validators;

public class UpdateUserAssignmentValidatorTests
{
    private readonly UpdateUserAssignmentValidator _validator = new();

    private static UpdateUserAssignmentRequest ValidRequest() => new()
    {
        ReportsToUserId = 20,
        DivisionId = null,
        EffectiveFrom = new DateOnly(2026, 4, 1)
    };

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidRequestWithDivision_PassesValidation()
    {
        var request = ValidRequest();
        request.DivisionId = 5;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidReportsToUserId_FailsWithReportsToUserIdError(int managerId)
    {
        var request = ValidRequest();
        request.ReportsToUserId = managerId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportsToUserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_InvalidDivisionId_FailsWithDivisionIdError(int divisionId)
    {
        var request = ValidRequest();
        request.DivisionId = divisionId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DivisionId");
    }

    [Fact]
    public void Validate_DefaultEffectiveFrom_FailsWithEffectiveFromError()
    {
        var request = ValidRequest();
        request.EffectiveFrom = default;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EffectiveFrom");
    }
}
