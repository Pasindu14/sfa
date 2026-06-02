'use client'

import { useQuery } from '@tanstack/react-query'
import { getMyDistributorStockAction } from '../actions/distributor-stock.actions'

export const myStockKeys = {
  all: ['my-stock'] as const,
  list: (params: object) => [...myStockKeys.all, 'list', params] as const,
}

export function useMyStockDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { stockType?: string },
) {
  const stockType = customFilters?.stockType

  return useQuery({
    queryKey: myStockKeys.list({ page, pageSize, search, stockType }),
    queryFn: async () => {
      const result = await getMyDistributorStockAction()
      if (!result.success) throw new Error(result.error)

      let items = result.data

      if (stockType) {
        items = items.filter((item) => item.stockType === stockType)
      }

      if (search.trim()) {
        const q = search.toLowerCase()
        items = items.filter(
          (item) =>
            item.productCode.toLowerCase().includes(q) ||
            item.productDescription.toLowerCase().includes(q),
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
  })
}

;(useMyStockDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useMyStockSummary() {
  return useQuery({
    queryKey: [...myStockKeys.all, 'summary'] as const,
    queryFn: async () => {
      const result = await getMyDistributorStockAction()
      if (!result.success) throw new Error(result.error)
      const items = result.data

      const normalItems = items.filter((i) => i.stockType === 'Normal')
      const freeItems = items.filter((i) => i.stockType === 'FreeIssue')
      const totalQoH = normalItems.reduce((s, i) => s + i.quantityOnHand, 0)

      const lowStockItems = [...normalItems]
        .sort((a, b) => a.quantityOnHand - b.quantityOnHand)
        .slice(0, 5)
        .map((i) => ({
          productCode: i.productCode,
          productDescription: i.productDescription,
          quantityOnHand: i.quantityOnHand,
        }))

      return {
        totalSkus: items.length,
        totalQoH,
        normalCount: normalItems.length,
        freeIssueCount: freeItems.length,
        lowStockItems,
      }
    },
    staleTime: 2 * 60_000,
  })
}
