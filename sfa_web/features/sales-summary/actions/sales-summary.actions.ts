'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import {
  getActiveAreasForSelectAction,
  getActiveDivisionsForSelectAction,
  getActiveRegionsForSelectAction,
  getActiveTerritoriesForSelectAction,
} from '@/features/user-geo-assignment/actions/user-geo-assignment.actions'
import { getActiveRoutesAction } from '@/features/route/actions/route.actions'
import type { AreaDto } from '@/features/area/schema/area.schema'
import type { DivisionDto } from '@/features/division/schema/division.schema'
import type { RegionDto } from '@/features/region/schema/region.schema'
import type { RouteDto } from '@/features/route/schema/route.schema'
import type { TerritoryDto } from '@/features/territory/schema/territory.schema'
import {
  salesSummaryResponseSchema,
  type SalesSummaryFilterIds,
  type SalesSummaryGroupBy,
  type SalesSummaryResponse,
  type UserOption,
} from '../schema/sales-summary.schema'

/**
 * Fetches the sales summary for an inclusive date range.
 *
 * `from`/`to` are Colombo `YYYY-MM-DD` strings. Note the API parses dates with `DateOnly.TryParse`
 * and silently treats an unparseable value as "no filter", which WIDENS the range instead of
 * erroring — so always send strings built by `toColomboDateStr`, never `Date.toISOString()`.
 */
export const getSalesSummaryAction = createAction(
  { name: 'getSalesSummaryAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    from: string,
    to: string,
    groupBy: SalesSummaryGroupBy,
    filters: SalesSummaryFilterIds,
  ): Promise<SalesSummaryResponse> => {
    const res = await client.get('/api/v1/reports/sales-summary', {
      params: {
        from,
        to,
        groupBy,
        // `?? undefined` so axios drops the key entirely rather than sending `regionId=null`,
        // which the API would reject as a non-positive id.
        regionId: filters.regionId ?? undefined,
        areaId: filters.areaId ?? undefined,
        territoryId: filters.territoryId ?? undefined,
        divisionId: filters.divisionId ?? undefined,
        routeId: filters.routeId ?? undefined,
        distributorId: filters.distributorId ?? undefined,
        salesRepId: filters.salesRepId ?? undefined,
        supervisorId: filters.supervisorId ?? undefined,
        productId: filters.productId ?? undefined,
      },
    })
    return salesSummaryResponseSchema.parse(res.data.data)
  },
)

/**
 * Users for the Supervisor / Sales rep pickers.
 *
 * Filtered server-side to active users of the requested role — every dropdown in this app loads
 * active, non-deleted records only, and for users the parameter is `isActive=true` (routes and
 * distributors use `status=Active` instead).
 */
export const getUsersByRoleForSelectAction = createAction(
  { name: 'getUsersByRoleForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async (role: 'Supervisor' | 'SalesRep', search?: string): Promise<UserOption[]> => {
    const res = await client.get('/api/v1/users', {
      params: {
        page: 1,
        pageSize: 50,
        isActive: true,
        role,
        ...(search ? { search } : {}),
      },
    })
    return (res.data.data as { users: UserOption[] }).users
  },
)

// ── Geo / route fetchers for the optional narrowing filters ─────────────────
//
// These reuse the existing `/active` endpoints rather than adding new ones. Each returns the full
// active list (all small — a handful of regions up to a few hundred routes), so the pickers use
// AsyncSelect's `preload` mode and filter client-side. Server-side filtering to active records is
// what matters, and these endpoints already do it.

export const fetchRegionsForSelect = async (): Promise<RegionDto[]> => {
  const res = await getActiveRegionsForSelectAction()
  return res.success ? res.data : []
}

export const fetchAreasForSelect = async (): Promise<AreaDto[]> => {
  const res = await getActiveAreasForSelectAction()
  return res.success ? res.data : []
}

export const fetchTerritoriesForSelect = async (): Promise<TerritoryDto[]> => {
  const res = await getActiveTerritoriesForSelectAction()
  return res.success ? res.data : []
}

export const fetchDivisionsForSelect = async (): Promise<DivisionDto[]> => {
  const res = await getActiveDivisionsForSelectAction()
  return res.success ? res.data : []
}

export const fetchRoutesForSelect = async (): Promise<RouteDto[]> => {
  const res = await getActiveRoutesAction()
  return res.success ? res.data : []
}
