'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateDailyRouteAssignmentInput,
  DailyRouteAssignmentDto,
  RepRouteDto,
  SupervisorRepDto,
} from '../schema/daily-route-assignment.schema'
import type { UserDto } from '@/features/user/schema/user.schema'

type DailyRouteAssignmentsListResponse = {
  assignments: DailyRouteAssignmentDto[]
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

type SubordinateReportingLine = {
  userId: number
  userName: string
  userRole: string
  isActive: boolean
}

export const getDailyRouteAssignmentsAction = createAction(
  { name: 'getDailyRouteAssignmentsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 10,
    userId?: number,
    routeId?: number,
    date?: string,
  ) => {
    const res = await client.get('/api/v1/daily-route-assignments', {
      params: {
        page,
        pageSize,
        userId: userId || undefined,
        routeId: routeId || undefined,
        date: date || undefined,
      },
    })
    return res.data.data as DailyRouteAssignmentsListResponse
  },
)

// Shared: load all active users for the Supervisor picker
// Uses a large pageSize to avoid pagination — org is small (~200 users max)
export const getSupervisorsForSelectAction = createAction(
  { name: 'getSupervisorsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/users', {
      params: { page: 1, pageSize: 200, isActive: true },
    })
    return (res.data.data as UsersListResponse).users
  },
)

// Direct reports of a given supervisor
export const getSupervisorRepsAction = createAction(
  { name: 'getSupervisorRepsAction', requireAuth: true, requiredRole: 'Admin' },
  async (supervisorId: number) => {
    const res = await client.get(`/api/v1/user-reporting-lines/${supervisorId}/subordinates`, {
      params: { depth: 1 },
    })
    const lines = res.data.data as SubordinateReportingLine[]
    return lines.map(
      (l): SupervisorRepDto => ({
        userId: l.userId,
        userName: l.userName,
        userRole: l.userRole,
        isActive: l.isActive,
      }),
    )
  },
)

// Routes available to a rep, scoped to their division
export const getRepRoutesAction = createAction(
  { name: 'getRepRoutesAction', requireAuth: true, requiredRole: 'Admin' },
  async (userId: number) => {
    const res = await client.get(`/api/v1/daily-route-assignments/rep-routes/${userId}`)
    return res.data.data as RepRouteDto[]
  },
)

export const createDailyRouteAssignmentAction = createAction(
  { name: 'createDailyRouteAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateDailyRouteAssignmentInput) => {
    const res = await client.post('/api/v1/daily-route-assignments', data)
    revalidatePath('/route-assignments')
    return res.data.data as DailyRouteAssignmentDto
  },
)

export const deleteDailyRouteAssignmentAction = createAction(
  { name: 'deleteDailyRouteAssignmentAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/daily-route-assignments/${id}`)
    revalidatePath('/route-assignments')
  },
)
