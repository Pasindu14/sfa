using FluentAssertions;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.GRNs.Validators;

namespace sfa_api.UnitTests.Features.GRNs.Validators;

public class ConfirmGrnValidatorTests
{
    private readonly ConfirmGrnValidator _validator = new();

    // ── ReceivedAt — required ─────────────────────────────────────────────

    [Fact]
    public async Task Validate_ReceivedAtIsDefault_Fails()
    {
        var request = new ConfirmGrnRequest(ReceivedAt: default);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReceivedAt");
    }

    // ── ReceivedAt — must not be in the future (beyond 5 min) ─────────────

    [Fact]
    public async Task Validate_ReceivedAtFarInFuture_Fails()
    {
        var request = new ConfirmGrnRequest(ReceivedAt: DateTime.UtcNow.AddHours(2));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReceivedAt");
    }

    [Fact]
    public async Task Validate_ReceivedAtMoreThan5MinutesInFuture_Fails()
    {
        // 6 minutes ahead — outside the 5-minute grace window
        var request = new ConfirmGrnRequest(ReceivedAt: DateTime.UtcNow.AddMinutes(6));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ── ReceivedAt — valid ────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ReceivedAtNow_Passes()
    {
        var request = new ConfirmGrnRequest(ReceivedAt: DateTime.UtcNow);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ReceivedAtPastDate_Passes()
    {
        var request = new ConfirmGrnRequest(ReceivedAt: DateTime.UtcNow.AddDays(-1));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    // ── Notes — optional, 1000 chars max ─────────────────────────────────

    [Fact]
    public async Task Validate_NotesExceed1000Chars_Fails()
    {
        var request = new ConfirmGrnRequest(
            ReceivedAt: DateTime.UtcNow,
            Notes: new string('x', 1001));

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task Validate_NullNotes_Passes()
    {
        var request = new ConfirmGrnRequest(ReceivedAt: DateTime.UtcNow, Notes: null);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidNotesWithin1000Chars_Passes()
    {
        var request = new ConfirmGrnRequest(
            ReceivedAt: DateTime.UtcNow,
            Notes: "Received in good condition.");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}
