using FluentAssertions;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.RepTimelines.DTOs;
using sfa_api.Features.RepTimelines.Services;

namespace sfa_api.UnitTests.Features.RepTimelines;

public class RepTimelineBuilderTests
{
    private const int Threshold = 15;
    private const double BaseLat = 7.0, BaseLng = 81.0;
    // ~0.0001 degrees of latitude is roughly 11 m.
    private const double Deg100M = 0.0009;

    private static readonly DateTime T0 = new(2026, 9, 30, 4, 0, 0, DateTimeKind.Utc);
    private static DateTime Min(int m) => T0.AddMinutes(m);
    private static DateTimeOffset MinO(int m) => new(Min(m));

    private static RepRoutePointDto Ping(int minute, double lat = BaseLat, double lng = BaseLng)
        => new(lat, lng, 5f, MinO(minute), MinO(minute));

    /// A ping ~1 km further from the origin per step, so it is never part of a stop.
    private static RepRoutePointDto Moving(int minute, int step)
        => Ping(minute, BaseLat + step * 0.01);

    private static TimelineBill Bill(
        int id = 1, int outletId = 1, int minute = 0, int? capturedMinute = null, int? createdMinute = null,
        bool cancelled = false, bool overridden = false, decimal amount = 100m)
        => new(id, $"B-{id:D4}", outletId, $"Outlet{outletId}",
            capturedMinute is null ? null : Min(capturedMinute.Value),
            Min(createdMinute ?? minute),
            BaseLat, BaseLng, amount, cancelled, 12.5, overridden);

    private static TimelineVisit Visit(
        int id = 1, int outletId = 1, int minute = 0, double lat = BaseLat, double lng = BaseLng)
        => new(id, outletId, $"Outlet{outletId}", lat, lng, "OutletClosed", null, Min(minute));

    private static TimelineOutlet RouteOutlet(int id, double lat = BaseLat, double lng = BaseLng)
        => new(id, $"Outlet{id}", lat, lng);

    private static (RepTimelineSummaryDto Summary, IReadOnlyList<RepTimelineEventDto> Events) Run(
        IReadOnlyList<RepRoutePointDto>? pings = null,
        IReadOnlyList<TimelineBill>? bills = null,
        IReadOnlyList<TimelineVisit>? visits = null,
        IReadOnlyList<TimelineUnlockEvent>? unlocks = null,
        IReadOnlyList<TimelineOutlet>? outlets = null,
        TimelineAssignment? assignment = null)
        => RepTimelineBuilder.Build(
            pings ?? [], Threshold, bills ?? [], visits ?? [], unlocks ?? [], outlets ?? [], assignment);

    private static List<RepTimelineEventDto> OfKind(IReadOnlyList<RepTimelineEventDto> e, string kind)
        => e.Where(x => x.Kind == kind).ToList();

    // ── Empty day ────────────────────────────────────────────────────────

    [Fact]
    public void Build_EmptyDay_HasNoEventsAndZeroSummary()
    {
        var (summary, events) = Run();

        events.Should().BeEmpty();
        summary.DayStartAt.Should().BeNull();
        summary.DayEndAt.Should().BeNull();
        summary.FirstActivityAt.Should().BeNull();
        summary.WorkingMinutes.Should().Be(0);
        summary.BillCount.Should().Be(0);
        summary.BillRevenue.Should().Be(0m);
        summary.NoSaleCount.Should().Be(0);
        summary.GpsGapCount.Should().Be(0);
        summary.UnrecordedStopCount.Should().Be(0);
        summary.LongestIdleMinutes.Should().Be(0);
        summary.CoveragePercent.Should().BeNull();
    }

    // ── GPS gaps ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_PingsMoreThanThresholdApart_EmitsGpsGapWithMinutes()
    {
        var (summary, events) = Run(pings: [Moving(0, 0), Moving(16, 1)]);

        var gap = OfKind(events, "GpsGap").Should().ContainSingle().Subject;
        gap.Minutes.Should().Be(16);
        gap.At.Should().Be(MinO(0));
        gap.EndAt.Should().Be(MinO(16));
        gap.Latitude.Should().Be(BaseLat);
        summary.GpsGapCount.Should().Be(1);
        summary.GpsGapMinutes.Should().Be(16);
    }

    [Fact]
    public void Build_PingsExactlyThresholdApart_NoGpsGap()
    {
        var (summary, events) = Run(pings: [Moving(0, 0), Moving(15, 1)]);

        OfKind(events, "GpsGap").Should().BeEmpty();
        summary.GpsGapCount.Should().Be(0);
    }

