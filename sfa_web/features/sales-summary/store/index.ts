'use client'

import { useShallow } from 'zustand/react/shallow'
import { useSalesSummaryFilterStore } from './sales-summary.filter-store'

export { useSalesSummaryFilterStore }
export type { AppliedSalesSummaryFilters } from './sales-summary.filter-store'

export const useSalesSummaryFilters = () =>
  useSalesSummaryFilterStore(
    useShallow((s) => ({
      from: s.from,
      to: s.to,
      groupBy: s.groupBy,
      role: s.role,
      regionId: s.regionId,
      areaId: s.areaId,
      territoryId: s.territoryId,
      divisionId: s.divisionId,
      routeId: s.routeId,
      distributorId: s.distributorId,
      salesRepId: s.salesRepId,
      supervisorId: s.supervisorId,
      asmId: s.asmId,
      rsmId: s.rsmId,
      nsmId: s.nsmId,
      productId: s.productId,
      appliedFilters: s.appliedFilters,
      setDateRange: s.setDateRange,
      setGroupBy: s.setGroupBy,
      setGeoId: s.setGeoId,
      setFilterId: s.setFilterId,
      setRole: s.setRole,
      setRoleUserId: s.setRoleUserId,
      clearFilterIds: s.clearFilterIds,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
