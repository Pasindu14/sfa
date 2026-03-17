import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface SalesOrderDialogState {
  selectedOrderId: number | null
  isSubmitOpen: boolean
  isRepApproveOpen: boolean
  isApproveOpen: boolean
  isAcknowledgeOpen: boolean
  isFinalizeOpen: boolean
  openSubmit: (id: number) => void
  closeSubmit: () => void
  openRepApprove: (id: number) => void
  closeRepApprove: () => void
  openApprove: (id: number) => void
  closeApprove: () => void
  openAcknowledge: (id: number) => void
  closeAcknowledge: () => void
  openFinalize: (id: number) => void
  closeFinalize: () => void
}

export const useSalesOrderDialogStore = create<SalesOrderDialogState>()(
  devtools(
    (set) => ({
      selectedOrderId: null,
      isSubmitOpen: false,
      isRepApproveOpen: false,
      isApproveOpen: false,
      isAcknowledgeOpen: false,
      isFinalizeOpen: false,
      openSubmit: (id) => set({ isSubmitOpen: true, selectedOrderId: id }),
      closeSubmit: () => set({ isSubmitOpen: false, selectedOrderId: null }),
      openRepApprove: (id) => set({ isRepApproveOpen: true, selectedOrderId: id }),
      closeRepApprove: () => set({ isRepApproveOpen: false, selectedOrderId: null }),
      openApprove: (id) => set({ isApproveOpen: true, selectedOrderId: id }),
      closeApprove: () => set({ isApproveOpen: false, selectedOrderId: null }),
      openAcknowledge: (id) => set({ isAcknowledgeOpen: true, selectedOrderId: id }),
      closeAcknowledge: () => set({ isAcknowledgeOpen: false, selectedOrderId: null }),
      openFinalize: (id) => set({ isFinalizeOpen: true, selectedOrderId: id }),
      closeFinalize: () => set({ isFinalizeOpen: false, selectedOrderId: null }),
    }),
    { name: 'SalesOrderDialogStore' }
  )
)
