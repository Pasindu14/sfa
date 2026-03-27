using FluentAssertions;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserGeoAssignments.Validators;

namespace sfa_api.UnitTests.Features.UserGeoAssignments.Validators;

public class CreateUserAssignmentValidatorTests
{
    private readonly CreateUserAssignmentValidator _validator = new();

    private static CreateUserAssignmentRequest ValidRequest() => new()
    {
        UserId = 10,
        DivisionId = null,
        EffectiveFrom = new DateOnly(2026, 3, 26)
    };

    [Fact]
    public void Validate_ValidRequestNoDivision_PassesValidation()
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
    public void Validate_InvalidUserId_FailsWithUserIdError(int userId)
    {
        var request = ValidRequest();
        request.UserId = userId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_InvalidDivisionId_FailsWithDivisionIdError(int divisionId)
    {
        var request = ValidRequest();
        request.DivisionId = divisionId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DivisionId");
    }

    [Fact]
    public void Validate_NullDivisionId_PassesValidation()
    {
        var request = ValidRequest();
        request.DivisionId = null;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
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
