'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { DistributorStockItem } from '@/features/stock/schema/stock.schema'

export const getMyDistributorStockAction = createAction(
  { name: 'getMyDistributorStockAction', requireAuth: true, requiredRole: 'Distributor' },
  async () => {
    const res = await client.get('/api/v1/stock/portal')
    return res.data.data as DistributorStockItem[]
  }
)