    // ── Stops ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_PingsWithinRadiusForTenMinutes_EmitsStopNamingNearestRouteOutlet()
    {
        var pings = new[] { Ping(0), Ping(5), Ping(10) };
        var outlets = new[]
        {
            RouteOutlet(1, BaseLat + Deg100M),          // ~100 m
            RouteOutlet(2, BaseLat + 0.0005),           // ~55 m, nearest
            RouteOutlet(3, BaseLat + 0.01),             // ~1.1 km
        };

        var (summary, events) = Run(pings: pings, outlets: outlets);

        var stop = OfKind(events, "Stop").Should().ContainSingle().Subject;
        stop.Minutes.Should().Be(10);
        stop.At.Should().Be(MinO(0));
        stop.EndAt.Should().Be(MinO(10));
        stop.OutletId.Should().Be(2);
        stop.OutletName.Should().Be("Outlet2");
        stop.Detail.Should().StartWith("Near Outlet2 (").And.EndWith(" m)");
        summary.UnrecordedStopCount.Should().Be(1);
    }

    [Fact]
    public void Build_StopWithNoRouteOutletWithin150m_SaysNoOutletNearby()
    {
        var outlets = new[] { RouteOutlet(1, BaseLat + 0.01) };   // ~1.1 km away

        var (_, events) = Run(pings: [Ping(0), Ping(5), Ping(10)], outlets: outlets);

        var stop = OfKind(events, "Stop").Should().ContainSingle().Subject;
        stop.OutletId.Should().BeNull();
        stop.OutletName.Should().BeNull();
        stop.Detail.Should().Be("No route outlet nearby");
    }

    [Fact]
    public void Build_StayShorterThanTenMinutes_IsNotAStop()
    {
        var (summary, events) = Run(pings: [Ping(0), Ping(5), Ping(9)]);

        OfKind(events, "Stop").Should().BeEmpty();
        summary.UnrecordedStopCount.Should().Be(0);
    }

    [Fact]
    public void Build_PingsDriftBeyond120mFromAnchor_DoNotFormAStop()
    {
        // Each ping is inside 120 m of the previous one, but the third is ~200 m from the first.
        var (_, events) = Run(pings: [Ping(0), Ping(5, BaseLat + 0.001), Ping(10, BaseLat + 0.0018)]);

        OfKind(events, "Stop").Should().BeEmpty();
    }

    [Fact]
    public void Build_GapInsideAStay_BreaksTheStop()
    {
        // 0-5 and 25-30 are each only 5 min; the 20 min hole between them is a GPS gap, not stopped time.
        var (summary, events) = Run(pings: [Ping(0), Ping(5), Ping(25), Ping(30)]);

        OfKind(events, "Stop").Should().BeEmpty();
        summary.GpsGapCount.Should().Be(1);
    }

    [Fact]
    public void Build_StopExplainedByBill_IsNotEmittedAndBillGetsDwell()
    {
        var (summary, events) = Run(
            pings: [Ping(0), Ping(5), Ping(10)],
            bills: [Bill(minute: 5)]);

        OfKind(events, "Stop").Should().BeEmpty();
        summary.UnrecordedStopCount.Should().Be(0);
        OfKind(events, "Bill").Single().DwellMinutes.Should().Be(10);
    }

    [Fact]
    public void Build_BillWithinTenMinutesAfterStopEnds_StillExplainsIt()
    {
        var (_, events) = Run(
            pings: [Ping(0), Ping(5), Ping(10), Moving(20, 1)],
            bills: [Bill(minute: 19)]);

        OfKind(events, "Stop").Should().BeEmpty();
        OfKind(events, "Bill").Single().DwellMinutes.Should().Be(10);
    }

    [Fact]
    public void Build_BillMoreThanTenMinutesFromStop_DoesNotExplainIt()
    {
        var (_, events) = Run(
            pings: [Ping(0), Ping(5), Ping(10), Moving(30, 1)],
            bills: [Bill(minute: 25)]);

        OfKind(events, "Stop").Should().ContainSingle();
        OfKind(events, "Bill").Single().DwellMinutes.Should().BeNull();
    }

    [Fact]
    public void Build_StopExplainedByNoSaleVisit_GivesVisitDwell()
    {
        var (_, events) = Run(
            pings: [Ping(0), Ping(6), Ping(12)],
            visits: [Visit(minute: 6)]);

        OfKind(events, "Stop").Should().BeEmpty();
        OfKind(events, "NoSale").Single().DwellMinutes.Should().Be(12);
    }

    // ── Time source / late sync ──────────────────────────────────────────

    [Fact]
    public void Build_BillWithCapturedAt_UsesDeviceTime()
    {
        var (_, events) = Run(bills: [Bill(capturedMinute: 10, createdMinute: 20)]);

        var bill = OfKind(events, "Bill").Single();
        bill.At.Should().Be(MinO(10));
        bill.TimeSource.Should().Be("Device");
    }

