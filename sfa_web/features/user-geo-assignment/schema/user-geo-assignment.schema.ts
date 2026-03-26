import { z } from 'zod'

export const createUserGeoAssignmentSchema = z.object({
  userId: z.number().min(1, 'User is required'),
  reportsToUserId: z.number().min(1, 'Manager is required'),
  divisionId: z.number().int().min(1).optional(),
  effectiveFrom: z.string().min(1, 'Effective date is required'),
})

export const updateUserGeoAssignmentSchema = z.object({
  reportsToUserId: z.number().min(1, 'Manager is required'),
  divisionId: z.number().int().min(1).optional(),
  effectiveFrom: z.string().min(1, 'Effective date is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  role: z.string().optional(),
  regionId: z.number().optional(),
  isActive: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateUserGeoAssignmentInput = z.infer<typeof createUserGeoAssignmentSchema>
export type UpdateUserGeoAssignmentInput = z.infer<typeof updateUserGeoAssignmentSchema>
export type UserGeoAssignmentFilterInput = z.infer<typeof filterSchema>

export type UserAssignmentDto = {
  id: number
  userId: number
  userName: string
  userRole: string
  reportsToUserId: number | null
  reportsToUserName: string | null
  divisionId: number | null
  divisionName: string | null
  territoryId: number | null
  territoryName: string | null
  areaId: number | null
  areaName: string | null
  regionId: number | null
  regionName: string | null
  effectiveFrom: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type UserAssignmentStatsDto = {
  totalAssignments: number
  activeAssignments: number
  activeTerritories: number
  assignmentsThisMonth: number
}
