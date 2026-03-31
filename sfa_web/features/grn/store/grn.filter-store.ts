import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

function todayIso() {
  return new Date().toISOString().split('T')[0]
}

export interface AppliedGrnFilters {
  date: string
  distributorId: number | null
}

interface GrnFilterState {
  date: string
  distributorId: number | null
  appliedFilters: AppliedGrnFilters | null
  setDate: (date: string) => void
  setDistributorId: (id: number | null) => void
  applyFilters: () => void
  reset: () => void
}

export const useGrnFilterStore = create<GrnFilterState>()(
  devtools(
    (set, get) => ({
      date: todayIso(),
      distributorId: null,
      appliedFilters: null,
      setDate: (date) => set({ date }),
      setDistributorId: (distributorId) => set({ distributorId }),
      applyFilters: () => {
        const { date, distributorId } = get()
        set({ appliedFilters: { date, distributorId } })
      },
      reset: () => set({ date: todayIso(), distributorId: null, appliedFilters: null }),
    }),
    { name: 'GrnFilterStore' }
  )
)
