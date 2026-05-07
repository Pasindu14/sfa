import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

export type StockTypeFilter = 'Normal' | 'FreeIssue' | null

export interface AppliedStockFilters {
  distributorId: number
  stockType: StockTypeFilter
  loadCount: number
}

interface StockFilterState {
  distributorId: number | null
  stockType: StockTypeFilter
  appliedFilters: AppliedStockFilters | null
  setDistributorId: (id: number | null) => void
  setStockType: (type: StockTypeFilter) => void
  applyFilters: () => void
  reset: () => void
}

export const useStockFilterStore = create<StockFilterState>()(
  devtools(
    (set, get) => ({
      distributorId: null,
      stockType: null,
      appliedFilters: null,
      setDistributorId: (distributorId) => set({ distributorId }),
      setStockType: (stockType) => set({ stockType }),
      applyFilters: () => {
        const { distributorId, stockType, appliedFilters } = get()
        if (!distributorId) return
        set({
          appliedFilters: {
            distributorId,
            stockType,
            loadCount: (appliedFilters?.loadCount ?? 0) + 1,
          },
        })
      },
      reset: () => set({ distributorId: null, stockType: null, appliedFilters: null }),
    }),
    { name: 'StockFilterStore' }
  )
)
