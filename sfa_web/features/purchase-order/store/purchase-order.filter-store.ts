import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface PurchaseOrderFilterState {
  page: number
  pageSize: number
  search: string
  status: string
  fromDate: string
  toDate: string

  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSearch: (search: string) => void
  setStatus: (status: string) => void
  setFromDate: (fromDate: string) => void
  setToDate: (toDate: string) => void
  resetFilters: () => void
}

const defaultState = {
  page: 1,
  pageSize: 10,
  search: '',
  status: '',
  fromDate: '',
  toDate: '',
}

export const usePurchaseOrderFilterStore = create<PurchaseOrderFilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSearch: (search) => set({ search, page: 1 }),
      setStatus: (status) => set({ status, page: 1 }),
      setFromDate: (fromDate) => set({ fromDate, page: 1 }),
      setToDate: (toDate) => set({ toDate, page: 1 }),
      resetFilters: () => set(defaultState),
    }),
    { name: 'PurchaseOrderFilterStore' }
  )
)
