import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface RouteCancellationDialogState {
  selectedId: number | null
  selectedRepName: string | null

  isApproveOpen: boolean
  isRejectOpen: boolean

  openApprove: (id: number, repName: string) => void
  closeApprove: () => void
  openReject: (id: number, repName: string) => void
  closeReject: () => void
}

export const useRouteCancellationDialogStore = create<RouteCancellationDialogState>()(
  devtools(
    (set) => ({
      selectedId: null,
      selectedRepName: null,

      isApproveOpen: false,
      isRejectOpen: false,

      openApprove: (id, repName) =>
        set({ isApproveOpen: true, selectedId: id, selectedRepName: repName }),
      closeApprove: () =>
        set({ isApproveOpen: false, selectedId: null, selectedRepName: null }),
      openReject: (id, repName) =>
        set({ isRejectOpen: true, selectedId: id, selectedRepName: repName }),
      closeReject: () =>
        set({ isRejectOpen: false, selectedId: null, selectedRepName: null }),
    }),
    { name: 'RouteCancellationDialogStore' }
  )
)
