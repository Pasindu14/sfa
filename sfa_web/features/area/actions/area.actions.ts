'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateAreaInput, UpdateAreaInput, AreaDto } from '../schema/area.schema'

type AreasListResponse = {
  areas: AreaDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getAreasAction = createAction(
  { name: 'getAreasAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/areas', { params: { page, pageSize } })
    return res.data.data as AreasListResponse
  }
)

export const getAreaByIdAction = createAction(
  { name: 'getAreaByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/areas/${id}`)
    return res.data.data as AreaDto
  }
)

export const createAreaAction = createAction(
  { name: 'createAreaAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateAreaInput) => {
    const res = await client.post('/api/v1/areas', data)
    revalidatePath('/areas')
    return res.data.data as AreaDto
  }
)

export const updateAreaAction = createAction(
  { name: 'updateAreaAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateAreaInput) => {
    const res = await client.put(`/api/v1/areas/${id}`, data)
    revalidatePath('/areas')
    return res.data.data as AreaDto
  }
)

export const getActiveAreasAction = createAction(
  { name: 'getActiveAreasAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/areas/active')
    return res.data.data as AreaDto[]
  }
)

export const activateAreaAction = createAction(
  { name: 'activateAreaAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/areas/${id}/activate`)
    revalidatePath('/areas')
  }
)

export const deactivateAreaAction = createAction(
  { name: 'deactivateAreaAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/areas/${id}/deactivate`)
    revalidatePath('/areas')
  }
)
