using sfa_api.Features.LocationPings.DTOs;

namespace sfa_api.Features.RepTimelines.DTOs;

/// <summary>
/// One rep's business day as a single ordered story: where they went (the embedded GPS
/// <see cref="Route"/>) and what they did along the way (<see cref="Events"/>).
/// </summary>
public record RepDayTimelineDto(
    int RepId,
    string RepName,
    DateOnly Date,
    RepTimelineAssignmentDto? Assignment,
    RepTimelineSummaryDto Summary,
    IReadOnlyList<RepTimelineEventDto> Events,
    RepRouteDto Route);

public record RepTimelineAssignmentDto(int RouteId, string RouteName, int PlannedOutlets);

public record RepTimelineSummaryDto(
    DateTimeOffset? DayStartAt,
    DateTimeOffset? DayEndAt,
    int WorkingMinutes,
    int BillCount,
    decimal BillRevenue,
    int CancelledBillCount,
    int NoSaleCount,
    int OutletsCovered,
    int? PlannedOutlets,
    double? CoveragePercent,
    DateTimeOffset? FirstActivityAt,
    int GpsGapCount,
    int GpsGapMinutes,
    int UnrecordedStopCount,
    int OutOfRangeBillCount,
    int LateSyncCount,
    int LongestIdleMinutes);

/// <summary>
/// One entry on the timeline. A flat shape with nullable fields rather than a type hierarchy,
/// so every client can render it with one switch on <see cref="Kind"/>.
/// </summary>
public record RepTimelineEventDto(
    string Kind,
    DateTimeOffset At,
    DateTimeOffset? EndAt = null,
    int? Minutes = null,
    double? Latitude = null,
    double? Longitude = null,
    int? OutletId = null,
    string? OutletName = null,
    int? BillingId = null,
    string? BillingNumber = null,
    decimal? Amount = null,
    bool? Cancelled = null,
    double? DistanceFromOutletMeters = null,
    bool? OutOfRange = null,
    string? Detail = null,
    bool? SyncedLate = null,
    string? TimeSource = null,
    int? DwellMinutes = null,
    int? SinceLastActivityMinutes = null);

public static class RepTimelineEventKinds
{
    public const string DayStart = "DayStart";
    public const string Bill = "Bill";
    public const string NoSale = "NoSale";
    public const string Stop = "Stop";
    public const string GpsGap = "GpsGap";
    public const string Unlock = "Unlock";
    public const string DayEnd = "DayEnd";
}

// ── Builder inputs (repository projections) ───────────────────────────────────

public record TimelineBill(
    int Id, string BillingNumber, int OutletId, string OutletName,
    DateTime? CapturedAt, DateTime CreatedAt,
    double? Latitude, double? Longitude,
    decimal TotalAmount, bool Cancelled,
    double? DistanceFromOutletMeters, bool ProximityOverridden);

public record TimelineVisit(
    int Id, int OutletId, string OutletName, double OutletLatitude, double OutletLongitude,
    string Reason, DateTime? CapturedAt, DateTime CreatedAt);

public record TimelineUnlockEvent(
    string Action, DateTime PerformedAt, string? PerformedByName, string PerformedByRole,
    string? Note, double? Latitude, double? Longitude);

public record TimelineOutlet(int Id, string Name, double Latitude, double Longitude);

public record TimelineAssignment(int RouteId, string RouteName);
