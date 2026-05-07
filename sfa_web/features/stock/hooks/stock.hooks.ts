'use client'

import { useQuery, useIsFetching } from '@tanstack/react-query'
import { getDistributorStockAction, getStockTransactionsAction } from '../actions/stock.actions'
import { useStockFilterStore } from '../store'

// ── Query key factory ──────────────────────────────────────────────────────

export const stockKeys = {
  all: ['stock'] as const,
  distributor: (distributorId: number) =>
    [...stockKeys.all, 'distributor', distributorId] as const,
  distributorList: (distributorId: number, params: object) =>
    [...stockKeys.all, 'distributor', distributorId, 'list', params] as const,
  transactions: (distributorId: number, productId: number, page: number) =>
    [...stockKeys.all, 'transactions', distributorId, productId, page] as const,
}

// ── DataTable hook (fetchDataFn with isQueryHook = true) ───────────────────

export function useStockDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: unknown,
) {
  const appliedFilters = useStockFilterStore((s) => s.appliedFilters)

  const query = useQuery({
    queryKey: stockKeys.distributorList(
      appliedFilters?.distributorId ?? 0,
      { page, pageSize, search, stockType: appliedFilters?.stockType, loadCount: appliedFilters?.loadCount }
    ),
    queryFn: async () => {
      const result = await getDistributorStockAction(appliedFilters!.distributorId)
      if (!result.success) throw new Error(result.error)

      // Client-side search + type filter + pagination (stock items per distributor are bounded)
      let items = result.data
      if (appliedFilters?.stockType) {
        items = items.filter((item) => item.stockType === appliedFilters.stockType)
      }
      if (search.trim()) {
        const q = search.toLowerCase()
        items = items.filter(
          (item) =>
            item.productCode.toLowerCase().includes(q) ||
            item.productDescription.toLowerCase().includes(q)
        )
      }

      const totalCount = items.length
      const paginated = items.slice((page - 1) * pageSize, page * pageSize)

      return {
        success: true as const,
        data: paginated,
        pagination: {
          page,
          limit: pageSize,
          total_pages: Math.ceil(totalCount / pageSize),
          total_items: totalCount,
        },
      }
    },
    enabled: !!appliedFilters?.distributorId,
  })

  return query
}

;(useStockDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Real-time loading state via TanStack (no Zustand sync needed) ─────────

export function useStockIsFetching() {
  const appliedFilters = useStockFilterStore((s) => s.appliedFilters)
  return useIsFetching({
    queryKey: appliedFilters
      ? [...stockKeys.all, 'distributor', appliedFilters.distributorId, 'list']
      : undefined,
  }) > 0
}

// ── Single distributor stock (for detail views) ───────────────────────────

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
