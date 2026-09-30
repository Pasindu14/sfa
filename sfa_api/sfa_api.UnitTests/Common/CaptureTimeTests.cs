using FluentAssertions;
using sfa_api.Common.Extensions;

namespace sfa_api.UnitTests.Common;

/// <summary>
/// Covers <see cref="CaptureTime.Sanitize"/> — the plausibility gate on the phone-reported
/// capture time. A wrong phone clock must degrade to "server time" (null), never invent a timeline.
/// </summary>
public class CaptureTimeTests
{
    private static readonly DateTime Received = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
    private static DateTimeOffset At(DateTime utc) => new(utc, TimeSpan.Zero);

    [Fact]
    public void Sanitize_Null_ReturnsNull()
        => CaptureTime.Sanitize(null, Received).Should().BeNull();

    [Fact]
    public void Sanitize_NormalPastTime_ReturnsSameUtcInstant()
    {
        var captured = Received.AddHours(-4);

        CaptureTime.Sanitize(At(captured), Received).Should().Be(captured);
    }

    [Fact]
    public void Sanitize_FutureBeyondSkew_ReturnsNull()
        => CaptureTime.Sanitize(At(Received.AddMinutes(5).AddSeconds(1)), Received).Should().BeNull();

    [Fact]
    public void Sanitize_FutureWithinSkew_IsClampedToReceipt()
        => CaptureTime.Sanitize(At(Received.AddMinutes(3)), Received).Should().Be(Received);

    [Fact]
    public void Sanitize_ExactlyAtSkewLimit_IsClampedNotDropped()
        => CaptureTime.Sanitize(At(Received.AddMinutes(5)), Received).Should().Be(Received);

    [Fact]
    public void Sanitize_OlderThanThreeDays_ReturnsNull()
        => CaptureTime.Sanitize(At(Received.AddDays(-3).AddSeconds(-1)), Received).Should().BeNull();

    [Fact]
    public void Sanitize_ExactlyThreeDaysOld_IsKept()
    {
        var captured = Received.AddDays(-3);

        CaptureTime.Sanitize(At(captured), Received).Should().Be(captured);
    }

    [Fact]
    public void Sanitize_NonUtcOffset_ConvertsToUtc()
    {
        // 14:30 at +05:30 is 09:00 UTC, an hour before receipt.
        var local = new DateTimeOffset(2026, 9, 30, 14, 30, 0, TimeSpan.FromHours(5.5));

        var result = CaptureTime.Sanitize(local, Received);

        result.Should().Be(new DateTime(2026, 9, 30, 9, 0, 0, DateTimeKind.Utc));
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
