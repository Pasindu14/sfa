'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  DistributorStockItem,
  StockTransactionListResponse,
} from '../schema/stock.schema'

/**
 * Every stock balance of a distributor (unpaged). `includeZeroStock` also returns each active
 * product the distributor never held as a zero-quantity placeholder. The API gives placeholders
 * id 0, so they get a negative per-product id here to stay unique as table row keys.
 */
export const getDistributorStockAction = createAction(
  { name: 'getDistributorStockAction', requireAuth: true, requiredRole: 'Admin' },
  async (distributorId: number, includeZeroStock: boolean = false) => {
    const res = await client.get(`/api/v1/stock/distributors/${distributorId}/balances`, {
      params: { includeZeroStock },
    })
    const items = res.data.data as DistributorStockItem[]
    return items.map((item) => (item.id === 0 ? { ...item, id: -item.productId } : item))
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
