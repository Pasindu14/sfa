using FluentAssertions;
using sfa_api.Features.UserReportingLines.Requests;
using sfa_api.Features.UserReportingLines.Validators;

namespace sfa_api.UnitTests.Features.UserReportingLines.Validators;

public class UpdateUserReportingLineValidatorTests
{
    private readonly UpdateUserReportingLineValidator _validator = new();

    private static UpdateUserReportingLineRequest ValidRequest() => new()
    {
        ReportsToUserId = 20,
        EffectiveFrom = new DateOnly(2026, 4, 1)
    };

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_InvalidReportsToUserId_FailsWithReportsToUserIdError(int managerId)
    {
        var request = ValidRequest();
        request.ReportsToUserId = managerId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportsToUserId");
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
