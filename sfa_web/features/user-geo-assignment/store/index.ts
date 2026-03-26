import { useShallow } from 'zustand/react/shallow'
import { useUserGeoAssignmentDialogStore } from './user-geo-assignment.dialog-store'
import { useUserGeoAssignmentFilterStore } from './user-geo-assignment.filter-store'

export { useUserGeoAssignmentDialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  useUserGeoAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    })),
  )

export const useEditDialog = () =>
  useUserGeoAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selectedId,
      open: s.openEdit,
      close: s.closeEdit,
    })),
  )

export const useDeactivateDialog = () =>
  useUserGeoAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeactivateOpen,
      selectedId: s.selectedId,
      open: s.openDeactivate,
      close: s.closeDeactivate,
    })),
  )

export const useActivateDialog = () =>
  useUserGeoAssignmentDialogStore(
    useShallow((s) => ({
      isOpen: s.isActivateOpen,
      selectedId: s.selectedId,
      open: s.openActivate,
      close: s.closeActivate,
    })),
  )

// --- Filter selectors ---

export const useUserGeoAssignmentFilters = () =>
  useUserGeoAssignmentFilterStore(
    useShallow((s) => ({
      pending: s.pending,
      committed: s.committed,
      page: s.page,
      pageSize: s.pageSize,
      setPending: s.setPending,
      commit: s.commit,
      reset: s.reset,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
    })),
  )
