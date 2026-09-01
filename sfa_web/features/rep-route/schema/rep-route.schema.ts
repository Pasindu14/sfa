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
  totalDistanceMeters: z.number(),
})

export const repRouteSchema = z.object({
  repId: z.number(),
  repName: z.string(),
  date: z.string(),
  summary: repRouteSummarySchema,
  points: z.array(repRoutePointSchema),
})

/** Minimal shape needed to render the rep picker — not the full user record. */
export const repOptionSchema = z.object({
  id: z.number(),
  name: z.string(),
  username: z.string().optional(),
  role: z.string(),
  isActive: z.boolean(),
})

export type RepRoutePointDto = z.infer<typeof repRoutePointSchema>
export type RepRouteSummaryDto = z.infer<typeof repRouteSummarySchema>
export type RepRouteDto = z.infer<typeof repRouteSchema>
export type RepOptionDto = z.infer<typeof repOptionSchema>
