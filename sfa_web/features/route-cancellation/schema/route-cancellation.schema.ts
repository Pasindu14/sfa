import { z } from 'zod'

// ── Enums ──────────────────────────────────────────────────────────────────

export const DeletionStatus = {
  None: 0,
  PendingApproval: 1,
  Approved: 2,
  Rejected: 3,
} as const

export type DeletionStatusValue = (typeof DeletionStatus)[keyof typeof DeletionStatus]

export const deletionStatusLabels: Record<DeletionStatusValue, string> = {
  0: 'None',
  1: 'Pending Approval',
  2: 'Approved',
  3: 'Rejected',
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
  date: string
  userId: number
  userName: string
  routeId: number
  routeCode: string
  routeName: string
  deletionStatus: DeletionStatusValue
  deletionRequestedAt: string | null
  deletionRequestReason: string | null
  deletionRejectionReason: string | null
  isActive: boolean
  createdAt: string
}

export type RouteCancellationListDto = {
  assignments: RouteCancellationDto[]
  totalCount: number
  page: number
  pageSize: number
}
