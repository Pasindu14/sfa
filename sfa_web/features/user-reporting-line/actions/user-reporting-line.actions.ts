'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateUserReportingLineInput,
  UpdateUserReportingLineInput,
  UserReportingLineDto,
} from '../schema/user-reporting-line.schema'
import type { UserDto } from '@/features/user/schema/user.schema'

type UserReportingLinesListResponse = {
  userReportingLines: UserReportingLineDto[]
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

export const getUserReportingLinesAction = createAction(
  { name: 'getUserReportingLinesAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    role?: string,
    reportsToUserId?: number,
    isActive?: string,
  ) => {
    const res = await client.get('/api/v1/user-reporting-lines', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        role: role || undefined,
        reportsToUserId: reportsToUserId || undefined,
        isActive: isActive === '' ? undefined : isActive === 'true' ? true : isActive === 'false' ? false : undefined,
      },
    })
    return res.data.data as UserReportingLinesListResponse
  },
)

export const getUserReportingLineByIdAction = createAction(
  { name: 'getUserReportingLineByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/user-reporting-lines/${id}`)
    return res.data.data as UserReportingLineDto
  },
)

export const createUserReportingLineAction = createAction(
  { name: 'createUserReportingLineAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateUserReportingLineInput) => {
    const res = await client.post('/api/v1/user-reporting-lines', data)
    revalidatePath('/user-reporting-lines')
    return res.data.data as UserReportingLineDto
  },
)

export const updateUserReportingLineAction = createAction(
  { name: 'updateUserReportingLineAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateUserReportingLineInput) => {
    const res = await client.put(`/api/v1/user-reporting-lines/${id}`, data)
    revalidatePath('/user-reporting-lines')
    return res.data.data as UserReportingLineDto
  },
)

export const deactivateUserReportingLineAction = createAction(
  { name: 'deactivateUserReportingLineAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/user-reporting-lines/${id}`)
    revalidatePath('/user-reporting-lines')
  },
)

export const activateUserReportingLineAction = createAction(
  { name: 'activateUserReportingLineAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/user-reporting-lines/${id}/activate`)
    revalidatePath('/user-reporting-lines')
  },
)

// Shared: load all assignable users for select dropdowns
// Uses a large pageSize to avoid pagination — org is small (~200 users max)
export const getUsersForSelectAction = createAction(
  { name: 'getUsersForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/users', {
      params: { page: 1, pageSize: 200 },
    })
    return (res.data.data as UsersListResponse).users
  },
)
