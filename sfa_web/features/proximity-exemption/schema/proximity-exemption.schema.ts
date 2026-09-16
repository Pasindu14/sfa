import { z } from 'zod'

export const exemptionReasonEnum = z.enum([
  'BadOutletCoordinates',
  'SharedCoordinateMarket',
  'DeviceGpsFault',
  'ManagementApproval',
])

export type ExemptionReason = z.infer<typeof exemptionReasonEnum>

/// Labels the admin actually reads. The enum members are the wire values.
export const exemptionReasonLabels: Record<ExemptionReason, string> = {
  BadOutletCoordinates: 'Outlet coordinates are wrong',
  SharedCoordinateMarket: 'Wholesale market / shared coordinate',
  DeviceGpsFault: 'Device GPS fault',
  ManagementApproval: 'Management approval',
}

/// Mirrors BillingGeoOptions.MaxExemptionDays on the API. The server rejects
/// anything longer regardless — this only spares the admin a round-trip.
export const MAX_EXEMPTION_DAYS = 30

export const grantExemptionSchema = z.object({
  // Plain YYYY-MM-DD, matching how every other date travels in this app.
  validUntil: z.string().min(1, 'Choose the last day the exemption applies'),
  reason: exemptionReasonEnum,
  notes: z.string().max(500, 'Notes must not exceed 500 characters').optional(),
})

export type GrantExemptionInput = z.infer<typeof grantExemptionSchema>

export type ProximityExemptionDto = {
  id: number
  userId: number
  name: string
  username: string
  validFrom: string
  validTo: string
  validUntilDate: string
  reason: ExemptionReason
  notes: string | null
  grantedByUserId: number
  grantedByUserName: string | null
  revokedAt: string | null
  revokedByUserId: number | null
  isActive: boolean
  isCurrentlyEffective: boolean
  rowVersion: number
  createdAt: string
  updatedAt: string
}
