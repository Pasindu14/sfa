'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  GrnListResponse,
  GrnListItem,
  CreateGrnInput,
  ConfirmGrnInput,
} from '../schema/grn.schema'

export const getGrnsAction = createAction(
  { name: 'getGrnsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 20,
    status?: string,
    distributorId?: number,
    date?: string,
  ) => {
    const res = await client.get('/api/v1/grns', {
      params: {
        page,
        pageSize,
        status: status || undefined,
        distributorId: distributorId || undefined,
        date: date || undefined,
      },
    })
    const body = res.data
    return {
      grns: body.data as GrnListResponse['grns'],
      totalCount: body.pagination?.totalCount ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    } satisfies GrnListResponse
  }
)

export const getGrnByIdAction = createAction(
  { name: 'getGrnByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/grns/${id}`)
    return res.data.data as GrnListItem
  }
)

export const createGrnAction = createAction(
  { name: 'createGrnAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateGrnInput) => {
    const res = await client.post('/api/v1/grns', data)
    revalidatePath('/grns')
    return res.data.data as GrnListItem
  }
)

export const confirmGrnAction = createAction(
  { name: 'confirmGrnAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: ConfirmGrnInput) => {
    const res = await client.patch(`/api/v1/grns/${id}/confirm`, data)
    revalidatePath('/grns')
    return res.data.data as GrnListItem
  }
)

export const deleteGrnAction = createAction(
  { name: 'deleteGrnAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/grns/${id}`)
    revalidatePath('/grns')
  }
)
