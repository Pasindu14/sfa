import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface RepBillDialogState {
  isDetailOpen: boolean
  selectedBillId: number | null
  openDetail: (id: number) => void
  closeDetail: () => void
}

export const useRepBillDialogStore = create<RepBillDialogState>()(
  devtools(
    (set) => ({
      isDetailOpen: false,
      selectedBillId: null,
      openDetail: (selectedBillId) => set({ isDetailOpen: true, selectedBillId }),
      // `selectedBillId` is kept on close so the dialog's content does not blank out mid
      // close-animation; the detail query is gated on `isOpen` instead.
      closeDetail: () => set({ isDetailOpen: false }),
    }),
    { name: 'RepBillDialogStore' },
  ),
)
