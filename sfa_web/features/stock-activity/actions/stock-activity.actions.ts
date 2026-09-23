'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  StockActivityFilters,
  StockActivityListResponse,
  StockActivityUser,
} from '../schema/stock-activity.schema'

export const getStockActivityAction = createAction(
  { name: 'getStockActivityAction', requireAuth: true, requiredRole: 'Admin' },
  async (filters: StockActivityFilters, page: number = 1, pageSize: number = 50) => {
    const res = await client.get('/api/v1/stock/activity', {
      params: {
        from: filters.from,
        to: filters.to,
        distributorId: filters.distributorId ?? undefined,
        productId: filters.productId ?? undefined,
        userId: filters.userId ?? undefined,
        transactionType: filters.transactionType ?? undefined,
        direction: filters.direction ?? undefined,
        page,
        pageSize,
      },
    })
    const body = res.data
    return {
      items: body.data as StockActivityListResponse['items'],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    } satisfies StockActivityListResponse
  }
)

export const getStockActivityUsersAction = createAction(
  { name: 'getStockActivityUsersAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/stock/activity/users')
    return res.data.data as StockActivityUser[]
  }
)
