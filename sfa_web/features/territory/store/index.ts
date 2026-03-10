import { useShallow } from 'zustand/react/shallow'
import { useTerritoryDialogStore } from './territory.dialog-store'
import { useTerritoryFilterStore } from './territory.filter-store'

export { useTerritoryDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useTerritoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useTerritoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedTerritoryId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useActivateDialog = () =>
  useTerritoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedTerritoryId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useTerritoryDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedTerritoryId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useTerritoryFilters = () =>
  useTerritoryFilterStore(
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
