import { useShallow } from 'zustand/react/shallow'
import { useStockTakingDialogStore } from './stock-taking.dialog-store'

export { useStockTakingDialogStore }

export const useCreateDialog = () =>
  useStockTakingDialogStore(
    useShallow((s) => ({ isOpen: s.isCreateOpen, open: s.openCreate, close: s.closeCreate }))
  )

export const useLockDialog = () =>
  useStockTakingDialogStore(
    useShallow((s) => ({
      isOpen: s.isLockOpen,
      selectedId: s.selectedPeriodId,
      open: s.openLock,
      close: s.closeLock,
    }))
  )

export const useUnlockDialog = () =>
  useStockTakingDialogStore(
    useShallow((s) => ({
      isOpen: s.isUnlockOpen,
      selectedId: s.selectedPeriodId,
      open: s.openUnlock,
      close: s.closeUnlock,
    }))
  )

export const useAdjustDialog = () =>
  useStockTakingDialogStore(
    useShallow((s) => ({
      isOpen: s.isAdjustOpen,
      selectedLineId: s.selectedLineId,
      selectedLineCountedQty: s.selectedLineCountedQty,
      open: s.openAdjust,
      close: s.closeAdjust,
    }))
  )
