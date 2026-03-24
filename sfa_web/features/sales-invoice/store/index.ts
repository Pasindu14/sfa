'use client'

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'

interface ImportDialogStore {
  isOpen: boolean
  open: () => void
  close: () => void
}

const useImportDialogStore = create<ImportDialogStore>((set) => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
}))

export function useImportDialog() {
  return useImportDialogStore(
    useShallow((s) => ({ isOpen: s.isOpen, open: s.open, close: s.close }))
  )
}
