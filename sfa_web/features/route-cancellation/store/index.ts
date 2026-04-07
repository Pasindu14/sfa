import { useShallow } from 'zustand/react/shallow'
import { useRouteCancellationDialogStore } from './route-cancellation.dialog-store'

export { useRouteCancellationDialogStore }

// ── Dialog selectors ───────────────────────────────────────────────────────

export const useApproveDialog = () =>
  useRouteCancellationDialogStore(
    useShallow((s) => ({
      isOpen: s.isApproveOpen,
      selectedId: s.selectedId,
      selectedRepName: s.selectedRepName,
      open: s.openApprove,
      close: s.closeApprove,
    }))
  )

export const useRejectDialog = () =>
  useRouteCancellationDialogStore(
    useShallow((s) => ({
      isOpen: s.isRejectOpen,
      selectedId: s.selectedId,
      selectedRepName: s.selectedRepName,
      open: s.openReject,
      close: s.closeReject,
    }))
  )
