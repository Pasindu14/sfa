'use client'

import { useCallback } from 'react'
import { useIsFetching, useQuery, useQueryClient } from '@tanstack/react-query'
import { userSelectKeys } from '@/lib/api/query-keys'
import {
  getActiveAreasForRegionAction,
  getActiveDivisionsForTerritoryAction,
  getActiveRegionsAction,
  getActiveRoutesForDivisionAction,
  getActiveTerritoriesForAreaAction,
  getSalesSummaryAction,
  getUsersByRoleForSelectAction,
} from '../actions/sales-summary.actions'
import { useSalesSummaryFilterStore } from '../store'
import type { UserOption } from '../schema/sales-summary.schema'

// ── Query keys ──────────────────────────────────────────────────────────────

export const salesSummaryKeys = {
  all: ['sales-summary'] as const,
  report: (applied: object) => [...salesSummaryKeys.all, 'report', applied] as const,
  geo: (level: string, parentId: number | null) =>
    [...salesSummaryKeys.all, 'geo', level, parentId] as const,
  // Shared namespace so a user mutation elsewhere can drop a deactivated person out of these
  // pickers immediately instead of leaving them there until the stale time expires.
  users: userSelectKeys.salesSummaryUser,
}

const SELECT_STALE_TIME = 5 * 60 * 1000

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
        asmId: applied.asmId,
        rsmId: applied.rsmId,
        nsmId: applied.nsmId,
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
 * `fetchQuery` satisfies both, and re-opening a picker for the same parent is a cache hit rather
 * than another round trip through the server action + API.
 */
export function useUsersByRoleFetcher(role: string | null) {
  const queryClient = useQueryClient()

  return useCallback(
    (search?: string): Promise<UserOption[]> => {
      if (!role) return Promise.resolve([])
      return queryClient.fetchQuery({
        queryKey: [...salesSummaryKeys.users, role, search ?? ''] as const,
        queryFn: async () => {
          const result = await getUsersByRoleForSelectAction(role, search)
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: SELECT_STALE_TIME,
      })
    },
    [queryClient, role],
  )
}

/**
 * One cached fetcher per geo level, keyed by its parent id.
 *
 * Returns an empty list when the parent is unset, so a child picker can never issue a request
 * before its parent is chosen — that guarantee, plus the collapsed sections, is what keeps page
 * load at zero dropdown requests.
 */
function useGeoFetcher<T>(
  level: string,
  parentId: number | null,
  action: (parentId: number) => Promise<{ success: true; data: T[] } | { success: false; error: string }>,
) {
  const queryClient = useQueryClient()

  return useCallback((): Promise<T[]> => {
    if (parentId === null) return Promise.resolve([])
    return queryClient.fetchQuery({
      queryKey: salesSummaryKeys.geo(level, parentId),
      queryFn: async () => {
        const result = await action(parentId)
        if (!result.success) throw new Error(result.error)
        return result.data
      },
      staleTime: SELECT_STALE_TIME,
    })
  }, [queryClient, level, parentId, action])
}

export function useRegionsFetcher() {
  const queryClient = useQueryClient()

  return useCallback(
    () =>
      queryClient.fetchQuery({
        queryKey: salesSummaryKeys.geo('regions', null),
        queryFn: async () => {
          const result = await getActiveRegionsAction()
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: SELECT_STALE_TIME,
      }),
    [queryClient],
  )
}

export const useAreasFetcher = (regionId: number | null) =>
  useGeoFetcher('areas', regionId, getActiveAreasForRegionAction)

export const useTerritoriesFetcher = (areaId: number | null) =>
  useGeoFetcher('territories', areaId, getActiveTerritoriesForAreaAction)

export const useDivisionsFetcher = (territoryId: number | null) =>
  useGeoFetcher('divisions', territoryId, getActiveDivisionsForTerritoryAction)

export const useRoutesFetcher = (divisionId: number | null) =>
  useGeoFetcher('routes', divisionId, getActiveRoutesForDivisionAction)
