import { z } from 'zod'

export const createDailyRouteAssignmentSchema = z.object({
  userId: z.number().min(1, 'Sales rep is required'),
  routeId: z.number().min(1, 'Route is required'),
  assignedDate: z.string().min(1, 'Assigned date is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  userId: z.number().optional(),
  routeId: z.number().optional(),
  date: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateDailyRouteAssignmentInput = z.infer<typeof createDailyRouteAssignmentSchema>
export type DailyRouteAssignmentFilterInput = z.infer<typeof filterSchema>

export type DailyRouteAssignmentDeletionStatus = 'None' | 'PendingApproval' | 'Approved' | 'Rejected'

export type DailyRouteAssignmentDto = {
  id: number
  userId: number
  userName: string
  routeId: number
  routeName: string
  assignedDate: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  deletionStatus: DailyRouteAssignmentDeletionStatus
  deletionRequestedAt?: string | null
  deletionRequestReason?: string | null
  deletionRejectionReason?: string | null
}

/** A supervisor's direct-report row, as returned by GET /user-reporting-lines/{id}/subordinates */
export type SupervisorRepDto = {
  userId: number
  userName: string
  userRole: string
  isActive: boolean
}

/** A route available to a rep, as returned by GET /daily-route-assignments/rep-routes/{userId} */
export type RepRouteDto = {
  routeId: number
  routeName: string
}
