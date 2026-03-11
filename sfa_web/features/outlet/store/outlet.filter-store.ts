import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface OutletFilterState {
  search: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  statusFilter: string
  setSearch: (search: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  setStatusFilter: (status: string) => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
  statusFilter: '',
}

export const useOutletFilterStore = create<OutletFilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      setStatusFilter: (statusFilter) => set({ statusFilter, page: 1 }),
      resetFilters: () => set(defaultState),
    }),
    { name: 'OutletFilterStore' }
  )
)
