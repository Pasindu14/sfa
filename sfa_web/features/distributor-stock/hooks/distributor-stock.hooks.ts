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
