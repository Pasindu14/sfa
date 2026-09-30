import { z } from 'zod'

/**
 * One GPS fix on a rep's route.
 *
 * `recordedAt` is the device clock — the real moment the position was captured, and the
 * order the rep actually travelled in. `receivedAt` is the server clock at upload; a wide
 * gap between the two means the ping sat in the phone's offline outbox and was back-filled
 * later, which is worth being able to see rather than smoothing over.
 */
export const repRoutePointSchema = z.object({
  latitude: z.number(),
  longitude: z.number(),
  accuracy: z.number(),
  recordedAt: z.string(),
  receivedAt: z.string(),
})

export const repRouteSummarySchema = z.object({
  pointCount: z.number(),
  firstPingAt: z.string().nullable(),
  lastPingAt: z.string().nullable(),
  /** Straight-line distance across observed stretches only — gaps are excluded. */
  measuredDistanceMeters: z.number(),
  gapCount: z.number(),
  /** The server's gap rule. Used for drawing too, so dashes and distance always agree. */
  gapThresholdMinutes: z.number(),
})

/**
 * Why the phone last failed to record a position. Current state, not tied to the queried
 * date — it exists so an empty map can explain itself instead of looking the same whether
 * the service died or every fix was rejected.
 */
export const repTrackingStatusSchema = z.object({
  reason: z.string(),
  accuracyMeters: z.number().nullable(),
  reportedAt: z.string(),
})

export const repRouteSchema = z.object({
  repId: z.number(),
  repName: z.string(),
  date: z.string(),
  summary: repRouteSummarySchema,
  points: z.array(repRoutePointSchema),
  trackingStatus: repTrackingStatusSchema.nullable(),
})

/** Minimal shape needed to render the rep picker — not the full user record. */
export const repOptionSchema = z.object({
  id: z.number(),
  name: z.string(),
  username: z.string().optional(),
  role: z.string(),
  isActive: z.boolean(),
})

/**
 * The rep's route assignment for the day. Null on the parent when nothing was assigned —
 * coverage then has no denominator and is shown as a bare count.
 */
export const repTimelineAssignmentSchema = z.object({
  routeId: z.number(),
  routeName: z.string(),
  plannedOutlets: z.number(),
})

export const repTimelineSummarySchema = z.object({
  /** First / last of pings + activities. Null only when the day is completely empty. */
  dayStartAt: z.string().nullable(),
  dayEndAt: z.string().nullable(),
  workingMinutes: z.number(),
  billCount: z.number(),
  /** Excludes bills the rep cancelled. */
  billRevenue: z.number(),
  cancelledBillCount: z.number(),
  noSaleCount: z.number(),
  /** Distinct outlets billed or visited (non-cancelled). */
  outletsCovered: z.number(),
  plannedOutlets: z.number().nullable(),
  coveragePercent: z.number().nullable(),
  /** First bill / no-sale — distinct from day start, which can be an early ping. */
  firstActivityAt: z.string().nullable(),
  gpsGapCount: z.number(),
  gpsGapMinutes: z.number(),
  unrecordedStopCount: z.number(),
  outOfRangeBillCount: z.number(),
  lateSyncCount: z.number(),
  longestIdleMinutes: z.number().nullable(),
})

/**
 * API enums are serialized as their member names, so these are string unions — a numeric
 * map here would make every comparison silently false.
 */
export const repTimelineEventKindSchema = z.enum([
  'DayStart',
  'Bill',
  'NoSale',
  'Stop',
  'GpsGap',
  'Unlock',
  'DayEnd',
])

/**
 * One row of the day. Every field is always present; the ones irrelevant to `kind` are null.
 *
 * `at` is the device capture time when the phone sent one, otherwise the server receive
 * time — `timeSource: 'Server'` flags the latter, because an offline bill synced hours later
 * then sits at its sync time rather than when it was made.
 */
export const repTimelineEventSchema = z.object({
  kind: repTimelineEventKindSchema,
  at: z.string(),
  /** Stop / GpsGap only. */
  endAt: z.string().nullable(),
  minutes: z.number().nullable(),
  /** Bill: rep position · NoSale: outlet · Stop: centroid · GpsGap: last known fix. */
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  outletId: z.number().nullable(),
  outletName: z.string().nullable(),
  billingId: z.number().nullable(),
  billingNumber: z.string().nullable(),
  amount: z.number().nullable(),
  cancelled: z.boolean().nullable(),
  distanceFromOutletMeters: z.number().nullable(),
  outOfRange: z.boolean().nullable(),
  /** NoSale: reason code · Stop: nearest outlet · Unlock: request / decision text. */
  detail: z.string().nullable(),
  syncedLate: z.boolean().nullable(),
  timeSource: z.enum(['Device', 'Server']).nullable(),
  /** Bill / NoSale matched to a GPS stop — how long the rep stood there. */
  dwellMinutes: z.number().nullable(),
  /** Bill / NoSale: minutes since the previous Bill / NoSale (the first: since day start). */
  sinceLastActivityMinutes: z.number().nullable(),
})

/**
 * Everything the Rep Day page needs in one call. The route is embedded rather than fetched
 * separately because Next.js runs server actions one at a time — two calls would queue.
 */
export const repDayTimelineSchema = z.object({
  repId: z.number(),
  repName: z.string(),
  date: z.string(),
  assignment: repTimelineAssignmentSchema.nullable(),
  summary: repTimelineSummarySchema,
  events: z.array(repTimelineEventSchema),
  route: repRouteSchema,
})

export type RepTimelineAssignmentDto = z.infer<typeof repTimelineAssignmentSchema>
export type RepTimelineSummaryDto = z.infer<typeof repTimelineSummarySchema>
export type RepTimelineEventKind = z.infer<typeof repTimelineEventKindSchema>
export type RepTimelineEventDto = z.infer<typeof repTimelineEventSchema>
export type RepDayTimelineDto = z.infer<typeof repDayTimelineSchema>
export type RepTrackingStatusDto = z.infer<typeof repTrackingStatusSchema>
export type RepRoutePointDto = z.infer<typeof repRoutePointSchema>
export type RepRouteSummaryDto = z.infer<typeof repRouteSummarySchema>
export type RepRouteDto = z.infer<typeof repRouteSchema>
export type RepOptionDto = z.infer<typeof repOptionSchema>
