import { useShallow } from 'zustand/react/shallow'
import { usePricingStructureDialogStore } from './pricing-structure.dialog-store'
import { usePricingStructureFilterStore } from './pricing-structure.filter-store'

export { usePricingStructureDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedPricingStructureId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeactivateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedPricingStructureId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

export const useDeleteDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedPricingStructureId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useActivateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedPricingStructureId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useManageItemsDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isManageItemsOpen,
      selectedId: s.selectedPricingStructureId,
      open: s.openManageItems,
      close: s.closeManageItems,
    }))
  )

// --- Filter selectors ---

export const usePricingStructureFilters = () =>
  usePricingStructureFilterStore(
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