    [Fact]
    public void Build_BillWithoutCapturedAt_FallsBackToServerTime()
    {
        var (_, events) = Run(bills: [Bill(minute: 20)]);

        var bill = OfKind(events, "Bill").Single();
        bill.At.Should().Be(MinO(20));
        bill.TimeSource.Should().Be("Server");
        bill.SyncedLate.Should().BeFalse();
    }

    [Fact]
    public void Build_UnspecifiedKindTimestamps_AreTreatedAsUtc()
    {
        var created = DateTime.SpecifyKind(Min(20), DateTimeKind.Unspecified);
        var bill = new TimelineBill(1, "B-1", 1, "O", null, created, null, null, 10m, false, null, false);

        var (_, events) = Run(bills: [bill]);

        OfKind(events, "Bill").Single().At.Should().Be(MinO(20));
    }

    [Fact]
    public void Build_ReceivedMoreThan15MinAfterCapture_IsSyncedLate()
    {
        var (summary, events) = Run(bills: [
            Bill(id: 1, capturedMinute: 0, createdMinute: 16),
            Bill(id: 2, outletId: 2, capturedMinute: 100, createdMinute: 115),   // exactly 15 -> on time
        ]);

        OfKind(events, "Bill").Single(e => e.BillingId == 1).SyncedLate.Should().BeTrue();
        OfKind(events, "Bill").Single(e => e.BillingId == 2).SyncedLate.Should().BeFalse();
        summary.LateSyncCount.Should().Be(1);
    }

    [Fact]
    public void Build_VisitWithCapturedAt_UsesDeviceTimeAndFlagsLateSync()
    {
        var visit = new TimelineVisit(1, 1, "O", BaseLat, BaseLng, "NoOrder", Min(0), Min(60));

        var (summary, events) = Run(visits: [visit]);

        var e = OfKind(events, "NoSale").Single();
        e.At.Should().Be(MinO(0));
        e.TimeSource.Should().Be("Device");
        e.SyncedLate.Should().BeTrue();
        e.Detail.Should().Be("NoOrder");
        summary.LateSyncCount.Should().Be(1);
    }

    // ── Since-last-activity / idle ───────────────────────────────────────

    [Fact]
    public void Build_SinceLastActivity_FirstIsSinceDayStartAndLaterSinceThePreviousActivity()
    {
        var (summary, events) = Run(
            pings: [Ping(0)],
            bills: [Bill(id: 1, outletId: 1, minute: 100), Bill(id: 2, outletId: 2, minute: 160)]);

        var bills = OfKind(events, "Bill");
        bills[0].SinceLastActivityMinutes.Should().Be(100);
        bills[1].SinceLastActivityMinutes.Should().Be(60);
        // The 100 min lead-in is not idle time between activities.
        summary.LongestIdleMinutes.Should().Be(60);
    }

    [Fact]
    public void Build_SingleActivityAndNoPings_HasZeroSinceAndZeroIdle()
    {
        var (summary, events) = Run(bills: [Bill(minute: 30)]);

        OfKind(events, "Bill").Single().SinceLastActivityMinutes.Should().Be(0);
        summary.LongestIdleMinutes.Should().Be(0);
    }

    // ── Bills: cancelled, revenue, out of range ──────────────────────────

    [Fact]
    public void Build_CancelledBill_IsExcludedFromTotalsButStillEmitted()
    {
        var (summary, events) = Run(bills: [
            Bill(id: 1, outletId: 1, minute: 0, amount: 100m),
            Bill(id: 2, outletId: 2, minute: 10, amount: 900m, cancelled: true),
        ]);

        summary.BillCount.Should().Be(1);
        summary.BillRevenue.Should().Be(100m);
        summary.CancelledBillCount.Should().Be(1);
        summary.OutletsCovered.Should().Be(1);
        var cancelled = OfKind(events, "Bill").Single(e => e.BillingId == 2);
        cancelled.Cancelled.Should().BeTrue();
        cancelled.Amount.Should().Be(900m);
    }

    [Fact]
    public void Build_OutOfRangeBills_AreCountedAndFlagged()
    {
        var (summary, events) = Run(bills: [
            Bill(id: 1, outletId: 1, minute: 0, overridden: true),
            Bill(id: 2, outletId: 2, minute: 10),
            Bill(id: 3, outletId: 3, minute: 20, overridden: true, cancelled: true),
        ]);

        summary.OutOfRangeBillCount.Should().Be(1);
        OfKind(events, "Bill").Single(e => e.BillingId == 1).OutOfRange.Should().BeTrue();
        OfKind(events, "Bill").Single(e => e.BillingId == 2).OutOfRange.Should().BeFalse();
    }

