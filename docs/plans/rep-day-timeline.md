# Rep Day Timeline

## Context
Admins can already see *where* a rep went (Rep Route History: GPS trail) but not *what they did*
along the way. This adds one scrolling timeline of a rep's business day, alongside the existing map on
`/rep-route-history`: day start → each bill / no-sale visit (with dwell time and time since the last
activity) → GPS gaps → stops where nothing was recorded → unlock requests → day end, plus day KPIs
(route coverage, first activity, idle time, late syncs, out-of-range bills).

**Accuracy fix bundled in:** bills and no-sale visits only stored the *server receive* time, so an
offline bill made at 10:00 and synced at 14:00 would sit at 14:00. The phone now sends `capturedAt`
(its local create time); the server stores it (`Billings.CapturedAt`, `NotBillings.CapturedAt`) when it
is plausible (not in the future, not > 3 days before receipt). Rows without it fall back to server time
and are marked `timeSource: "Server"`.

## API — `GET /api/v1/rep-timeline/{repId}?date=yyyy-MM-dd` (Admin)

`RepDayTimelineDto`
```jsonc
{
  "repId": 17, "repName": "Nuwan Sanjaya", "date": "2026-09-30",
  "assignment": { "routeId": 771, "routeName": "Uhana 01", "plannedOutlets": 25 },   // null if none that day
  "summary": {
    "dayStartAt": "…Z", "dayEndAt": "…Z",          // first/last of pings + activities; null if nothing
    "workingMinutes": 412,
    "billCount": 9, "billRevenue": 48210.50, "cancelledBillCount": 1,
    "noSaleCount": 4,
    "outletsCovered": 12,                         // distinct outlets billed or visited (non-cancelled)
    "plannedOutlets": 25, "coveragePercent": 48.0, // null when no assignment
    "firstActivityAt": "…Z",                      // first bill / no-sale
    "gpsGapCount": 2, "gpsGapMinutes": 55,
    "unrecordedStopCount": 3,
    "outOfRangeBillCount": 1,                     // Billing.ProximityOverridden
    "lateSyncCount": 2,                           // synced > 15 min after capture
    "longestIdleMinutes": 74                      // longest time between consecutive activities
  },
  "events": [ /* RepTimelineEventDto, ordered by `at` */ ],
  "route": { /* existing RepRouteDto: repId, repName, date, summary, points[], trackingStatus */ }
}
```

`RepTimelineEventDto` (all fields present; irrelevant ones null)
```jsonc
{
  "kind": "DayStart" | "Bill" | "NoSale" | "Stop" | "GpsGap" | "Unlock" | "DayEnd",
  "at": "…Z", "endAt": "…Z" | null, "minutes": 25 | null,        // Stop / GpsGap duration
  "latitude": 7.1, "longitude": 81.6,                              // Bill: rep position; NoSale: outlet; Stop: centroid; GpsGap: last known
  "outletId": 5, "outletName": "Kamal Stores",                     // Bill / NoSale / Stop (nearest route outlet ≤150 m)
  "billingId": 88, "billingNumber": "B-0088", "amount": 5120.00,
  "cancelled": false,
  "distanceFromOutletMeters": 34.2, "outOfRange": false,
  "detail": "OutletClosed" | "Near Kamal Stores (42 m)" | "Requested: GPS not accurate" | "Approved by Dhanushka (Supervisor)" | null,
  "syncedLate": false, "timeSource": "Device" | "Server" | null,
  "dwellMinutes": 12 | null,                  // Bill / NoSale matched to a GPS stop
  "sinceLastActivityMinutes": 38 | null       // Bill / NoSale: minutes since previous Bill/NoSale (first: since DayStart)
}
```

Rules
- Bills: `SalesRepId = rep AND BillingDate = date`. Revenue excludes `RepStatus = Cancelled`.
- No-sale: `SalesRepId = rep AND NotBillingDate = date`.
- Activity time = `CapturedAt ?? CreatedAt`. `syncedLate` = `CreatedAt − CapturedAt > 15 min`.
- GPS gap: consecutive pings > 15 min apart (same threshold as route summary).
- Stop: consecutive pings staying within 120 m of the first for ≥ 10 min. A stop with a bill/no-sale inside
  [start−10 min, end+10 min] is not emitted; its span becomes that activity's `dwellMinutes`. Otherwise it is
  emitted as `Stop` with the nearest active outlet of the day's route within 150 m.
- Unlock: every `RouteUnlockRequestEvent` of the rep's requests with `BusinessDate = date`.

## Web — extend `/rep-route-history` (feature `sfa_web/features/rep-route`)
- Replace the route call with the timeline call (it embeds `route`), keep all existing map behaviour.
- Layout: map (left, ~2/3) + scrollable timeline panel (right, ~1/3); stacks on small screens.
- KPI tiles: Working time (start–end), Bills (count · revenue), Coverage (covered/planned, %), No-sale visits,
  Idle (longest gap between activities), Flags (GPS gaps · unrecorded stops · out-of-range · late sync).
- Map markers: bills (green), no-sale (slate), unrecorded stops (amber), unlock (violet) in addition to the trail.
  Clicking a timeline item pans/zooms the map to it and highlights its marker; clicking a marker scrolls the list.
- Timeline item shows time (HH:mm Colombo), icon by kind, title (outlet / "Stopped 25 min" / "No GPS for 40 min"),
  sub-line (amount, bill no, reason, distance), badges (Cancelled, Out of range, Synced late, Server time),
  and a small "38 min since last" connector.

## Mobile
- Send `capturedAt` (`createdAt.toUtc().toIso8601String()`) in bill and no-sale create payloads. Ship as a
  Shorebird patch; older builds keep working (field optional).
