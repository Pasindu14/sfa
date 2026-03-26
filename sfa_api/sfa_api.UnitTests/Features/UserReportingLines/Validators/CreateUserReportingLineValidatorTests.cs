using FluentAssertions;
using sfa_api.Features.UserReportingLines.Requests;
using sfa_api.Features.UserReportingLines.Validators;

namespace sfa_api.UnitTests.Features.UserReportingLines.Validators;

public class CreateUserReportingLineValidatorTests
{
    private readonly CreateUserReportingLineValidator _validator = new();

    private static CreateUserReportingLineRequest ValidRequest() => new()
    {
        UserId = 10,
        ReportsToUserId = 20,
        EffectiveFrom = new DateOnly(2026, 3, 26)
    };

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var result = _validator.Validate(ValidRequest());
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
    [InlineData(-5)]
    public void Validate_InvalidReportsToUserId_FailsWithReportsToUserIdError(int managerId)
    {
        var request = ValidRequest();
        request.ReportsToUserId = managerId;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportsToUserId");
    }

    [Fact]
    public void Validate_SelfReport_FailsWithReportsToUserIdError()
    {
        var request = ValidRequest();
        request.UserId = 10;
        request.ReportsToUserId = 10;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportsToUserId"
            && e.ErrorMessage.Contains("cannot report to themselves"));
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
