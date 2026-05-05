'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  ImportSalesTargetsPayload,
  ImportSalesTargetsResult,
  SalesTargetDto,
  SalesTargetImportBatchDto,
  UpdateTargetQuantityInput,
} from '../schema/sales-target.schema'

export const getSalesTargetsAction = createAction(
  { name: 'getSalesTargetsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 20,
    search?: string,
    year?: number,
    month?: number,
    salesRepId?: number,
  ) => {
    const res = await client.get('/api/v1/sales-targets', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        year: year || undefined,
        month: month || undefined,
        salesRepId: salesRepId || undefined,
      },
    })
    const body = res.data
    return {
      targets: body.data as SalesTargetDto[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    }
  }
)

export const updateSalesTargetAction = createAction(
  { name: 'updateSalesTargetAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateTargetQuantityInput) => {
    const res = await client.put(`/api/v1/sales-targets/${id}`, data)
    revalidatePath('/sales-targets')
    return res.data.data as SalesTargetDto
  }
)

export const importSalesTargetsAction = createAction(
  { name: 'importSalesTargetsAction', requireAuth: true, requiredRole: 'Admin' },
  async (payload: ImportSalesTargetsPayload) => {
    const res = await client.post('/api/v1/sales-targets/import', payload)
    revalidatePath('/sales-targets')
    return res.data.data as ImportSalesTargetsResult
  }
)

export const getImportBatchesAction = createAction(
  { name: 'getImportBatchesAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 20) => {
    const res = await client.get('/api/v1/sales-targets/import-batches', {
      params: { page, pageSize },
    })
    const body = res.data
    return {
      batches: body.data as SalesTargetImportBatchDto[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    }
  }
)
