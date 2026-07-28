'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateMyPurchaseOrderInput,
  UpdateMyPurchaseOrderInput,
  CancelMyPurchaseOrderInput,
  MyPurchaseOrderDto,
  MyPurchaseOrderListDto,
  MyPurchaseOrderStatsDto,
  MyDistributorProfileDto,
} from '../schema/distributor-purchase-order.schema'
import {
  distributorProductPriceSchema,
  type DistributorProductPrice,
} from '@/features/product-category-pricing/schema/product-category-pricing.schema'

// ── Read ───────────────────────────────────────────────────────────────────

export const getMyPurchaseOrdersAction = createAction(
  { name: 'getMyPurchaseOrdersAction', requireAuth: true, requiredRole: 'Distributor' },
  async (
    page: number = 1,
    pageSize: number = 20,
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
    return res.data.data as MyPurchaseOrderListDto
  }
)

export const getMyPurchaseOrderAction = createAction(
  { name: 'getMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.get(`/api/v1/purchase-orders/${id}`)
    return res.data.data as MyPurchaseOrderDto
  }
)

export const getMyPurchaseOrderStatsAction = createAction(
  { name: 'getMyPurchaseOrderStatsAction', requireAuth: true, requiredRole: 'Distributor' },
  async (fromDate?: string, toDate?: string) => {
    const res = await client.get('/api/v1/purchase-orders/stats', {
      params: {
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    })
    return res.data.data as MyPurchaseOrderStatsDto
  }
)

export const getMyDistributorProfileAction = createAction(
  { name: 'getMyDistributorProfileAction', requireAuth: true, requiredRole: 'Distributor' },
  async () => {
    const res = await client.get('/api/v1/distributors/portal/profile')
    return res.data.data as MyDistributorProfileDto
  }
)

export const getMyProductCategoryPricingsAction = createAction(
  { name: 'getMyProductCategoryPricingsAction', requireAuth: true, requiredRole: 'Distributor' },
  async (): Promise<DistributorProductPrice[]> => {
    const res = await client.get('/api/v1/product-category-pricings/portal')
    // Parse rather than cast — a silent contract drift here renders every line as
    // "No price" instead of failing, so make the mismatch loud.
    return distributorProductPriceSchema.array().parse(res.data.data)
  }
)

// ── Write ──────────────────────────────────────────────────────────────────

export const createMyPurchaseOrderAction = createAction(
  { name: 'createMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (data: CreateMyPurchaseOrderInput) => {
    const res = await client.post('/api/v1/purchase-orders', data)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)

export const updateMyPurchaseOrderAction = createAction(
  { name: 'updateMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number, data: UpdateMyPurchaseOrderInput) => {
    const res = await client.put(`/api/v1/purchase-orders/${id}`, data)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)

// ── Workflow ───────────────────────────────────────────────────────────────

export const submitMyPurchaseOrderAction = createAction(
  { name: 'submitMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/submit`)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)

export const acknowledgeMyPurchaseOrderAction = createAction(
  { name: 'acknowledgeMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/acknowledge`)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)

export const finalizeMyPurchaseOrderAction = createAction(
  { name: 'finalizeMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/finalize`)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)

export const cancelMyPurchaseOrderAction = createAction(
  { name: 'cancelMyPurchaseOrderAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number, data: CancelMyPurchaseOrderInput) => {
    const res = await client.post(`/api/v1/purchase-orders/${id}/cancel`, data)
    revalidatePath('/distributor-purchase-orders')
    return res.data.data as MyPurchaseOrderDto
  }
)
