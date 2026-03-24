import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

function todayIso() {
  return new Date().toISOString().split('T')[0]
}

export interface AppliedSalesInvoiceFilters {
  date: string
  distributorId: number | null
}

interface SalesInvoiceFilterState {
  // Form values (live — updated as user interacts)
  date: string
  distributorId: number | null
  // Committed values (set on Load click — drives the query key)
  appliedFilters: AppliedSalesInvoiceFilters | null
  // Actions
  setDate: (date: string) => void
  setDistributorId: (id: number | null) => void
  applyFilters: () => void
  reset: () => void
}

export const useSalesInvoiceFilterStore = create<SalesInvoiceFilterState>()(
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
    { name: 'SalesInvoiceFilterStore' }
  )
)
