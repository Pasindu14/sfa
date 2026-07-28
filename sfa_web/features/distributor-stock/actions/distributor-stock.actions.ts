'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { DistributorStockItem } from '@/features/stock/schema/stock.schema'

/**
 * `includeZeroStock` also returns every active product the distributor has never held, as a
 * zero-quantity placeholder (`id: 0`, `lastUpdatedAt: null`). The stock balance table asks for
 * them so the full catalogue is listed; the dashboard summary does not, so its SKU counts stay
 * "what I actually hold".
 */
export const getMyDistributorStockAction = createAction(
  { name: 'getMyDistributorStockAction', requireAuth: true, requiredRole: 'Distributor' },
  async (includeZeroStock = false) => {
    const res = await client.get('/api/v1/stock/portal', {
      params: { includeZeroStock },
    })
    return res.data.data as DistributorStockItem[]
  }
)
