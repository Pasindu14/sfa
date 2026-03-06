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
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/distributors', { params: { page, pageSize } })
    return res.data.data as DistributorsListResponse
  }
)

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
