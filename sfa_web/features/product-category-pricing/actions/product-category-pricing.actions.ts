'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { ProductCategoryPricingRow } from '../schema/product-category-pricing.schema'

export const getAllProductCategoryPricingsAction = createAction(
  { name: 'getAllProductCategoryPricingsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/product-category-pricings')
    return res.data.data as ProductCategoryPricingRow[]
  }
)

export const bulkUpsertProductCategoryPricingsAction = createAction(
  { name: 'bulkUpsertProductCategoryPricingsAction', requireAuth: true, requiredRole: 'Admin' },
  async (items: { productId: number; priceA: number; priceB: number; priceC: number; priceD: number }[]) => {
    const res = await client.put('/api/v1/product-category-pricings', { items })
    return res.data.data as string
  }
)
