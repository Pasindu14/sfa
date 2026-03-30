'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreatePricingStructureInput,
  UpdatePricingStructureInput,
  BulkUpdateItemsInput,
  PricingStructureDto,
  PricingStructureDetailDto,
  PricingStructureItemDto,
  PricingStructureListDto,
} from '../schema/pricing-structure.schema'

export const getPricingStructuresAction = createAction(
  { name: 'getPricingStructuresAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/pricing-structures', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as PricingStructureListDto
  }
)

export const getPricingStructureByIdAction = createAction(
  { name: 'getPricingStructureByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/pricing-structures/${id}`)
    return res.data.data as PricingStructureDetailDto
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

export const deletePricingStructureAction = createAction(
  { name: 'deletePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/pricing-structures/${id}`)
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

export const activatePricingStructureAction = createAction(
  { name: 'activatePricingStructureAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/pricing-structures/${id}/activate`)
    revalidatePath('/pricing-structures')
  }
)

export const getPricingStructureItemsAction = createAction(
  { name: 'getPricingStructureItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/pricing-structures/${id}/items`)
    return res.data.data as PricingStructureItemDto[]
  }
)

export const bulkUpdatePricingStructureItemsAction = createAction(
  { name: 'bulkUpdatePricingStructureItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: BulkUpdateItemsInput) => {
    const res = await client.put(`/api/v1/pricing-structures/${id}/items`, data)
    revalidatePath('/pricing-structures')
    return res.data.data as PricingStructureItemDto[]
  }
)
