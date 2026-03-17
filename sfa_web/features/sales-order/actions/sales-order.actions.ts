'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  SalesOrderDto,
  SalesOrderListDto,
  DefaultPricingStructureDto,
} from '../types/sales-order.types'
import type {
  CreateSalesOrderInput,
  UpdateSalesOrderInput,
  RejectSalesOrderInput,
} from '../schema/sales-order.schema'

export const getSalesOrdersAction = createAction(
  { name: 'getSalesOrdersAction', requireAuth: true },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    status?: string,
    fromDate?: string,
    toDate?: string,
  ) => {
    const res = await client.get('/api/v1/sales-orders', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    })
    return res.data.data as SalesOrderListDto
  }
)

export const getSalesOrderByIdAction = createAction(
  { name: 'getSalesOrderByIdAction', requireAuth: true },
  async (id: number) => {
    const res = await client.get(`/api/v1/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const createSalesOrderAction = createAction(
  { name: 'createSalesOrderAction', requireAuth: true },
  async (data: CreateSalesOrderInput) => {
    const res = await client.post('/api/v1/sales-orders', data)
    revalidatePath('/sales-orders')
    return res.data.data as SalesOrderDto
  }
)

export const updateSalesOrderAction = createAction(
  { name: 'updateSalesOrderAction', requireAuth: true },
  async (id: number, data: UpdateSalesOrderInput) => {
    const res = await client.put(`/api/v1/sales-orders/${id}`, data)
    revalidatePath('/sales-orders')
    return res.data.data as SalesOrderDto
  }
)

export const submitSalesOrderAction = createAction(
  { name: 'submitSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/submit`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const repApproveSalesOrderAction = createAction(
  { name: 'repApproveSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/rep-approve`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const approveSalesOrderAction = createAction(
  { name: 'approveSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/approve`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const rejectSalesOrderAction = createAction(
  { name: 'rejectSalesOrderAction', requireAuth: true },
  async (id: number, data: RejectSalesOrderInput) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/reject`, data)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const acknowledgeSalesOrderAction = createAction(
  { name: 'acknowledgeSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/acknowledge`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const finalizeSalesOrderAction = createAction(
  { name: 'finalizeSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/finalize`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const cancelSalesOrderAction = createAction(
  { name: 'cancelSalesOrderAction', requireAuth: true },
  async (id: number, data: RejectSalesOrderInput) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/cancel`, data)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const getDefaultPricingStructureAction = createAction(
  { name: 'getDefaultPricingStructureAction', requireAuth: true },
  async () => {
    const res = await client.get('/api/v1/pricing-structures/default')
    return res.data.data as DefaultPricingStructureDto
  }
)
