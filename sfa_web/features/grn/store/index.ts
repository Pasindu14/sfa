'use client'

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'

// ── Confirm dialog store ───────────────────────────────────────────────────

interface ConfirmDialogStore {
  isOpen: boolean
  selectedGrnId: number | null
  open: (id: number) => void
  close: () => void
}

const useConfirmDialogStore = create<ConfirmDialogStore>((set) => ({
  isOpen: false,
  selectedGrnId: null,
  open: (id) => set({ isOpen: true, selectedGrnId: id }),
  close: () => set({ isOpen: false, selectedGrnId: null }),
}))

export function useConfirmDialog() {
  return useConfirmDialogStore(
    useShallow((s) => ({
      isOpen: s.isOpen,
      selectedGrnId: s.selectedGrnId,
      open: s.open,
      close: s.close,
    }))
  )
}
