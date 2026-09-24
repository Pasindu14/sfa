'use client'

import { useShallow } from 'zustand/react/shallow'
import { useStockFilterStore } from './stock.filter-store'

export { useStockFilterStore }

export const useStockFilters = () =>
  useStockFilterStore(
    useShallow((s) => ({
      distributorId: s.distributorId,
      stockType: s.stockType,
      includeZeroStock: s.includeZeroStock,
      appliedFilters: s.appliedFilters,
      setDistributorId: s.setDistributorId,
      setStockType: s.setStockType,
      setIncludeZeroStock: s.setIncludeZeroStock,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
