import { useShallow } from 'zustand/react/shallow'
import { usePurchaseOrderDialogStore } from './purchase-order.dialog-store'
import { usePurchaseOrderFilterStore } from './purchase-order.filter-store'

export { usePurchaseOrderDialogStore }

// ── Dialog selectors ───────────────────────────────────────────────────────

export const useSubmitDialog = () =>
  usePurchaseOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isSubmitOpen,
      selectedId: s.selectedOrderId,
      open: s.openSubmit,
      close: s.closeSubmit,
    }))
  )

export const useRepApproveDialog = () =>
  usePurchaseOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isRepApproveOpen,
      selectedId: s.selectedOrderId,
      open: s.openRepApprove,
      close: s.closeRepApprove,
    }))
  )

export const useApproveDialog = () =>
  usePurchaseOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isApproveOpen,
      selectedId: s.selectedOrderId,
      open: s.openApprove,
      close: s.closeApprove,
    }))
  )

export const useAcknowledgeDialog = () =>
  usePurchaseOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isAcknowledgeOpen,
      selectedId: s.selectedOrderId,
      open: s.openAcknowledge,
      close: s.closeAcknowledge,
    }))
  )

export const useFinalizeDialog = () =>
  usePurchaseOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isFinalizeOpen,
      selectedId: s.selectedOrderId,
      open: s.openFinalize,
      close: s.closeFinalize,
    }))
  )

// ── Filter selectors ───────────────────────────────────────────────────────

export const usePurchaseOrderFilters = () =>
  usePurchaseOrderFilterStore(
    useShallow((s) => ({
      page: s.page,
      pageSize: s.pageSize,
      search: s.search,
      status: s.status,
      fromDate: s.fromDate,
      toDate: s.toDate,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSearch: s.setSearch,
      setStatus: s.setStatus,
      setFromDate: s.setFromDate,
      setToDate: s.setToDate,
      resetFilters: s.resetFilters,
    }))
  )
