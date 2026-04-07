import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

export interface GeoAssignmentPendingFilters {
  role: string
  regionId: number
  areaId: number
  territoryId: number
  divisionId: number
  isActive: string
}

export interface GeoAssignmentCommittedFilters {
  role?: string
  regionId?: number
  areaId?: number
  territoryId?: number
  divisionId?: number
  isActive?: string
}

interface UserGeoAssignmentFilterState {
  // Pending — what the user is currently editing in the filter card
  pending: GeoAssignmentPendingFilters

  // Committed — what the table actually queries; null = never loaded yet
  committed: GeoAssignmentCommittedFilters | null

  // Pagination — active after any load
  page: number
  pageSize: number

  // Actions
  setPending: (updates: Partial<GeoAssignmentPendingFilters>) => void
  commit: () => void
  reset: () => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
}

const defaultPending: GeoAssignmentPendingFilters = {
  role: '',
  regionId: 0,
  areaId: 0,
  territoryId: 0,
  divisionId: 0,
  isActive: '',
}

export const useUserGeoAssignmentFilterStore = create<UserGeoAssignmentFilterState>()(
  devtools(
    (set, get) => ({
      pending: defaultPending,
      committed: null,
      page: 1,
      pageSize: 10,

      setPending: (updates) =>
        set((s) => ({ pending: { ...s.pending, ...updates } })),

      commit: () => {
        const { pending } = get()
        const committed: GeoAssignmentCommittedFilters = {
          role: pending.role || undefined,
          regionId: pending.regionId || undefined,
          areaId: pending.areaId || undefined,
          territoryId: pending.territoryId || undefined,
          divisionId: pending.divisionId || undefined,
          isActive: pending.isActive || undefined,
        }
        set({ committed, page: 1 })
      },

      reset: () =>
        set({ pending: defaultPending, committed: null, page: 1 }),

      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
    }),
    { name: 'UserGeoAssignmentFilterStore' },
  ),
)
