import { useShallow } from 'zustand/react/shallow'
import { useProductDialogStore } from './product.dialog-store'
import { useProductFilterStore } from './product.filter-store'

export { useProductDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useProductDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useProductDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedProductId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useProductDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedProductId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useActivateDialog = () =>
  useProductDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedProductId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

// --- Filter selectors ---

export const useProductFilters = () =>
  useProductFilterStore(
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
