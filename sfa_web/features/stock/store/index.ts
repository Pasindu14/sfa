'use client'

import { useShallow } from 'zustand/react/shallow'
import { useStockFilterStore } from './stock.filter-store'

export { useStockFilterStore }

export const useStockFilters = () =>
  useStockFilterStore(
    useShallow((s) => ({
      distributorId: s.distributorId,
      appliedFilters: s.appliedFilters,
      isFetching: s.isFetching,
      setDistributorId: s.setDistributorId,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
