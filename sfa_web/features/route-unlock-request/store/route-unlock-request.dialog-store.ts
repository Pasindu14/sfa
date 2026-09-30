import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

/** Just what the dialogs need to act on a row — the row itself stays in TanStack Query. */
export type SelectedUnlockRequest = {
  id: number
  rowVersion: number
  repName: string
  routeName: string
}

interface RouteUnlockDialogState {
  selected: SelectedUnlockRequest | null
  detailId: number | null

  isApproveOpen: boolean
  isRejectOpen: boolean
  isRevokeOpen: boolean
  isDetailOpen: boolean

  openApprove: (request: SelectedUnlockRequest) => void
  closeApprove: () => void
  openReject: (request: SelectedUnlockRequest) => void
  closeReject: () => void
  openRevoke: (request: SelectedUnlockRequest) => void
  closeRevoke: () => void
  openDetail: (id: number) => void
  closeDetail: () => void
}

export const useRouteUnlockDialogStore = create<RouteUnlockDialogState>()(
  devtools(
    (set) => ({
      selected: null,
      detailId: null,

      isApproveOpen: false,
      isRejectOpen: false,
      isRevokeOpen: false,
      isDetailOpen: false,

      openApprove: (request) => set({ isApproveOpen: true, selected: request }),
      closeApprove: () => set({ isApproveOpen: false, selected: null }),
      openReject: (request) => set({ isRejectOpen: true, selected: request }),
      closeReject: () => set({ isRejectOpen: false, selected: null }),
      openRevoke: (request) => set({ isRevokeOpen: true, selected: request }),
      closeRevoke: () => set({ isRevokeOpen: false, selected: null }),
      // The sheet can stay open underneath an action dialog, so detailId is kept
      // separate from `selected`.
      openDetail: (id) => set({ isDetailOpen: true, detailId: id }),
      closeDetail: () => set({ isDetailOpen: false, detailId: null }),
    }),
    { name: 'RouteUnlockDialogStore' }
  )
)
