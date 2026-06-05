'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreatePurchaseOrderInput,
  UpdatePurchaseOrderInput,
  RejectPurchaseOrderInput,
  PurchaseOrderDto,
  PurchaseOrderListDto,
} from '../schema/purchase-order.schema'

// ── Read ───────────────────────────────────────────────────────────────────

export const getPurchaseOrdersAction = createAction(
  { name: 'getPurchaseOrdersAction', requireAuth: true },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    status?: string,
    fromDate?: string,
    toDate?: string,
  ) => {
    const res = await client.get('/api/v1/purchase-orders', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    })
    return res.data.data as PurchaseOrderListDto
  }
)

export const getPurchaseOrderByIdAction = createAction(
  { name: 'getPurchaseOrderByIdAction', requireAuth: true },
  async (id: number) => {
    const res = await client.get(`/api/v1/purchase-orders/${id}`)
    return res.data.data as PurchaseOrderDto
  }
)

// ── Write ──────────────────────────────────────────────────────────────────

export const createPurchaseOrderAction = createAction(
  { name: 'createPurchaseOrderAction', requireAuth: true },
  async (data: CreatePurchaseOrderInput) => {
    const res = await client.post('/api/v1/purchase-orders', data)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const updatePurchaseOrderAction = createAction(
  { name: 'updatePurchaseOrderAction', requireAuth: true },
  async (id: number, data: UpdatePurchaseOrderInput) => {
    const res = await client.put(`/api/v1/purchase-orders/${id}`, data)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

// ── Workflow actions ───────────────────────────────────────────────────────

export const submitPurchaseOrderAction = createAction(
  { name: 'submitPurchaseOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/submit`)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const repApprovePurchaseOrderAction = createAction(
  { name: 'repApprovePurchaseOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/rep-approve`)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const approvePurchaseOrderAction = createAction(
  { name: 'approvePurchaseOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/approve`)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const rejectPurchaseOrderAction = createAction(
  { name: 'rejectPurchaseOrderAction', requireAuth: true },
  async (id: number, data: RejectPurchaseOrderInput) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/reject`, data)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const acknowledgePurchaseOrderAction = createAction(
  { name: 'acknowledgePurchaseOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/acknowledge`)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const finalizePurchaseOrderAction = createAction(
  { name: 'finalizePurchaseOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/finalize`)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

export const cancelPurchaseOrderAction = createAction(
  { name: 'cancelPurchaseOrderAction', requireAuth: true },
  async (id: number, data: RejectPurchaseOrderInput) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/cancel`, data)
    revalidatePath('/purchase-orders')
    return res.data.data as PurchaseOrderDto
  }
)

// ── Stats ──────────────────────────────────────────────────────────────────

export type PurchaseOrderStatsDto = {
  pendingRepApproval: number
  pendingManagerApproval: number
  pendingAcknowledgement: number
  finalized: number
  total: number
}

export const getPurchaseOrderStatsAction = createAction(
  { name: 'getPurchaseOrderStatsAction', requireAuth: true },
  async (fromDate?: string, toDate?: string) => {
    const res = await client.get('/api/v1/purchase-orders/stats', {
      params: {
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    })
    return res.data.data as PurchaseOrderStatsDto
  }
)
