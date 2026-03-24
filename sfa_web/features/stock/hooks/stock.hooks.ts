'use client'

import { useQuery } from '@tanstack/react-query'
import { getDistributorStockAction, getStockTransactionsAction } from '../actions/stock.actions'

// ── Query key factory ──────────────────────────────────────────────────────

export const stockKeys = {
  all: ['stock'] as const,
  distributor: (distributorId: number) =>
    [...stockKeys.all, 'distributor', distributorId] as const,
  transactions: (distributorId: number, productId: number, page: number) =>
    [...stockKeys.all, 'transactions', distributorId, productId, page] as const,
}

// ── Distributor stock query hook ───────────────────────────────────────────

export function useDistributorStock(distributorId: number | null) {
  return useQuery({
    queryKey: stockKeys.distributor(distributorId!),
    queryFn: async () => {
      const result = await getDistributorStockAction(distributorId!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: distributorId !== null,
  })
}

// ── Product transactions query hook ───────────────────────────────────────

export function useStockTransactions(
  distributorId: number | null,
  productId: number | null,
  page: number = 1,
  pageSize: number = 50,
) {
  return useQuery({
    queryKey: stockKeys.transactions(distributorId!, productId!, page),
    queryFn: async () => {
      const result = await getStockTransactionsAction(distributorId!, productId!, page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: distributorId !== null && productId !== null,
  })
}
