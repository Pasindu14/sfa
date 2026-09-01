import { useShallow } from 'zustand/react/shallow'
import { useDailyRouteAssignmentDialogStore } from './daily-route-assignment.dialog-store'
import { useDailyRouteAssignmentFilterStore } from './daily-route-assignment.filter-store'

export { useDailyRouteAssignmentDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useDailyRouteAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    })),
  )

export const useDeleteDialog = () =>
  useDailyRouteAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedId,
      open: s.openDelete,
      close: s.closeDelete,
    })),
  )

// --- Filter selectors ---

export const useDailyRouteAssignmentFilters = () =>
  useDailyRouteAssignmentFilterStore(
    useShallow((s) => ({
      search: s.search,
      userId: s.userId,
      routeId: s.routeId,
      date: s.date,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setUserId: s.setUserId,
      setRouteId: s.setRouteId,
      setDate: s.setDate,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    })),
  )
