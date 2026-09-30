import { useShallow } from 'zustand/react/shallow'
import { useRouteUnlockDialogStore } from './route-unlock-request.dialog-store'

export { useRouteUnlockDialogStore }
export type { SelectedUnlockRequest } from './route-unlock-request.dialog-store'

// ── Dialog selectors ───────────────────────────────────────────────────────

export const useApproveUnlockDialog = () =>
  useRouteUnlockDialogStore(
    useShallow((s) => ({
      isOpen: s.isApproveOpen,
      selected: s.selected,
      open: s.openApprove,
      close: s.closeApprove,
    }))
  )

export const useRejectUnlockDialog = () =>
  useRouteUnlockDialogStore(
    useShallow((s) => ({
      isOpen: s.isRejectOpen,
      selected: s.selected,
      open: s.openReject,
      close: s.closeReject,
    }))
  )

export const useRevokeUnlockDialog = () =>
  useRouteUnlockDialogStore(
    useShallow((s) => ({
      isOpen: s.isRevokeOpen,
      selected: s.selected,
      open: s.openRevoke,
      close: s.closeRevoke,
    }))
  )

export const useUnlockDetailSheet = () =>
  useRouteUnlockDialogStore(
    useShallow((s) => ({
      isOpen: s.isDetailOpen,
      detailId: s.detailId,
      open: s.openDetail,
      close: s.closeDetail,
    }))
  )
