import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DailyRouteAssignmentFilterState {
  search: string
  userId: number | undefined
  routeId: number | undefined
  date: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setUserId: (userId: number | undefined) => void
  setRouteId: (routeId: number | undefined) => void
  setDate: (date: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  userId: undefined as number | undefined,
  routeId: undefined as number | undefined,
  date: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
}

export const useDailyRouteAssignmentFilterStore = create<DailyRouteAssignmentFilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setUserId: (userId) => set({ userId, page: 1 }),
      setRouteId: (routeId) => set({ routeId, page: 1 }),
      setDate: (date) => set({ date, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      resetFilters: () => set(defaultState),
    }),
    { name: 'DailyRouteAssignmentFilterStore' },
  ),
)
