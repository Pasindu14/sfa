import { useShallow } from 'zustand/react/shallow'
import { useOutletDialogStore } from './outlet.dialog-store'
import { useOutletFilterStore } from './outlet.filter-store'

export { useOutletDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useOutletDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useOutletDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedOutletId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useOutletDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedOutletId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useActivateDialog = () =>
  useOutletDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedOutletId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useOutletDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedOutletId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useOutletFilters = () =>
  useOutletFilterStore(
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
