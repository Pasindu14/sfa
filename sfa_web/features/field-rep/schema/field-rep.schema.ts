import { z } from 'zod'

export const repLocationPingSchema = z.object({
  repId: z.number(),
  repName: z.string(),
  latitude: z.number(),
  longitude: z.number(),
  accuracy: z.number(),
  recordedAt: z.string(),
  receivedAt: z.string(),
})

export type RepLocationPingDto = z.infer<typeof repLocationPingSchema>
