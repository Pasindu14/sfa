import { useShallow } from 'zustand/react/shallow'
import { useSalesOrderDialogStore } from './sales-order.dialog-store'
import { useSalesOrderFilterStore } from './sales-order.filter-store'

export { useSalesOrderDialogStore }

// ── Dialog selectors ───────────────────────────────────────────────────────

export const useSubmitDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isSubmitOpen,
      selectedId: s.selectedOrderId,
      open: s.openSubmit,
      close: s.closeSubmit,
    }))
  )

export const useRepApproveDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isRepApproveOpen,
      selectedId: s.selectedOrderId,
      open: s.openRepApprove,
      close: s.closeRepApprove,
    }))
  )

export const useApproveDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isApproveOpen,
      selectedId: s.selectedOrderId,
      open: s.openApprove,
      close: s.closeApprove,
    }))
  )

export const useAcknowledgeDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isAcknowledgeOpen,
      selectedId: s.selectedOrderId,
      open: s.openAcknowledge,
      close: s.closeAcknowledge,
    }))
  )

export const useFinalizeDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isFinalizeOpen,
      selectedId: s.selectedOrderId,
      open: s.openFinalize,
      close: s.closeFinalize,
    }))
  )

// ── Filter selectors ───────────────────────────────────────────────────────

export const useSalesOrderFilters = () =>
  useSalesOrderFilterStore(
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
