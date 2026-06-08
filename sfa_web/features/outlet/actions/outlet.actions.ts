'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateOutletInput, UpdateOutletInput, OutletDto, OutletMapPointDto } from '../schema/outlet.schema'

type OutletsListResponse = {
  outlets: OutletDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getOutletsAction = createAction(
  { name: 'getOutletsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string, status?: string) => {
    const res = await client.get('/api/v1/outlets', {
      params: { page, pageSize, search: search || undefined, status: status || undefined },
    })
    return res.data.data as OutletsListResponse
  }
)

export const getOutletByIdAction = createAction(
  { name: 'getOutletByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/outlets/${id}`)
    return res.data.data as OutletDto
  }
)

export const getActiveOutletsAction = createAction(
  { name: 'getActiveOutletsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/outlets/active')
    return res.data.data as OutletDto[]
  }
)

export const getOutletMapPointsAction = createAction(
  { name: 'getOutletMapPointsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/outlets/map-points')
    return res.data.data as OutletMapPointDto[]
  }
)

function sanitiseOutletPayload(data: CreateOutletInput) {
  return {
    ...data,
    email: data.email || null,
    contactPerson: data.contactPerson || null,
    vatNo: data.vatNo || null,
    ownerDOB: data.ownerDOB || null,
    remarks: data.remarks || null,
    image: data.image || null,
  }
}

export const createOutletAction = createAction(
  { name: 'createOutletAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateOutletInput) => {
    const res = await client.post('/api/v1/outlets', sanitiseOutletPayload(data))
    revalidatePath('/outlets')
    return res.data.data as OutletDto
  }
)

export const updateOutletAction = createAction(
  { name: 'updateOutletAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateOutletInput) => {
    const res = await client.put(`/api/v1/outlets/${id}`, sanitiseOutletPayload(data))
    revalidatePath('/outlets')
    return res.data.data as OutletDto
  }
)

export const deleteOutletAction = createAction(
  { name: 'deleteOutletAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/outlets/${id}`)
    revalidatePath('/outlets')
  }
)

export const activateOutletAction = createAction(
  { name: 'activateOutletAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/outlets/${id}/activate`)
    revalidatePath('/outlets')
  }
)

export const deactivateOutletAction = createAction(
  { name: 'deactivateOutletAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/outlets/${id}/deactivate`)
    revalidatePath('/outlets')
  }
)

export const getMyOutletsAction = createAction(
  { name: 'getMyOutletsAction', requireAuth: true },
  async (page: number = 1, pageSize: number = 10, search?: string, status?: string) => {
    const res = await client.get('/api/v1/distributors/portal/outlets', {
      params: { page, pageSize, search: search || undefined, status: status || undefined },
    })
    return res.data.data as OutletsListResponse
  }
)
