import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

export type StockTypeFilter = 'Normal' | 'FreeIssue' | null

export interface AppliedStockFilters {
  distributorId: number
  stockType: StockTypeFilter
  includeZeroStock: boolean
  loadCount: number
}

interface StockFilterState {
  distributorId: number | null
  stockType: StockTypeFilter
  // Show zero-balance items too, including active products the distributor never held.
  includeZeroStock: boolean
  appliedFilters: AppliedStockFilters | null
  setDistributorId: (id: number | null) => void
  setStockType: (type: StockTypeFilter) => void
  setIncludeZeroStock: (include: boolean) => void
  applyFilters: () => void
  reset: () => void
}

export const useStockFilterStore = create<StockFilterState>()(
  devtools(
    (set, get) => ({
      distributorId: null,
      stockType: null,
      includeZeroStock: true,
      appliedFilters: null,
      setDistributorId: (distributorId) => set({ distributorId }),
      setStockType: (stockType) => set({ stockType }),
      setIncludeZeroStock: (includeZeroStock) => set({ includeZeroStock }),
      applyFilters: () => {
        const { distributorId, stockType, includeZeroStock, appliedFilters } = get()
        if (!distributorId) return
        set({
          appliedFilters: {
            distributorId,
            stockType,
            includeZeroStock,
            loadCount: (appliedFilters?.loadCount ?? 0) + 1,
          },
        })
      },
      reset: () => set({ distributorId: null, stockType: null, includeZeroStock: true, appliedFilters: null }),
    }),
    { name: 'StockFilterStore' }
  )
)
