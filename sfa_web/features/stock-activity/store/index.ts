'use client'

import { useShallow } from 'zustand/react/shallow'
import { useStockActivityFilterStore } from './stock-activity.filter-store'

export { useStockActivityFilterStore }

export const useStockActivityFilters = () =>
  useStockActivityFilterStore(
    useShallow((s) => ({
      from: s.from,
      to: s.to,
      distributorId: s.distributorId,
      productId: s.productId,
      userId: s.userId,
      transactionType: s.transactionType,
      direction: s.direction,
      appliedFilters: s.appliedFilters,
      setDateRange: s.setDateRange,
      setDistributorId: s.setDistributorId,
      setProductId: s.setProductId,
      setUserId: s.setUserId,
      setTransactionType: s.setTransactionType,
      setDirection: s.setDirection,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
