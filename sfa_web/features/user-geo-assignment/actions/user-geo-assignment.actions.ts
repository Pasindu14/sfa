'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateUserGeoAssignmentInput,
  UpdateUserGeoAssignmentInput,
  UserAssignmentDto,
  UserAssignmentStatsDto,
} from '../schema/user-geo-assignment.schema'
import type { UserDto } from '@/features/user/schema/user.schema'
import type { RegionDto } from '@/features/region/schema/region.schema'
import type { AreaDto } from '@/features/area/schema/area.schema'
import type { TerritoryDto } from '@/features/territory/schema/territory.schema'
import type { DivisionDto } from '@/features/division/schema/division.schema'

type UserAssignmentsListResponse = {
  userAssignments: UserAssignmentDto[]
  totalCount: number
  page: number
  pageSize: number
}

type UsersListResponse = {
  users: UserDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getUserAssignmentsAction = createAction(
  { name: 'getUserAssignmentsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    role?: string,
    regionId?: number,
    areaId?: number,
    territoryId?: number,
    divisionId?: number,
    isActive?: string,
  ) => {
    const res = await client.get('/api/v1/user-assignments', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        role: role || undefined,
        regionId: regionId || undefined,
        areaId: areaId || undefined,
        territoryId: territoryId || undefined,
        divisionId: divisionId || undefined,
        isActive:
          isActive === 'true' ? true : isActive === 'false' ? false : undefined,
      },
    })
    return res.data.data as UserAssignmentsListResponse
  },
)

export const getUserAssignmentByIdAction = createAction(
  { name: 'getUserAssignmentByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/user-assignments/${id}`)
    return res.data.data as UserAssignmentDto
  },
)

export const getUserAssignmentStatsAction = createAction(
  { name: 'getUserAssignmentStatsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/user-assignments/stats')
    return res.data.data as UserAssignmentStatsDto
  },
)

export const createUserAssignmentAction = createAction(
  { name: 'createUserAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateUserGeoAssignmentInput) => {
    const res = await client.post('/api/v1/user-assignments', data)
    revalidatePath('/geo-assignments')
    return res.data.data as UserAssignmentDto
  },
)

export const updateUserAssignmentAction = createAction(
  { name: 'updateUserAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateUserGeoAssignmentInput) => {
    const res = await client.put(`/api/v1/user-assignments/${id}`, data)
    revalidatePath('/geo-assignments')
    return res.data.data as UserAssignmentDto
  },
)

export const deactivateUserAssignmentAction = createAction(
  { name: 'deactivateUserAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/user-assignments/${id}`)
    revalidatePath('/geo-assignments')
  },
)

export const activateUserAssignmentAction = createAction(
  { name: 'activateUserAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/user-assignments/${id}/activate`)
    revalidatePath('/geo-assignments')
  },
)

// ── Select data loaders ──────────────────────────────────────────────────────
// Pre-load once, shared across form and filter — no extra calls per keystroke.

export const getUsersForGeoSelectAction = createAction(
  { name: 'getUsersForGeoSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/users', { params: { page: 1, pageSize: 200 } })
    return (res.data.data as UsersListResponse).users
  },
)

export const getActiveRegionsForSelectAction = createAction(
  { name: 'getActiveRegionsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/regions/active')
    return res.data.data as RegionDto[]
  },
)

export const getActiveAreasForSelectAction = createAction(
  { name: 'getActiveAreasForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/areas/active')
    return res.data.data as AreaDto[]
  },
)

export const getActiveTerritoriesForSelectAction = createAction(
  { name: 'getActiveTerritoriesForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/territories/active')
    return res.data.data as TerritoryDto[]
  },
)

export const getActiveDivisionsForSelectAction = createAction(
  { name: 'getActiveDivisionsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/divisions/active')
    return res.data.data as DivisionDto[]
  },
)

