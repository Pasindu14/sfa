import { useShallow } from 'zustand/react/shallow'
import { useProductCategoryDialogStore } from './product-category.dialog-store'
import { useProductCategoryFilterStore } from './product-category.filter-store'

export { useProductCategoryDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useProductCategoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useProductCategoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedProductCategoryId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useActivateDialog = () =>
  useProductCategoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedProductCategoryId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useProductCategoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedProductCategoryId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useProductCategoryFilters = () =>
  useProductCategoryFilterStore(
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
