import { z } from 'zod'

export const createUserReportingLineSchema = z.object({
  userId: z.number().min(1, 'User is required'),
  reportsToUserId: z.number().min(1, 'Manager is required'),
  effectiveFrom: z.string().min(1, 'Effective date is required'),
})

export const updateUserReportingLineSchema = z.object({
  reportsToUserId: z.number().min(1, 'Manager is required'),
  effectiveFrom: z.string().min(1, 'Effective date is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  role: z.string().optional(),
  reportsToUserId: z.number().optional(),
  isActive: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateUserReportingLineInput = z.infer<typeof createUserReportingLineSchema>
export type UpdateUserReportingLineInput = z.infer<typeof updateUserReportingLineSchema>
export type UserReportingLineFilterInput = z.infer<typeof filterSchema>

export type UserReportingLineDto = {
  id: number
  userId: number
  userName: string
  userRole: string
  reportsToUserId: number
  reportsToUserName: string
  reportsToUserRole: string
  effectiveFrom: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
