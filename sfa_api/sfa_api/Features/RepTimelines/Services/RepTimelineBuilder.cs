using sfa_api.Common.Geo;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.RepTimelines.DTOs;
using K = sfa_api.Features.RepTimelines.DTOs.RepTimelineEventKinds;

namespace sfa_api.Features.RepTimelines.Services;

/// <summary>
/// Turns one rep-day of raw facts (GPS pings, bills, no-sale visits, unlock events) into an
/// ordered timeline plus day KPIs. Pure — no I/O — so every rule is unit-testable.
/// </summary>
public static class RepTimelineBuilder
{
    /// A stop is the phone staying within this radius of where it arrived...
    public const double StopRadiusMeters = 120;
    /// ...for at least this long.
    public static readonly TimeSpan MinStop = TimeSpan.FromMinutes(10);
    /// A bill/visit this close in time to a stop explains it.
    public static readonly TimeSpan StopMatchSlack = TimeSpan.FromMinutes(10);
    /// Nearest route outlet named on an unexplained stop, if within this distance.
    public const double NearestOutletMeters = 150;
    /// Receipt this long after capture counts as a late (offline) sync.
    public static readonly TimeSpan LateSync = TimeSpan.FromMinutes(15);

    private record Activity(RepTimelineEventDto Event, DateTimeOffset At, int? OutletId, bool Cancelled);

