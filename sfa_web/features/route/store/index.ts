import { useShallow } from 'zustand/react/shallow'
import { useRouteDialogStore } from './route.dialog-store'
import { useRouteFilterStore } from './route.filter-store'

export { useRouteDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useRouteDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useRouteDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedRouteId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useRouteDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedRouteId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

// --- Filter selectors ---

export const useRouteFilters = () =>
  useRouteFilterStore(
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
