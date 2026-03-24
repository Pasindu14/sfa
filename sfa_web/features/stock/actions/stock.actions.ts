'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  DistributorStockItem,
  StockTransactionListResponse,
} from '../schema/stock.schema'

export const getDistributorStockAction = createAction(
  { name: 'getDistributorStockAction', requireAuth: true, requiredRole: 'Admin' },
  async (distributorId: number) => {
    const res = await client.get(`/api/v1/stock/distributors/${distributorId}`)
    return res.data.data as DistributorStockItem[]
  }
)

export const getStockTransactionsAction = createAction(
  { name: 'getStockTransactionsAction', requireAuth: true, requiredRole: 'Admin' },
  async (distributorId: number, productId: number, page: number = 1, pageSize: number = 50) => {
    const res = await client.get(
      `/api/v1/stock/distributors/${distributorId}/products/${productId}/transactions`,
      { params: { page, pageSize } }
    )
    const body = res.data
    return {
      transactions: body.data as StockTransactionListResponse['transactions'],
      totalCount: body.pagination?.totalCount ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    } satisfies StockTransactionListResponse
  }
)
