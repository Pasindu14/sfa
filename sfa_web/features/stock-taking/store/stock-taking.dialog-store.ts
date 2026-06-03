import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface StockTakingDialogState {
  isCreateOpen: boolean
  isLockOpen: boolean
  isUnlockOpen: boolean
  isAdjustOpen: boolean
  selectedPeriodId: number | null
  selectedLineId: number | null
  selectedLineCountedQty: number
  openCreate: () => void
  closeCreate: () => void
  openLock: (id: number) => void
  closeLock: () => void
  openUnlock: (id: number) => void
  closeUnlock: () => void
  openAdjust: (lineId: number, countedQty: number) => void
  closeAdjust: () => void
}

export const useStockTakingDialogStore = create<StockTakingDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isLockOpen: false,
      isUnlockOpen: false,
      isAdjustOpen: false,
      selectedPeriodId: null,
      selectedLineId: null,
      selectedLineCountedQty: 0,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openLock: (id) => set({ isLockOpen: true, selectedPeriodId: id }),
      closeLock: () => set({ isLockOpen: false, selectedPeriodId: null }),
      openUnlock: (id) => set({ isUnlockOpen: true, selectedPeriodId: id }),
      closeUnlock: () => set({ isUnlockOpen: false, selectedPeriodId: null }),
      openAdjust: (lineId, countedQty) =>
        set({ isAdjustOpen: true, selectedLineId: lineId, selectedLineCountedQty: countedQty }),
      closeAdjust: () =>
        set({ isAdjustOpen: false, selectedLineId: null, selectedLineCountedQty: 0 }),
    }),
    { name: 'StockTakingDialogStore' }
  )
)
