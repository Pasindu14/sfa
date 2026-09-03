'use client'

import { useCallback } from 'react'
import { useIsFetching, useQuery, useQueryClient } from '@tanstack/react-query'
import { userSelectKeys } from '@/lib/api/query-keys'
import {
  getSalesSummaryAction,
  getUsersByRoleForSelectAction,
} from '../actions/sales-summary.actions'
import { useSalesSummaryFilterStore } from '../store'
import type { UserOption } from '../schema/sales-summary.schema'

// ── Query keys ──────────────────────────────────────────────────────────────

export const salesSummaryKeys = {
  all: ['sales-summary'] as const,
  report: (applied: object) => [...salesSummaryKeys.all, 'report', applied] as const,
  // Shared namespace so a user mutation elsewhere can drop a deactivated person out of these
  // pickers immediately instead of leaving them there until the stale time expires.
  users: userSelectKeys.salesSummaryUser,
}

// ── Report query (fires only after Load sets appliedFilters) ────────────────

export function useSalesSummary() {
  const applied = useSalesSummaryFilterStore((s) => s.appliedFilters)

  return useQuery({
    // `loadCount` is part of the key on purpose: it makes a second press of Load a fresh request
    // that re-enters a loading state, rather than a cache hit that looks like nothing happened.
    queryKey: applied ? salesSummaryKeys.report(applied) : [...salesSummaryKeys.all, 'idle'],
    queryFn: async () => {
      if (!applied) throw new Error('No filters applied')

      const result = await getSalesSummaryAction(applied.from, applied.to, applied.groupBy, {
        regionId: applied.regionId,
        areaId: applied.areaId,
        territoryId: applied.territoryId,
        divisionId: applied.divisionId,
        routeId: applied.routeId,
        distributorId: applied.distributorId,
        salesRepId: applied.salesRepId,
        supervisorId: applied.supervisorId,
        productId: applied.productId,
      })
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: !!applied,
  })
}

export function useSalesSummaryIsFetching() {
  return useIsFetching({ queryKey: salesSummaryKeys.all }) > 0
}

// ── Select fetchers ─────────────────────────────────────────────────────────

/**
 * Bridges AsyncSelect's imperative `(query) => Promise<T[]>` to the query cache.
 *
 * Components must not call server actions directly, and AsyncSelect cannot consume a hook.
 * `fetchQuery` satisfies both, and repeated searches for the same term come from cache rather
 * than re-hitting the API every time the popover opens.
 */
export function useUsersByRoleFetcher(role: 'Supervisor' | 'SalesRep') {
  const queryClient = useQueryClient()

  return useCallback(
    (search?: string): Promise<UserOption[]> =>
      queryClient.fetchQuery({
        queryKey: [...salesSummaryKeys.users, role, search ?? ''] as const,
        queryFn: async () => {
          const result = await getUsersByRoleForSelectAction(role, search)
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: 5 * 60 * 1000,
      }),
    [queryClient, role],
  )
}
