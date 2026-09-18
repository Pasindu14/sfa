'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreatePricingStructureInput,
  UpdatePricingStructureInput,
  DuplicatePricingStructureInput,
  PricingStructureItemUpdate,
  PricingStructureDto,
  PricingStructureItemRow,
  DefaultStructurePrices,
} from '../schema/pricing-structure.schema'

type PricingStructuresListResponse = {
  pricingStructures: PricingStructureDto[]
  totalCount: number
  page: number
  pageSize: number
}

// Everything here is Admin-only on the API too, except the default-prices read, which the
// staff PO editors need and the API opens to the whole management chain.

export const getPricingStructuresAction = createAction(
  { name: 'getPricingStructuresAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/pricing-structures', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as PricingStructuresListResponse
  }
)

export const getPricingStructureByIdAction = createAction(
  { name: 'getPricingStructureByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/pricing-structures/${id}`)
    return res.data.data as PricingStructureDto
  }
)

export const createPricingStructureAction = createAction(
  { name: 'createPricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreatePricingStructureInput) => {
    const res = await client.post('/api/v1/pricing-structures', data)
    revalidatePath('/pricing-structures')
    return res.data.data as PricingStructureDto
  }
)

export const updatePricingStructureAction = createAction(
  { name: 'updatePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdatePricingStructureInput) => {
    const res = await client.put(`/api/v1/pricing-structures/${id}`, data)
    revalidatePath('/pricing-structures')
    return res.data.data as PricingStructureDto
  }
)

export const duplicatePricingStructureAction = createAction(
  { name: 'duplicatePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: DuplicatePricingStructureInput) => {
    const res = await client.post(`/api/v1/pricing-structures/${id}/duplicate`, data)
    revalidatePath('/pricing-structures')
    return res.data.data as PricingStructureDto
  }
)

export const setDefaultPricingStructureAction = createAction(
  { name: 'setDefaultPricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, rowVersion: number) => {
    const res = await client.post(`/api/v1/pricing-structures/${id}/set-default`, { rowVersion })
    revalidatePath('/pricing-structures')
    return res.data.data as PricingStructureDto
  }
)

export const activatePricingStructureAction = createAction(
  { name: 'activatePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/pricing-structures/${id}/activate`)
    revalidatePath('/pricing-structures')
  }
)

export const deactivatePricingStructureAction = createAction(
  { name: 'deactivatePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/pricing-structures/${id}/deactivate`)
    revalidatePath('/pricing-structures')
  }
)

export const deletePricingStructureAction = createAction(
  { name: 'deletePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/pricing-structures/${id}`)
    revalidatePath('/pricing-structures')
  }
)

export const getPricingStructureItemsAction = createAction(
  { name: 'getPricingStructureItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/pricing-structures/${id}/items`)
    return (res.data.data ?? []) as PricingStructureItemRow[]
  }
)

export const updatePricingStructureItemsAction = createAction(
  { name: 'updatePricingStructureItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, items: PricingStructureItemUpdate[]) => {
    const res = await client.put(`/api/v1/pricing-structures/${id}/items`, { items })
    revalidatePath(`/pricing-structures/${id}`)
    return res.data.data as PricingStructureDto
  }
)

export const getDefaultStructurePricesAction = createAction(
  {
    name: 'getDefaultStructurePricesAction',
    requireAuth: true,
    requiredRole: ['Admin', 'NSM', 'RSM', 'ASM', 'Supervisor'],
  },
  async () => {
    const res = await client.get('/api/v1/pricing-structures/default/prices')
    return res.data.data as DefaultStructurePrices
  }
)
