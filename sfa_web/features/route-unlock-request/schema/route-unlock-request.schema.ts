import { z } from 'zod'

// ── Enums ──────────────────────────────────────────────────────────────────

// The API serializes enums by member name (JsonStringEnumConverter), not by number.
// `effectiveStatus` is `status` plus the read-only "Expired" state: a Pending request
// from a past day, or an Approved one past its validTo.
export const RouteUnlockStatus = {
  Pending: 'Pending',
  Approved: 'Approved',
  Expired: 'Expired',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Revoked: 'Revoked',
} as const

export type RouteUnlockStatusValue = (typeof RouteUnlockStatus)[keyof typeof RouteUnlockStatus]

export const routeUnlockStatusLabels: Record<RouteUnlockStatusValue, string> = {
  Pending: 'Pending',
  Approved: 'Approved',
  Expired: 'Expired',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Revoked: 'Revoked',
}

export type RouteUnlockEventAction =
  | 'Requested'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled'
  | 'Revoked'

// ── Action schemas ─────────────────────────────────────────────────────────

export const approveRouteUnlockSchema = z.object({
  note: z.string().max(500, 'Note must not exceed 500 characters').optional(),
})

export type ApproveRouteUnlockInput = z.infer<typeof approveRouteUnlockSchema>

/** Shared by reject and revoke — both require a reason. */
export const routeUnlockReasonSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(3, 'Reason must be at least 3 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})

export type RouteUnlockReasonInput = z.infer<typeof routeUnlockReasonSchema>

// ── DTO types (match API camelCase response) ───────────────────────────────

export type RouteUnlockRequestDto = {
  id: number
  userId: number
  userName: string
  loginName: string
  routeId: number
  routeName: string
  businessDate: string
  status: Exclude<RouteUnlockStatusValue, 'Expired'>
  effectiveStatus: RouteUnlockStatusValue
  isCurrentlyEffective: boolean
  requestReason: string
  requestedAt: string
  requestLatitude: number | null
  requestLongitude: number | null
  requestGpsAccuracyMeters: number | null
  supervisorUserId: number | null
  supervisorName: string | null
  reviewedByUserId: number | null
  reviewedByName: string | null
  reviewedByRole: string | null
  reviewedAt: string | null
  reviewNote: string | null
  validFrom: string | null
  validTo: string | null
  revokedByUserId: number | null
  revokedByName: string | null
  revokedAt: string | null
  revokeReason: string | null
  cancelledAt: string | null
  rowVersion: number
}

export type RouteUnlockRequestEventDto = {
  id: number
  action: RouteUnlockEventAction
  fromStatus: string | null
  toStatus: string
  performedByUserId: number
  performedByName: string | null
  performedByRole: string
  performedAt: string
  note: string | null
  ipAddress: string | null
}

export type RouteUnlockBillDto = {
  billingId: number
  billingNumber: string
  billingDate: string
  outletId: number
  outletName: string
  distanceFromOutletMeters: number | null
  totalAmount: number
  createdAt: string
}

export type RouteUnlockRequestDetailDto = {
  request: RouteUnlockRequestDto
  events: RouteUnlockRequestEventDto[]
  bills: RouteUnlockBillDto[]
}

export type RouteUnlockListDto = {
  items: RouteUnlockRequestDto[]
  totalCount: number
  page: number
  pageSize: number
}

export type RouteUnlockListFilters = {
  status?: RouteUnlockStatusValue
  from?: string
  to?: string
}
