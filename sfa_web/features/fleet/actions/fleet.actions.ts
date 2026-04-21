'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateFleetInput,
  UpdateFleetInput,
  FleetDto,
} from '../schema/fleet.schema'

type FleetsListResponse = {
  fleets: FleetDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getFleetsAction = createAction(
  { name: 'getFleetsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/fleets', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as FleetsListResponse
  }
)

export const getFleetByIdAction = createAction(
  { name: 'getFleetByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/fleets/${id}`)
    return res.data.data as FleetDto
  }
)

export const createFleetAction = createAction(
  { name: 'createFleetAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateFleetInput) => {
    const res = await client.post('/api/v1/fleets', data)
    revalidatePath('/fleets')
    return res.data.data as FleetDto
  }
)

export const updateFleetAction = createAction(
  { name: 'updateFleetAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateFleetInput) => {
    const res = await client.put(`/api/v1/fleets/${id}`, data)
    revalidatePath('/fleets')
    return res.data.data as FleetDto
  }
)

export const activateFleetAction = createAction(
  { name: 'activateFleetAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/fleets/${id}/activate`)
    revalidatePath('/fleets')
  }
)

export const deactivateFleetAction = createAction(
  { name: 'deactivateFleetAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/fleets/${id}/deactivate`)
    revalidatePath('/fleets')
  }
)

// Lightweight fetcher for AsyncSelect dropdowns — calls the /all endpoint
export const fetchFleetsForSelect = async (search?: string): Promise<FleetDto[]> => {
  const res = await client.get('/api/v1/fleets/all')
  if (!res.data?.data) return []
  const fleets = res.data.data as FleetDto[]
  if (!search) return fleets
  const lower = search.toLowerCase()
  return fleets.filter((f) => f.name.toLowerCase().includes(lower))
}