    public static (RepTimelineSummaryDto Summary, IReadOnlyList<RepTimelineEventDto> Events) Build(
        IReadOnlyList<RepRoutePointDto> points,
        int gapThresholdMinutes,
        IReadOnlyList<TimelineBill> bills,
        IReadOnlyList<TimelineVisit> visits,
        IReadOnlyList<TimelineUnlockEvent> unlocks,
        IReadOnlyList<TimelineOutlet> routeOutlets,
        TimelineAssignment? assignment)
    {
        var pings = points.OrderBy(p => p.RecordedAt).ToList();
        var gap = TimeSpan.FromMinutes(gapThresholdMinutes);

        // ── Activities ──────────────────────────────────────────────────────
        var activities = new List<Activity>();
        foreach (var b in bills)
        {
            var (at, source, late) = TimeOf(b.CapturedAt, b.CreatedAt);
            activities.Add(new Activity(new RepTimelineEventDto(
                Kind: K.Bill, At: at,
                Latitude: b.Latitude, Longitude: b.Longitude,
                OutletId: b.OutletId, OutletName: b.OutletName,
                BillingId: b.Id, BillingNumber: b.BillingNumber,
                Amount: b.TotalAmount, Cancelled: b.Cancelled,
                DistanceFromOutletMeters: b.DistanceFromOutletMeters,
                OutOfRange: b.ProximityOverridden,
                SyncedLate: late, TimeSource: source), at, b.OutletId, b.Cancelled));
        }
        foreach (var v in visits)
        {
            var (at, source, late) = TimeOf(v.CapturedAt, v.CreatedAt);
            var hasPos = !(v.OutletLatitude == 0 && v.OutletLongitude == 0);
            activities.Add(new Activity(new RepTimelineEventDto(
                Kind: K.NoSale, At: at,
                Latitude: hasPos ? v.OutletLatitude : null,
                Longitude: hasPos ? v.OutletLongitude : null,
                OutletId: v.OutletId, OutletName: v.OutletName,
                Detail: v.Reason,
                SyncedLate: late, TimeSource: source), at, v.OutletId, false));
        }
        activities.Sort((a, b) => a.At.CompareTo(b.At));

        var events = new List<RepTimelineEventDto>();

        // ── GPS gaps ────────────────────────────────────────────────────────
        var gapCount = 0;
        var gapMinutes = 0;
        for (var i = 1; i < pings.Count; i++)
        {
            var span = pings[i].RecordedAt - pings[i - 1].RecordedAt;
            if (span <= gap) continue;
            gapCount++;
            gapMinutes += Minutes(span);
            events.Add(new RepTimelineEventDto(
                Kind: K.GpsGap, At: pings[i - 1].RecordedAt, EndAt: pings[i].RecordedAt,
                Minutes: Minutes(span),
                Latitude: pings[i - 1].Latitude, Longitude: pings[i - 1].Longitude));
        }

        // ── Stops: explain them with activities, or report them ─────────────
        var dwell = new Dictionary<Activity, int>();
        var unrecordedStops = 0;
        foreach (var stop in DetectStops(pings, gap))
        {
            var matched = activities
                .Where(a => a.At >= stop.Start - StopMatchSlack && a.At <= stop.End + StopMatchSlack)
                .ToList();
            var stayed = Minutes(stop.End - stop.Start);

            if (matched.Count > 0)
            {
                foreach (var a in matched)
                    dwell[a] = Math.Max(dwell.GetValueOrDefault(a), stayed);
                continue;
            }

            unrecordedStops++;
            var nearest = routeOutlets
                .Where(o => !(o.Latitude == 0 && o.Longitude == 0))
                .Select(o => (o, d: GeoMath.HaversineMeters(stop.Lat, stop.Lng, o.Latitude, o.Longitude)))
                .Where(x => x.d <= NearestOutletMeters)
                .OrderBy(x => x.d)
                .FirstOrDefault();

            events.Add(new RepTimelineEventDto(
                Kind: K.Stop, At: stop.Start, EndAt: stop.End, Minutes: stayed,
                Latitude: stop.Lat, Longitude: stop.Lng,
                OutletId: nearest.o?.Id, OutletName: nearest.o?.Name,
                Detail: nearest.o is null
                    ? "No route outlet nearby"
                    : $"Near {nearest.o.Name} ({Math.Round(nearest.d)} m)"));
        }

        // ── Activities with dwell + time since the previous one ─────────────
        var dayStart = Min(pings.FirstOrDefault()?.RecordedAt, activities.FirstOrDefault()?.At);
        var dayEnd = Max(pings.LastOrDefault()?.RecordedAt, activities.LastOrDefault()?.At);

        var longestIdle = 0;
        DateTimeOffset? previous = null;
        foreach (var a in activities)
        {
            var since = previous is { } p ? Minutes(a.At - p)
                      : dayStart is { } s ? Minutes(a.At - s)
                      : (int?)null;
            if (previous is not null && since is { } idle)
                longestIdle = Math.Max(longestIdle, idle);
            previous = a.At;

            events.Add(a.Event with
            {
                DwellMinutes = dwell.TryGetValue(a, out var d) ? d : null,
                SinceLastActivityMinutes = since
            });
        }

        // ── Unlock requests ─────────────────────────────────────────────────
        foreach (var u in unlocks)
        {
            events.Add(new RepTimelineEventDto(
                Kind: K.Unlock, At: Utc(u.PerformedAt),
                Latitude: u.Action == "Requested" ? u.Latitude : null,
                Longitude: u.Action == "Requested" ? u.Longitude : null,
                Detail: UnlockDetail(u)));
        }

        // ── Bookends ────────────────────────────────────────────────────────
        if (dayStart is { } start)
        {
            var first = pings.FirstOrDefault();
            events.Add(new RepTimelineEventDto(
                Kind: K.DayStart, At: start,
                Latitude: first?.Latitude, Longitude: first?.Longitude));
        }
        if (dayEnd is { } end && dayEnd != dayStart)
        {
            var last = pings.LastOrDefault();
            events.Add(new RepTimelineEventDto(
                Kind: K.DayEnd, At: end,
                Latitude: last?.Latitude, Longitude: last?.Longitude));
        }

        var ordered = events
            .OrderBy(e => e.At)
            .ThenBy(e => e.Kind == K.DayStart ? 0 : e.Kind == K.DayEnd ? 2 : 1)
            .ToList();

        // ── Summary ─────────────────────────────────────────────────────────
        var live = activities.Where(a => !a.Cancelled).ToList();
        var covered = live.Where(a => a.OutletId.HasValue).Select(a => a.OutletId!.Value).ToHashSet();
        var routeIds = routeOutlets.Select(o => o.Id).ToHashSet();
        int? planned = assignment is null ? null : routeOutlets.Count;
        double? coverage = planned is > 0
            ? Math.Round(covered.Count(routeIds.Contains) * 100.0 / planned.Value, 1)
            : null;

        var summary = new RepTimelineSummaryDto(
            DayStartAt: dayStart,
            DayEndAt: dayEnd,
            WorkingMinutes: dayStart is { } ds && dayEnd is { } de ? Minutes(de - ds) : 0,
            BillCount: bills.Count(b => !b.Cancelled),
            BillRevenue: bills.Where(b => !b.Cancelled).Sum(b => b.TotalAmount),
            CancelledBillCount: bills.Count(b => b.Cancelled),
            NoSaleCount: visits.Count,
            OutletsCovered: covered.Count,
            PlannedOutlets: planned,
            CoveragePercent: coverage,
            FirstActivityAt: activities.FirstOrDefault()?.At,
            GpsGapCount: gapCount,
            GpsGapMinutes: gapMinutes,
            UnrecordedStopCount: unrecordedStops,
            OutOfRangeBillCount: bills.Count(b => b.ProximityOverridden && !b.Cancelled),
            LateSyncCount: ordered.Count(e => e.SyncedLate == true),
            LongestIdleMinutes: longestIdle);

        return (summary, ordered);
    }

