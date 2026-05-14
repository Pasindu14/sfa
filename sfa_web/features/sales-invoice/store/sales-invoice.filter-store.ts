import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

function todayIso() {
  const d = new Date()
  return [
    d.getFullYear(),
    String(d.getMonth() + 1).padStart(2, '0'),
    String(d.getDate()).padStart(2, '0'),
  ].join('-')
}

export interface AppliedSalesInvoiceFilters {
  dateFrom: string
  dateTo: string
  distributorId: number | null
}

interface SalesInvoiceFilterState {
  // Form values (live — updated as user interacts)
  dateFrom: string
  dateTo: string
  distributorId: number | null
  // Committed values (set on Load click — drives the query key)
  appliedFilters: AppliedSalesInvoiceFilters | null
  // Loading state — true between applyFilters() and query settlement
  isFetching: boolean
  // Actions
  setDateFrom: (date: string) => void
  setDateTo: (date: string) => void
  setDistributorId: (id: number | null) => void
  applyFilters: () => void
  setFetching: (v: boolean) => void
  reset: () => void
}

export const useSalesInvoiceFilterStore = create<SalesInvoiceFilterState>()(
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
    { name: 'SalesInvoiceFilterStore' }
  )
)
