'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateDivisionInput, UpdateDivisionInput, DivisionDto } from '../schema/division.schema'

type DivisionsListResponse = {
  divisions: DivisionDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getDivisionsAction = createAction(
  { name: 'getDivisionsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/divisions', { params: { page, pageSize } })
    return res.data.data as DivisionsListResponse
  }
)

export const getDivisionByIdAction = createAction(
  { name: 'getDivisionByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/divisions/${id}`)
    return res.data.data as DivisionDto
  }
)

export const createDivisionAction = createAction(
  { name: 'createDivisionAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateDivisionInput) => {
    const res = await client.post('/api/v1/divisions', data)
    revalidatePath('/divisions')
    return res.data.data as DivisionDto
  }
)

export const updateDivisionAction = createAction(
  { name: 'updateDivisionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateDivisionInput) => {
    const res = await client.put(`/api/v1/divisions/${id}`, data)
    revalidatePath('/divisions')
    return res.data.data as DivisionDto
  }
)

export const getActiveDivisionsAction = createAction(
  { name: 'getActiveDivisionsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/divisions/active')
    return res.data.data as DivisionDto[]
  }
)

export const activateDivisionAction = createAction(
  { name: 'activateDivisionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/divisions/${id}/activate`)
    revalidatePath('/divisions')
  }
)

export const deactivateDivisionAction = createAction(
  { name: 'deactivateDivisionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/divisions/${id}/deactivate`)
    revalidatePath('/divisions')
  }
)
