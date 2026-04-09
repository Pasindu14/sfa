import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

export interface AppliedStockFilters {
  distributorId: number
}

interface StockFilterState {
  distributorId: number | null
  appliedFilters: AppliedStockFilters | null
  isFetching: boolean
  setDistributorId: (id: number | null) => void
  applyFilters: () => void
  setFetching: (v: boolean) => void
  reset: () => void
}

export const useStockFilterStore = create<StockFilterState>()(
  devtools(
    (set, get) => ({
      distributorId: null,
      appliedFilters: null,
      isFetching: false,
      setDistributorId: (distributorId) => set({ distributorId }),
      applyFilters: () => {
        const { distributorId } = get()
        if (!distributorId) return
        set({ appliedFilters: { distributorId }, isFetching: true })
      },
      setFetching: (isFetching) => set({ isFetching }),
      reset: () => set({ distributorId: null, appliedFilters: null, isFetching: false }),
    }),
    { name: 'StockFilterStore' }
  )
)
