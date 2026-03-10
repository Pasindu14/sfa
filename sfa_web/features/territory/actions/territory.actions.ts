'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateTerritoryInput, UpdateTerritoryInput, TerritoryDto } from '../schema/territory.schema'

type TerritoriesListResponse = {
  territories: TerritoryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getTerritoriesAction = createAction(
  { name: 'getTerritoriesAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/territories', { params: { page, pageSize } })
    return res.data.data as TerritoriesListResponse
  }
)

export const getTerritoryByIdAction = createAction(
  { name: 'getTerritoryByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/territories/${id}`)
    return res.data.data as TerritoryDto
  }
)

export const createTerritoryAction = createAction(
  { name: 'createTerritoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateTerritoryInput) => {
    const res = await client.post('/api/v1/territories', data)
    revalidatePath('/territories')
    return res.data.data as TerritoryDto
  }
)

export const updateTerritoryAction = createAction(
  { name: 'updateTerritoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateTerritoryInput) => {
    const res = await client.put(`/api/v1/territories/${id}`, data)
    revalidatePath('/territories')
    return res.data.data as TerritoryDto
  }
)

export const activateTerritoryAction = createAction(
  { name: 'activateTerritoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/territories/${id}/activate`)
    revalidatePath('/territories')
  }
)

export const deactivateTerritoryAction = createAction(
  { name: 'deactivateTerritoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/territories/${id}/deactivate`)
    revalidatePath('/territories')
  }
)
