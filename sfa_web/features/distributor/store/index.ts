import { useShallow } from 'zustand/react/shallow'
import { useDistributorDialogStore } from './distributor.dialog-store'
import { useDistributorFilterStore } from './distributor.filter-store'

export { useDistributorDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useDistributorDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useDistributorDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedDistributorId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useDistributorDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedDistributorId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useActivateDialog = () =>
  useDistributorDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedDistributorId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useDistributorDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedDistributorId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useDistributorFilters = () =>
  useDistributorFilterStore(
    useShallow((s) => ({
      search: s.search,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      statusFilter: s.statusFilter,
      setSearch: s.setSearch,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      setStatusFilter: s.setStatusFilter,
      resetFilters: s.resetFilters,
    }))
  )
