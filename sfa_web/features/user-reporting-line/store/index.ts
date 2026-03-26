import { useShallow } from 'zustand/react/shallow'
import { useUserReportingLineDialogStore } from './user-reporting-line.dialog-store'
import { useUserReportingLineFilterStore } from './user-reporting-line.filter-store'

export { useUserReportingLineDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useUserReportingLineDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    })),
  )

export const useEditDialog = () =>
  useUserReportingLineDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedId,
      open: s.openEdit,
      close: s.closeEdit,
    })),
  )

export const useDeactivateDialog = () =>
  useUserReportingLineDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    })),
  )

export const useActivateDialog = () =>
  useUserReportingLineDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedId,
      open: s.openActivate,
      close: s.closeActivate,
    })),
  )

// --- Filter selectors ---

export const useUserReportingLineFilters = () =>
  useUserReportingLineFilterStore(
    useShallow((s) => ({
      search: s.search,
      role: s.role,
      reportsToUserId: s.reportsToUserId,
      isActive: s.isActive,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setRole: s.setRole,
      setReportsToUserId: s.setReportsToUserId,
      setIsActive: s.setIsActive,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    })),
  )
