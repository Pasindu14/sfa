namespace sfa_api.Common.Extensions;

/// <summary>
/// The device-reported moment a record was made in the field — distinct from the server's
/// CreatedAt, which is when it arrived. An offline bill made at 10:00 and synced at 14:00 must
/// read 10:00 on the rep's day timeline.
///
/// The phone clock is trusted only when it is plausible: never in the future (beyond small
/// skew) and never more than a few days before receipt. Otherwise it is dropped and readers
/// fall back to CreatedAt, so a wrong phone clock degrades to "server time" instead of
/// inventing a timeline.
/// </summary>
public static class CaptureTime
{
    private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(3);

    public static DateTime? Sanitize(DateTimeOffset? captured, DateTime receivedUtc)
    {
        if (captured is not { } c) return null;
        var utc = c.UtcDateTime;
        if (utc > receivedUtc + MaxFutureSkew) return null;
        if (utc < receivedUtc - MaxAge) return null;
        // Never after receipt — a record can't be made after it arrived.
        return utc > receivedUtc ? receivedUtc : utc;
    }
}
