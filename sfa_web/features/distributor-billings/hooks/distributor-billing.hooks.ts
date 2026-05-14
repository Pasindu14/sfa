'use client'

import { useQuery } from '@tanstack/react-query'
import { getMyBillingsAction, getMyBillingDetailAction } from '../actions/distributor-billing.actions'
import type { DistributorBillingListItem } from '../schema/distributor-billing.schema'

export const myBillingKeys = {
  all: ['my-billings'] as const,
  list: (params: object) => [...myBillingKeys.all, 'list', params] as const,
  detail: (id: number) => [...myBillingKeys.all, 'detail', id] as const,
}

export function useMyBillingsDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string; dateFrom?: string; dateTo?: string },
) {
  const status = customFilters?.status
  const dateFrom = customFilters?.dateFrom
  const dateTo = customFilters?.dateTo

  return useQuery({
    queryKey: myBillingKeys.list({ page, pageSize, search, status, dateFrom, dateTo }),
    queryFn: async () => {
      const result = await getMyBillingsAction(
        page, pageSize, search || undefined, status || undefined,
        dateFrom || undefined, dateTo || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { billings, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: billings as DistributorBillingListItem[],
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
  })
}

;(useMyBillingsDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useMyBillingDetail(id: number | null) {
  return useQuery({
    queryKey: myBillingKeys.detail(id!),
    queryFn: async () => {
      const result = await getMyBillingDetailAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}
