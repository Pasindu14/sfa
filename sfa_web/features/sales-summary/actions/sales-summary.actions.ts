'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
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
        asmId: filters.asmId ?? undefined,
        rsmId: filters.rsmId ?? undefined,
        nsmId: filters.nsmId ?? undefined,
        productId: filters.productId ?? undefined,
      },
    })
    return salesSummaryResponseSchema.parse(res.data.data)
  },
)

/**
 * Users for the People picker, filtered to one org role.
 *
 * Filtered server-side to active users of the requested role — every dropdown in this app loads
 * active, non-deleted records only, and for users the parameter is `isActive=true` (routes and
 * distributors use `status=Active` instead).
 */
export const getUsersByRoleForSelectAction = createAction(
  { name: 'getUsersByRoleForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async (role: string, search?: string): Promise<UserOption[]> => {
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

// ── Geo cascade ─────────────────────────────────────────────────────────────
//
// Each level is filtered server-side by its parent, so a child list can only ever contain valid
// choices. The shared actions in the region/area/division features take no parent argument even
// though the API accepts one, so these are defined here rather than changing those and risking
// the features that already depend on them.

export const getActiveRegionsAction = createAction(
  { name: 'salesSummary.getActiveRegions', requireAuth: true, requiredRole: 'Admin' },
  async (): Promise<RegionDto[]> => {
    const res = await client.get('/api/v1/regions/active')
    return res.data.data as RegionDto[]
  },
)

export const getActiveAreasForRegionAction = createAction(
  { name: 'salesSummary.getActiveAreasForRegion', requireAuth: true, requiredRole: 'Admin' },
  async (regionId: number): Promise<AreaDto[]> => {
    const res = await client.get('/api/v1/areas/active', { params: { regionId } })
    return res.data.data as AreaDto[]
  },
)

export const getActiveTerritoriesForAreaAction = createAction(
  { name: 'salesSummary.getActiveTerritoriesForArea', requireAuth: true, requiredRole: 'Admin' },
  async (areaId: number): Promise<TerritoryDto[]> => {
    const res = await client.get('/api/v1/territories/active', { params: { areaId } })
    return res.data.data as TerritoryDto[]
  },
)

export const getActiveDivisionsForTerritoryAction = createAction(
  { name: 'salesSummary.getActiveDivisionsForTerritory', requireAuth: true, requiredRole: 'Admin' },
  async (territoryId: number): Promise<DivisionDto[]> => {
    const res = await client.get('/api/v1/divisions/active', { params: { territoryId } })
    return res.data.data as DivisionDto[]
  },
)

/**
 * Routes under one division. Uses `/routes/by-division/{id}` rather than `/routes/active`, whose
 * only filter is `territoryId` — the cascade needs the division level.
 */
export const getActiveRoutesForDivisionAction = createAction(
  { name: 'salesSummary.getActiveRoutesForDivision', requireAuth: true, requiredRole: 'Admin' },
  async (divisionId: number): Promise<RouteDto[]> => {
    const res = await client.get(`/api/v1/routes/by-division/${divisionId}`)
    return res.data.data as RouteDto[]
  },
)
