import { useShallow } from 'zustand/react/shallow'
import { useUserDialogStore } from './user.dialog-store'
import { useUserFilterStore } from './user.filter-store'

export { useUserDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedUserId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedUserId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

export const useChangePasswordDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isChangePasswordOpen,
      selectedId: s.selectedUserId,
      open: s.openChangePassword,
      close: s.closeChangePassword,
    }))
  )

export const useActivateDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedUserId,
      open: s.openActivate,
      close: s.closeActivate,
    }))
  )

export const useDeactivateDialog = () =>
  useUserDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedUserId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    }))
  )

// --- Filter selectors ---

export const useUserFilters = () =>
  useUserFilterStore(
    useShallow((s) => ({
      search: s.search,
      role: s.role,
      isActive: s.isActive,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setRole: s.setRole,
      setIsActive: s.setIsActive,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    }))
  )
