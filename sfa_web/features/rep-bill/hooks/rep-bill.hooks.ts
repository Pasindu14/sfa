'use client'

import { useCallback, useEffect } from 'react'
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getRepBillDetailAction,
  getRepBillsAction,
  getSupervisorsForSelectAction,
} from '../actions/rep-bill.actions'
import { useRepBillFilterStore } from '../store/rep-bill.filter-store'
import { getSupervisorRepsAction } from '@/features/daily-route-assignment/actions/daily-route-assignment.actions'
import { userSelectKeys } from '@/lib/api/query-keys'
import type { RepBillListItem, RepOption, SupervisorOption } from '../schema/rep-bill.schema'

export const repBillKeys = {
  all: ['rep-bills'] as const,
  list: (params: object) => [...repBillKeys.all, 'list', params] as const,
  detail: (id: number) => [...repBillKeys.all, 'detail', id] as const,
  // Shared namespace so the Users feature's mutations can drop a deactivated supervisor out
  // of this picker immediately instead of leaving it there for the stale time.
  supervisors: userSelectKeys.repBillSupervisor,
}

/**
 * The DataTable's fetch hook. Must take exactly 8 positional args — the table calls it
 * positionally (`data-table.tsx:859`) once `isQueryHook` is set.
 *
 * Args 3-7 are unused here: there is no `search` param on `GET /api/v1/billings` (the *portal*
 * endpoint has one, the staff list does not), the date range comes from the external filter bar
 * rather than the toolbar picker, and sorting is fixed server-side to `BillingDate DESC, Id DESC`.
 * Only `customFilters` is read, for the two toolbar dropdowns.
 */
export function useRepBillDataTable(
  page: number,
  pageSize: number,
  _search?: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { distributorStatus?: string; paymentType?: string },
) {
  const appliedFilters = useRepBillFilterStore((s) => s.appliedFilters)

  const distributorStatus = customFilters?.distributorStatus
  const paymentType = customFilters?.paymentType

  const query = useQuery({
    queryKey: repBillKeys.list({ page, pageSize, appliedFilters, distributorStatus, paymentType }),
    queryFn: async () => {
      // The table is only mounted once `appliedFilters` is set, so this is a type guard rather
      // than a reachable state.
      if (!appliedFilters) throw new Error('No filters applied')

      const result = await getRepBillsAction(
        page,
        pageSize,
        appliedFilters.salesRepId,
        appliedFilters.dateFrom,
        appliedFilters.dateTo,
        distributorStatus || undefined,
        paymentType || undefined,
      )
      if (!result.success) throw new Error(result.error)

      const { bills, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: bills as RepBillListItem[],
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

  // Release the filter bar's spinner once the request settles, either way.
  useEffect(() => {
    if (query.isSuccess || query.isError) useRepBillFilterStore.getState().setFetching(false)
  }, [query.isSuccess, query.isError])

  return query
}

;(useRepBillDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useRepBillDetail(id: number | null) {
  return useQuery({
    queryKey: repBillKeys.detail(id ?? 0),
    queryFn: async () => {
      const result = await getRepBillDetailAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

/**
 * Bridges `AsyncSelect`'s imperative `(query) => Promise<T[]>` to the query cache.
 *
 * Components must not call server actions directly, and `AsyncSelect` cannot consume a hook.
 * `fetchQuery` satisfies both: the action stays behind the hooks layer, and repeated searches
 * for the same term come from cache instead of re-hitting the API each time the popover opens.
 */
export function useSupervisorSearchFetcher() {
  const queryClient = useQueryClient()

  return useCallback(
    (search?: string): Promise<SupervisorOption[]> =>
      queryClient.fetchQuery({
        queryKey: [...repBillKeys.supervisors, search ?? ''] as const,
        queryFn: async () => {
          const result = await getSupervisorsForSelectAction(search)
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: 5 * 60 * 1000,
      }),
    [queryClient],
  )
}

/**
 * Sales reps reporting directly to `supervisorId`.
 *
 * Reuses `getSupervisorRepsAction` from the daily-route-assignment feature rather than adding a
 * fourth "fetch users for a dropdown" action — it already calls `/subordinates?depth=1`, which
 * the API filters to active lines under active users.
 *
 * The `userRole === 'SalesRep'` filter is client-side, which is safe here and only here: the
 * response is one supervisor's *direct reports*, not a paged slice of all users, so nothing can
 * fall off the end of a page and disappear.
 */
export function useSupervisorRepsFetcher(supervisorId: number | null) {
  const queryClient = useQueryClient()

  return useCallback(
    async (): Promise<RepOption[]> => {
      if (!supervisorId) return []
      const reps = await queryClient.fetchQuery({
        queryKey: [...repBillKeys.all, 'reps', supervisorId] as const,
        queryFn: async () => {
          const result = await getSupervisorRepsAction(supervisorId)
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: 5 * 60 * 1000,
      })
      return reps.filter((r) => r.userRole === 'SalesRep' && r.isActive)
    },
    [queryClient, supervisorId],
  )
}
