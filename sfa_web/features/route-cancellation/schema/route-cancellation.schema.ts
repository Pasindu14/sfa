import { z } from 'zod'

// ── Enums ──────────────────────────────────────────────────────────────────

// The API serializes enums by member name (JsonStringEnumConverter), not by number.
export const DeletionStatus = {
  None: 'None',
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const

export type DeletionStatusValue = (typeof DeletionStatus)[keyof typeof DeletionStatus]

export const deletionStatusLabels: Record<DeletionStatusValue, string> = {
  None: 'None',
  PendingApproval: 'Pending Approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
}

// ── Action schemas ─────────────────────────────────────────────────────────

export const rejectCancellationSchema = z.object({
  reason: z
    .string()
    .min(3, 'Reason must be at least 3 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})

export type RejectCancellationInput = z.infer<typeof rejectCancellationSchema>

// ── DTO types (match API camelCase response) ───────────────────────────────

export type RouteCancellationDto = {
  id: number
  userId: number
  userName: string
  routeId: number
  routeName: string
  assignedDate: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  deletionStatus: DeletionStatusValue
  deletionRequestedAt: string | null
  deletionRequestReason: string | null
  deletionRejectionReason: string | null
}

export type RouteCancellationListDto = {
  assignments: RouteCancellationDto[]
  totalCount: number
  page: number
  pageSize: number
}
