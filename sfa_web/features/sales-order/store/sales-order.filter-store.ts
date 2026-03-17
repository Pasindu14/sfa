import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { format } from 'date-fns'

const today = () => format(new Date(), 'yyyy-MM-dd')

interface SalesOrderFilterState {
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
  setFromDate: (date: string) => void
  setToDate: (date: string) => void
  resetFilters: () => void
}

const getDefaultState = () => ({
  page: 1,
  pageSize: 10,
  search: '',
  status: '',
  fromDate: today(),
  toDate: today(),
})

export const useSalesOrderFilterStore = create<SalesOrderFilterState>()(
  devtools(
    (set) => ({
      ...getDefaultState(),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSearch: (search) => set({ search, page: 1 }),
      setStatus: (status) => set({ status, page: 1 }),
      setFromDate: (fromDate) => set({ fromDate, page: 1 }),
      setToDate: (toDate) => set({ toDate, page: 1 }),
      resetFilters: () => set(getDefaultState()),
    }),
    { name: 'SalesOrderFilterStore' }
  )
)