    // ── Coverage ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_Coverage_CountsDistinctLiveOutletsOnTheRouteOverPlanned()
    {
        var routeOutlets = new[] { RouteOutlet(1), RouteOutlet(2), RouteOutlet(3), RouteOutlet(4) };

        var (summary, _) = Run(
            bills: [
                Bill(id: 1, outletId: 1, minute: 0),
                Bill(id: 2, outletId: 1, minute: 10),                    // same outlet again
                Bill(id: 3, outletId: 2, minute: 20, cancelled: true),   // cancelled: not covered
                Bill(id: 4, outletId: 99, minute: 30),                   // off-route outlet
            ],
            visits: [Visit(outletId: 3, minute: 40)],
            outlets: routeOutlets,
            assignment: new TimelineAssignment(7, "Route 7"));

        summary.OutletsCovered.Should().Be(3);       // 1, 3, 99
        summary.PlannedOutlets.Should().Be(4);
        summary.CoveragePercent.Should().Be(50.0);   // only 1 and 3 are on the route
    }

    [Fact]
    public void Build_NoAssignment_CoverageAndPlannedAreNull()
    {
        var (summary, _) = Run(bills: [Bill()], outlets: [RouteOutlet(1)], assignment: null);

        summary.PlannedOutlets.Should().BeNull();
        summary.CoveragePercent.Should().BeNull();
    }

    // ── No-sale coordinates ──────────────────────────────────────────────

    [Fact]
    public void Build_NoSaleWithZeroZeroOutletCoords_HasNullLatLng()
    {
        var (_, events) = Run(visits: [Visit(lat: 0, lng: 0)]);

        var e = OfKind(events, "NoSale").Single();
        e.Latitude.Should().BeNull();
        e.Longitude.Should().BeNull();
    }

    [Fact]
    public void Build_NoSaleWithRealOutletCoords_CarriesThem()
    {
        var (_, events) = Run(visits: [Visit(lat: 6.5, lng: 80.5)]);

        var e = OfKind(events, "NoSale").Single();
        e.Latitude.Should().Be(6.5);
        e.Longitude.Should().Be(80.5);
    }

    // ── Unlock events ────────────────────────────────────────────────────

    [Fact]
    public void Build_UnlockEvents_HaveReadableDetails()
    {
        var unlocks = new[]
        {
            new TimelineUnlockEvent("Requested", Min(5), "Rep", "SalesRep", "GPS not accurate", 6.9, 79.8),
            new TimelineUnlockEvent("Approved", Min(9), "Dhanushka", "Supervisor", "ok", null, null),
        };

        var (_, events) = Run(unlocks: unlocks);

        var u = OfKind(events, "Unlock");
        u.Should().HaveCount(2);
        u[0].Detail.Should().Be("Unlock requested: GPS not accurate");
        u[0].Latitude.Should().Be(6.9);
        u[0].Longitude.Should().Be(79.8);
        u[1].Detail.Should().Be("Unlock approved by Dhanushka (Supervisor)");
        u[1].Latitude.Should().BeNull();
    }

    // ── Ordering / bookends ──────────────────────────────────────────────

    [Fact]
    public void Build_Events_AreOrderedByTimeWithDayStartFirstAndDayEndLast()
    {
        var (summary, events) = Run(
            pings: [Moving(0, 0), Moving(10, 1), Moving(40, 2)],
            bills: [Bill(minute: 30)],
            visits: [Visit(id: 1, outletId: 2, minute: 5)],
            unlocks: [new TimelineUnlockEvent("Requested", Min(20), null, "SalesRep", "why", null, null)]);

        events.Select(e => e.Kind).Should().Equal("DayStart", "NoSale", "GpsGap", "Unlock", "Bill", "DayEnd");
        events.Select(e => e.At).Should().BeInAscendingOrder();
        events.First().At.Should().Be(MinO(0));
        events.Last().At.Should().Be(MinO(40));
        summary.DayStartAt.Should().Be(MinO(0));
        summary.DayEndAt.Should().Be(MinO(40));
        summary.WorkingMinutes.Should().Be(40);
        summary.FirstActivityAt.Should().Be(MinO(5));
    }

    [Fact]
    public void Build_ActivityBeforeFirstPing_MovesDayStartEarlier()
    {
        var (summary, events) = Run(pings: [Ping(30)], bills: [Bill(minute: 10)]);

        summary.DayStartAt.Should().Be(MinO(10));
        events.First().Kind.Should().Be("DayStart");
        events.First().At.Should().Be(MinO(10));
    }

    [Fact]
    public void Build_SingleInstantDay_EmitsOnlyDayStart()
    {
        var (_, events) = Run(pings: [Ping(0)]);

        events.Select(e => e.Kind).Should().Equal("DayStart");
    }
}
