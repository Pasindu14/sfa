'use client'

import { useShallow } from 'zustand/react/shallow'
import { useBinCardFilterStore } from './bin-card.filter-store'

export { useBinCardFilterStore }

export const useBinCardFilters = () =>
  useBinCardFilterStore(
    useShallow((s) => ({
      distributorId: s.distributorId,
      from: s.from,
      to: s.to,
      appliedFilters: s.appliedFilters,
      setDistributorId: s.setDistributorId,
      setFrom: s.setFrom,
      setTo: s.setTo,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
