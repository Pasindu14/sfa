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
}
