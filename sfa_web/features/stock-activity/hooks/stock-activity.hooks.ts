'use client'

import { useCallback, useState } from 'react'
import {
  keepPreviousData,
  queryOptions,
  useIsFetching,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { ActionError } from '@/lib/actions/action-error'
import { getStockActivityAction, getStockActivityUsersAction } from '../actions/stock-activity.actions'
import { exportStockActivityExcel } from '../lib/stock-activity-export'
import type { StockActivity, StockActivityFilters, StockActivityUser } from '../schema/stock-activity.schema'
import { useStockActivityFilterStore } from '../store'

// ── Query key factory ──────────────────────────────────────────────────────

export const stockActivityKeys = {
  all: ['stock-activity'] as const,
  lists: () => [...stockActivityKeys.all, 'list'] as const,
  list: (params: object) => [...stockActivityKeys.lists(), params] as const,
  users: () => [...stockActivityKeys.all, 'users'] as const,
}

// ── DataTable hook (fetchDataFn with isQueryHook = true) ───────────────────

export function useStockActivityDataTable(
  page: number,
  pageSize: number,
  _search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: unknown,
) {
  const appliedFilters = useStockActivityFilterStore((s) => s.appliedFilters)

  return useQuery({
    queryKey: stockActivityKeys.list({ page, pageSize, ...appliedFilters }),
    queryFn: async () => {
      const result = await getStockActivityAction(appliedFilters!, page, pageSize)
      if (!result.success) throw new ActionError(result)
      const { items, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: items,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    enabled: !!appliedFilters,
    placeholderData: keepPreviousData,
  })
}

;(useStockActivityDataTable as unknown as Record<string, unknown>).isQueryHook = true

/** True while the activity list is loading — drives the Load button spinner. */
export function useStockActivityIsFetching() {
  return useIsFetching({ queryKey: stockActivityKeys.lists() }) > 0
}

// ── User filter options ────────────────────────────────────────────────────

const activityUsersQuery = queryOptions({
  queryKey: stockActivityKeys.users(),
  queryFn: async () => {
    const result = await getStockActivityUsersAction()
    if (!result.success) throw new ActionError(result)
    return result.data
  },
  staleTime: 5 * 60 * 1000,
})

/** AsyncSelect-compatible fetcher over the cached user list — filters by name locally. */
export function useStockActivityUsersFetcher() {
  const queryClient = useQueryClient()

  return useCallback(
    async (search?: string): Promise<StockActivityUser[]> => {
      const users = await queryClient.fetchQuery(activityUsersQuery)
      const q = search?.trim().toLowerCase()
      if (!q) return users
      return users.filter((u) => u.name.toLowerCase().includes(q))
    },
    [queryClient],
  )
}

// ── Excel export ───────────────────────────────────────────────────────────

const EXPORT_PAGE_SIZE = 1000
const EXPORT_MAX_ROWS = 20_000

/** Fetches every page for the applied filters (capped) and downloads them as an Excel file. */
export function useExportStockActivity() {
  const [isExporting, setIsExporting] = useState(false)

  const exportExcel = useCallback(async (filters: StockActivityFilters) => {
    setIsExporting(true)
    try {
      const rows: StockActivity[] = []
      let totalCount = 0
      for (let page = 1; rows.length < EXPORT_MAX_ROWS; page++) {
        const result = await getStockActivityAction(filters, page, EXPORT_PAGE_SIZE)
        if (!result.success) throw new ActionError(result)
        totalCount = result.data.totalCount
        rows.push(...result.data.items)
        if (result.data.items.length < EXPORT_PAGE_SIZE || rows.length >= totalCount) break
      }

      if (rows.length === 0) {
        toast.info('No stock activity to export for these filters')
        return
      }

      const capped = rows.length > EXPORT_MAX_ROWS || totalCount > EXPORT_MAX_ROWS
      await exportStockActivityExcel(filters, rows.slice(0, EXPORT_MAX_ROWS))
      if (capped) {
        toast.warning(
          `Export capped at ${EXPORT_MAX_ROWS.toLocaleString()} of ${totalCount.toLocaleString()} rows — narrow the filters to export the rest.`,
        )
      } else {
        toast.success(`Exported ${rows.length.toLocaleString()} rows`)
      }
    } catch (error) {
      console.error('[useExportStockActivity]', error)
      toast.error('Failed to export stock activity')
    } finally {
      setIsExporting(false)
    }
  }, [])

  return { exportExcel, isExporting }
}