    private record Stop(DateTimeOffset Start, DateTimeOffset End, double Lat, double Lng);

    /// Consecutive pings that stay within <see cref="StopRadiusMeters"/> of the first one, with
    /// no GPS gap inside, for at least <see cref="MinStop"/>.
    private static List<Stop> DetectStops(List<RepRoutePointDto> pings, TimeSpan gap)
    {
        var stops = new List<Stop>();
        var i = 0;
        while (i < pings.Count)
        {
            var anchor = pings[i];
            var j = i;
            while (j + 1 < pings.Count
                   && pings[j + 1].RecordedAt - pings[j].RecordedAt <= gap
                   && GeoMath.HaversineMeters(anchor.Latitude, anchor.Longitude,
                                               pings[j + 1].Latitude, pings[j + 1].Longitude) <= StopRadiusMeters)
                j++;

            if (j > i && pings[j].RecordedAt - anchor.RecordedAt >= MinStop)
            {
                var run = pings.Skip(i).Take(j - i + 1).ToList();
                stops.Add(new Stop(anchor.RecordedAt, pings[j].RecordedAt,
                    run.Average(p => p.Latitude), run.Average(p => p.Longitude)));
                i = j + 1;
            }
            else
            {
                i++;
            }
        }
        return stops;
    }

    private static (DateTimeOffset At, string Source, bool Late) TimeOf(DateTime? captured, DateTime created)
    {
        var createdUtc = Utc(created);
        if (captured is not { } c) return (createdUtc, "Server", false);
        var capturedUtc = Utc(c);
        return (capturedUtc, "Device", createdUtc - capturedUtc > LateSync);
    }

    private static string UnlockDetail(TimelineUnlockEvent u)
    {
        var who = u.PerformedByName is null ? u.PerformedByRole : $"{u.PerformedByName} ({u.PerformedByRole})";
        return u.Action switch
        {
            "Requested" => $"Unlock requested: {u.Note}",
            "Approved" => $"Unlock approved by {who}",
            "Rejected" => $"Unlock rejected by {who}: {u.Note}",
            "Cancelled" => "Unlock request cancelled",
            "Revoked" => $"Unlock revoked by {who}: {u.Note}",
            _ => $"Unlock {u.Action.ToLowerInvariant()} by {who}"
        };
    }

    /// EF returns timestamptz as UTC on Postgres but Unspecified on SQLite — both are UTC.
    private static DateTimeOffset Utc(DateTime d)
        => new(DateTime.SpecifyKind(d, DateTimeKind.Utc));

    private static int Minutes(TimeSpan t) => (int)Math.Round(t.TotalMinutes);

    private static DateTimeOffset? Min(DateTimeOffset? a, DateTimeOffset? b)
        => a is null ? b : b is null ? a : (a < b ? a : b);

    private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset? b)
        => a is null ? b : b is null ? a : (a > b ? a : b);
}
