import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { toColomboDateStr } from '@/lib/utils/datetime'

function todayIso() {
  return toColomboDateStr(new Date())
}

export interface AppliedGrnFilters {
  dateFrom: string
  dateTo: string
  distributorId: number | null
}

interface GrnFilterState {
  dateFrom: string
  dateTo: string
  distributorId: number | null
  appliedFilters: AppliedGrnFilters | null
  isFetching: boolean
  setDateFrom: (date: string) => void
  setDateTo: (date: string) => void
  setDistributorId: (id: number | null) => void
  applyFilters: () => void
  setFetching: (v: boolean) => void
  reset: () => void
}

export const useGrnFilterStore = create<GrnFilterState>()(
  devtools(
    (set, get) => ({
      dateFrom: todayIso(),
      dateTo: todayIso(),
      distributorId: null,
      appliedFilters: null,
      isFetching: false,
      setDateFrom: (dateFrom) => set({ dateFrom }),
      setDateTo: (dateTo) => set({ dateTo }),
      setDistributorId: (distributorId) => set({ distributorId }),
      applyFilters: () => {
        const { dateFrom, dateTo, distributorId } = get()
        set({ appliedFilters: { dateFrom, dateTo, distributorId }, isFetching: true })
      },
      setFetching: (isFetching) => set({ isFetching }),
      reset: () => set({ dateFrom: todayIso(), dateTo: todayIso(), distributorId: null, appliedFilters: null, isFetching: false }),
    }),
    { name: 'GrnFilterStore' }
  )
)
