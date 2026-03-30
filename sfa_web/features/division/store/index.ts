import { useShallow } from 'zustand/react/shallow'
import { useDivisionDialogStore } from './division.dialog-store'
import { useDivisionFilterStore } from './division.filter-store'

export { useDivisionDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useDivisionDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useDivisionDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedDivisionId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useDivisionDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedDivisionId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useActivateDialog = () =>
  useDivisionDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedDivisionId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useDivisionDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedDivisionId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useDivisionFilters = () =>
  useDivisionFilterStore(
    useShallow((s) => ({
      search: s.search,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    }))
  )
