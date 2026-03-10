'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateRegionInput,
  UpdateRegionInput,
  RegionDto,
} from '../schema/region.schema'

type RegionsListResponse = {
  regions: RegionDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getRegionsAction = createAction(
  { name: 'getRegionsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/regions', { params: { page, pageSize, search: search || undefined } })
    return res.data.data as RegionsListResponse
  }
)

export const getRegionByIdAction = createAction(
  { name: 'getRegionByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/regions/${id}`)
    return res.data.data as RegionDto
  }
)

export const createRegionAction = createAction(
  { name: 'createRegionAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateRegionInput) => {
    const res = await client.post('/api/v1/regions', data)
    revalidatePath('/regions')
    return res.data.data as RegionDto
  }
)

export const updateRegionAction = createAction(
  { name: 'updateRegionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateRegionInput) => {
    const res = await client.put(`/api/v1/regions/${id}`, data)
    revalidatePath('/regions')
    return res.data.data as RegionDto
  }
)

export const deleteRegionAction = createAction(
  { name: 'deleteRegionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/regions/${id}`)
    revalidatePath('/regions')
  }
)

export const getActiveRegionsAction = createAction(
  { name: 'getActiveRegionsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/regions/active')
    return res.data.data as RegionDto[]
  }
)

export const activateRegionAction = createAction(
  { name: 'activateRegionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/regions/${id}/activate`)
    revalidatePath('/regions')
  }
)

export const deactivateRegionAction = createAction(
  { name: 'deactivateRegionAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/regions/${id}/deactivate`)
    revalidatePath('/regions')
  }
)
