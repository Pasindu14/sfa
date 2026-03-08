'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateUserInput,
  UpdateUserInput,
  ChangePasswordInput,
  UserDto,
} from '../schema/user.schema'

type UsersListResponse = {
  users: UserDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getUsersAction = createAction(
  { name: 'getUsersAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/users', { params: { page, pageSize } })
    return res.data.data as UsersListResponse
  }
)

export const getUserByIdAction = createAction(
  { name: 'getUserByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/users/${id}`)
    return res.data.data as UserDto
  }
)

export const createUserAction = createAction(
  { name: 'createUserAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateUserInput) => {
    const res = await client.post('/api/v1/users', data)
    revalidatePath('/users')
    return res.data.data as UserDto
  }
)

export const updateUserAction = createAction(
  { name: 'updateUserAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateUserInput) => {
    const res = await client.put(`/api/v1/users/${id}`, data)
    revalidatePath('/users')
    return res.data.data as UserDto
  }
)

export const deleteUserAction = createAction(
  { name: 'deleteUserAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/users/${id}`)
    revalidatePath('/users')
  }
)

export const changePasswordAction = createAction(
  { name: 'changePasswordAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: ChangePasswordInput) => {
    const res = await client.post(`/api/v1/users/${id}/change-password`, data)
    revalidatePath('/users')
    return res.data.data as string
  }
)

export const activateUserAction = createAction(
  { name: 'activateUserAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/users/${id}/activate`)
    revalidatePath('/users')
  }
)

export const deactivateUserAction = createAction(
  { name: 'deactivateUserAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/users/${id}/deactivate`)
    revalidatePath('/users')
  }
)
