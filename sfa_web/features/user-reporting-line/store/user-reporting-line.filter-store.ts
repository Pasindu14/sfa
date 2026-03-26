import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface UserReportingLineFilterState {
  search: string
  role: string
  reportsToUserId: number | undefined
  isActive: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setRole: (role: string) => void
  setReportsToUserId: (id: number | undefined) => void
  setIsActive: (isActive: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  role: '',
  reportsToUserId: undefined as number | undefined,
  isActive: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
}

export const useUserReportingLineFilterStore = create<UserReportingLineFilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setRole: (role) => set({ role, page: 1 }),
      setReportsToUserId: (reportsToUserId) => set({ reportsToUserId, page: 1 }),
      setIsActive: (isActive) => set({ isActive, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      resetFilters: () => set(defaultState),
    }),
    { name: 'UserReportingLineFilterStore' },
  ),
)
