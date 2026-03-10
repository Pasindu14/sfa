import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DivisionFilterState {
  search: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
}

export const useDivisionFilterStore = create<DivisionFilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      resetFilters: () => set(defaultState),
    }),
    { name: 'DivisionFilterStore' }
  )
)
