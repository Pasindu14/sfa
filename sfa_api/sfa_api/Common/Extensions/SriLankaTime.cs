namespace sfa_api.Common.Extensions;

/// <summary>
/// Single source of truth for the business clock. The server runs in UTC, but Uswatte's
/// business day is Sri Lanka local time (UTC+5:30). Any logic that asks "what is today /
/// this year" in a business sense MUST go through here — never <c>DateTime.UtcNow</c> directly —
/// otherwise results skew by up to a day during the 00:00–05:30 SL window, where the UTC
/// date still reads as the previous day.
///
/// Audit stamps (<c>CreatedAt</c>/<c>UpdatedAt</c>), token expiry, and cache TTLs should stay
/// on <c>DateTime.UtcNow</c> — those are absolute instants, not business days.
/// </summary>
public static class SriLankaTime
{
    // "Asia/Colombo" (IANA) resolves on Linux/containers natively and on Windows via .NET's
    // ICU support. Fall back to the Windows registry id if ICU is unavailable on the host.
    private static readonly TimeZoneInfo Tz = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); }
    }

    /// <summary>Current wall-clock time in Sri Lanka.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    /// <summary>The current Sri Lanka business day.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>The current Sri Lanka business year — for document-number prefixes.</summary>
    public static int Year => Now.Year;

    /// <summary>
    /// The instant Sri Lankan midnight falls on <paramref name="date"/>, as UTC.
    /// For 2026-06-15 that is 2026-06-14T18:30:00Z.
    ///
    /// This is the single definition of "the start of a business day" — use it whenever a
    /// <c>DateOnly</c> business date has to filter an absolute-instant column
    /// (<c>CreatedAt</c>, <c>TransactedAt</c>, <c>SubmittedAt</c>, …). Build a day range as
    /// <c>[StartOfDayUtc(from), StartOfDayUtc(to.AddDays(1)))</c> — half-open, so the end
    /// day is fully included without a boundary tick belonging to two ranges at once.
    ///
    /// Do NOT use <c>date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)</c>: that treats
    /// the Sri Lankan calendar date as UTC midnight, shifting the window 5½ hours late so
    /// that anything recorded between 00:00 and 05:30 SL is filed under the previous day.
    /// </summary>
    public static DateTime StartOfDayUtc(DateOnly date)
    {
        var startLocal = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(startLocal, Tz.GetUtcOffset(startLocal)).UtcDateTime;
    }

    /// <summary>
    /// The half-open instant range covering one Sri Lanka business day — i.e. the interval
    /// from Colombo midnight on <paramref name="date"/> to Colombo midnight the next day,
    /// returned as UTC instants.
    ///
    /// Use this when filtering an absolute-instant column (<c>DateTimeOffset</c>/<c>DateTime</c>)
    /// by a business date. This is deliberately NOT
    /// <c>date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)</c> — that treats the SL calendar
    /// date as UTC midnight, shifting the window 5½ hours late and filing anything recorded
    /// between 00:00 and 05:30 SL under the previous day.
    ///
    /// The result is normalised to UTC (Offset = 0) rather than left at +05:30 because Npgsql
    /// refuses to bind a <c>DateTimeOffset</c> with a non-zero offset to a <c>timestamptz</c>
    /// parameter ("only offset 0 (UTC) is supported"). Both forms denote the same instant, so
    /// comparisons are unaffected — but only the UTC form can be sent to PostgreSQL.
    /// </summary>
    public static (DateTimeOffset FromInclusive, DateTimeOffset ToExclusive) DayRange(DateOnly date)
    {
        var from = new DateTimeOffset(StartOfDayUtc(date), TimeSpan.Zero);
        return (from, from.AddDays(1));
    }
}
