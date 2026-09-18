import { useShallow } from 'zustand/react/shallow'
import { usePricingStructureDialogStore } from './pricing-structure.dialog-store'
import { usePricingStructureFilterStore } from './pricing-structure.filter-store'

export { usePricingStructureDialogStore }
export type { PricingStructureSelection } from './pricing-structure.dialog-store'

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
      selectedId: s.selectedId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDuplicateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isDuplicateOpen,
      selectedId: s.selectedId,
      selectedName: s.selectedName,
      open: s.openDuplicate,
      close: s.closeDuplicate,
    }))
  )

export const useSetDefaultDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isSetDefaultOpen,
      selectedId: s.selectedId,
      selectedName: s.selectedName,
      selectedRowVersion: s.selectedRowVersion,
      open: s.openSetDefault,
      close: s.closeSetDefault,
    }))
  )

export const useActivateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedId,
      selectedName: s.selectedName,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedId,
      selectedName: s.selectedName,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

export const useDeleteDialog = () =>
  usePricingStructureDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedId,
      selectedName: s.selectedName,
      open: s.openDelete,
      close: s.closeDelete,
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
