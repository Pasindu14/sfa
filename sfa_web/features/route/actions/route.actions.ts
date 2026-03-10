'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateRouteInput, UpdateRouteInput, RouteDto } from '../schema/route.schema'

type RoutesListResponse = {
  routes: RouteDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getRoutesAction = createAction(
  { name: 'getRoutesAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/routes', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as RoutesListResponse
  }
)

export const getRouteByIdAction = createAction(
  { name: 'getRouteByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/routes/${id}`)
    return res.data.data as RouteDto
  }
)

export const createRouteAction = createAction(
  { name: 'createRouteAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateRouteInput) => {
    const res = await client.post('/api/v1/routes', data)
    revalidatePath('/routes')
    return res.data.data as RouteDto
  }
)

export const updateRouteAction = createAction(
  { name: 'updateRouteAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateRouteInput) => {
    const res = await client.put(`/api/v1/routes/${id}`, data)
    revalidatePath('/routes')
    return res.data.data as RouteDto
  }
)

export const deleteRouteAction = createAction(
  { name: 'deleteRouteAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/routes/${id}`)
    revalidatePath('/routes')
  }
)
