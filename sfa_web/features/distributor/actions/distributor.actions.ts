'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateDistributorInput,
  UpdateDistributorInput,
  DistributorDto,
} from '../schema/distributor.schema'

type DistributorsListResponse = {
  distributors: DistributorDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getDistributorsAction = createAction(
  { name: 'getDistributorsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string, status?: string) => {
    const res = await client.get('/api/v1/distributors', { params: { page, pageSize, search: search || undefined, status: status || undefined } })
    return res.data.data as DistributorsListResponse
  }
)

/**
 * Shared search-as-you-type fetcher for distributor dropdowns.
 *
 * Dropdowns must only offer distributors that are still selectable, so this
 * pins `status: 'Active'` — the API maps that to `IsActive = true` and its
 * repository already excludes soft-deleted rows. Never call
 * `getDistributorsAction` directly from a picker: that endpoint backs the
 * admin list and deliberately returns inactive distributors too.
 */
export const fetchActiveDistributorsForSelect = async (
  search?: string,
): Promise<DistributorDto[]> => {
  if (!search || search.trim().length === 0) return []
  const result = await getDistributorsAction(1, 50, search.trim(), 'Active')
  if (!result.success) return []
  return result.data.distributors
}

export const getDistributorByIdAction = createAction(
  { name: 'getDistributorByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/distributors/${id}`)
    return res.data.data as DistributorDto
  }
)

export const createDistributorAction = createAction(
  { name: 'createDistributorAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateDistributorInput) => {
    const res = await client.post('/api/v1/distributors', data)
    revalidatePath('/distributors')
    return res.data.data as DistributorDto
  }
)

export const updateDistributorAction = createAction(
  { name: 'updateDistributorAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateDistributorInput) => {
    const res = await client.put(`/api/v1/distributors/${id}`, data)
    revalidatePath('/distributors')
    return res.data.data as DistributorDto
  }
)

export const deleteDistributorAction = createAction(
  { name: 'deleteDistributorAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/distributors/${id}`)
    revalidatePath('/distributors')
  }
)

export const activateDistributorAction = createAction(
  { name: 'activateDistributorAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/distributors/${id}/activate`)
    revalidatePath('/distributors')
  }
)

export const deactivateDistributorAction = createAction(
  { name: 'deactivateDistributorAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/distributors/${id}/deactivate`)
    revalidatePath('/distributors')
  }
)
