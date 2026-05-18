'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { MyGrnListItem, ConfirmMyGrnInput } from '../schema/distributor-grn.schema'

export const getMyGrnsAction = createAction(
  { name: 'getMyGrnsAction', requireAuth: true, requiredRole: 'Distributor' },
  async (
    page: number = 1,
    pageSize: number = 20,
    status?: string,
    dateFrom?: string,
    dateTo?: string,
    search?: string,
  ) => {
    const res = await client.get('/api/v1/grns/portal', {
      params: {
        page,
        pageSize,
        status: status || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        search: search || undefined,
      },
    })
    const body = res.data
    return {
      grns: body.data as MyGrnListItem[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    }
  }
)

export const getMyGrnDetailAction = createAction(
  { name: 'getMyGrnDetailAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.get(`/api/v1/grns/portal/${id}`)
    return res.data.data as MyGrnListItem
  }
)

export const confirmMyGrnAction = createAction(
  { name: 'confirmMyGrnAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number, data: ConfirmMyGrnInput) => {
    const res = await client.patch(`/api/v1/grns/${id}/confirm`, data)
    revalidatePath('/distributor-grns')
    return res.data.data as MyGrnListItem
  }
)
