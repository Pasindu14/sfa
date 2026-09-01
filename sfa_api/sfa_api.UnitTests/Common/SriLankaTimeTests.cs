using FluentAssertions;
using sfa_api.Common.Extensions;

namespace sfa_api.UnitTests.Common;

/// <summary>
/// Covers <see cref="SriLankaTime.DayRange"/> — the conversion from a business date to the
/// instant window used to filter absolute-time columns.
///
/// This is where the Rep Route History day-boundary behaviour actually lives. The
/// integration-level equivalent cannot run because the SQLite test provider will not
/// translate ordering comparisons on DateTimeOffset (see LocationPingsApiTests), so these
/// tests carry that weight instead.
/// </summary>
public class SriLankaTimeTests
{
    private static readonly TimeSpan SlOffset = TimeSpan.FromHours(5.5);

    [Fact]
    public void DayRange_StartsAtColomboMidnight_NotUtcMidnight()
    {
        var (from, _) = SriLankaTime.DayRange(new DateOnly(2026, 6, 15));

        // Colombo midnight on the 15th is 18:30 UTC on the 14th. Using
        // date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) instead would start the
        // window 5½ hours late and misfile every pre-dawn record.
        from.UtcDateTime.Should().Be(new DateTime(2026, 6, 14, 18, 30, 0, DateTimeKind.Utc));

        // Expressed in Colombo time it is exactly midnight, as intended.
        from.ToOffset(SlOffset).ToString("yyyy-MM-dd HH:mm").Should().Be("2026-06-15 00:00");
    }

    [Fact]
    public void DayRange_IsReturnedInUtc_SoNpgsqlCanBindIt()
    {
        // Npgsql rejects a DateTimeOffset with a non-zero offset when binding to timestamptz
        // ("only offset 0 (UTC) is supported"), which surfaces as a 500 at query time rather
        // than anything a compiler or the SQLite test provider would catch.
        var (from, to) = SriLankaTime.DayRange(new DateOnly(2026, 6, 15));

        from.Offset.Should().Be(TimeSpan.Zero);
        to.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void DayRange_IsExactlyOneDayLong_AndHalfOpen()
    {
        var (from, to) = SriLankaTime.DayRange(new DateOnly(2026, 6, 15));

        (to - from).Should().Be(TimeSpan.FromDays(1));
        to.ToOffset(SlOffset).ToString("yyyy-MM-dd HH:mm").Should().Be("2026-06-16 00:00");
    }

    [Theory]
    // 00:30 SL is 19:00 UTC the previous day — the case a UTC-midnight window gets wrong.
    [InlineData(0, 30, true)]
    [InlineData(5, 29, true)]
    [InlineData(9, 0, true)]
    [InlineData(23, 59, true)]
    public void DayRange_ContainsEveryColomboWallClockTimeOfThatDay(int hour, int minute, bool expected)
    {
        var date = new DateOnly(2026, 6, 15);
        var (from, to) = SriLankaTime.DayRange(date);

        var ping = new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute)), SlOffset);

        (ping >= from && ping < to).Should().Be(expected);
    }

    [Fact]
    public void DayRange_ExcludesAdjacentDays()
    {
        var date = new DateOnly(2026, 6, 15);
        var (from, to) = SriLankaTime.DayRange(date);

        var lastMomentOfPreviousDay =
            new DateTimeOffset(date.AddDays(-1).ToDateTime(new TimeOnly(23, 59, 59)), SlOffset);
        var firstMomentOfNextDay =
            new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), SlOffset);

        (lastMomentOfPreviousDay >= from).Should().BeFalse();
        (firstMomentOfNextDay < to).Should().BeFalse();
    }

    [Fact]
    public void DayRange_MatchesPingsStoredInUtc()
    {
        // The mobile app uploads RecordedAt as UTC (position.timestamp.toUtc()), so the
        // stored offset is +00:00 while the window is +05:30. DateTimeOffset comparison is
        // offset-aware, so a 08:00 SL working-hours ping stored as 02:30 UTC must still land
        // inside the day.
        var date = new DateOnly(2026, 6, 15);
        var (from, to) = SriLankaTime.DayRange(date);

        var pingAsUtc = new DateTimeOffset(2026, 6, 15, 2, 30, 0, TimeSpan.Zero); // 08:00 SL
        (pingAsUtc >= from && pingAsUtc < to).Should().BeTrue();

        var preDawnAsUtc = new DateTimeOffset(2026, 6, 14, 19, 0, 0, TimeSpan.Zero); // 00:30 SL on the 15th
        (preDawnAsUtc >= from && preDawnAsUtc < to).Should().BeTrue();
    }
}
