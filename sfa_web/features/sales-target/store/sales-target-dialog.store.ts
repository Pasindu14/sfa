import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { useShallow } from 'zustand/react/shallow'

interface SalesTargetDialogState {
  isImportOpen: boolean
  openImport: () => void
  closeImport: () => void
}

export const useSalesTargetDialogStore = create<SalesTargetDialogState>()(
  devtools(
    (set) => ({
      isImportOpen: false,
      openImport: () => set({ isImportOpen: true }),
      closeImport: () => set({ isImportOpen: false }),
    }),
    { name: 'SalesTargetDialogStore' }
  )
)

export function useImportTargetDialog() {
  return useSalesTargetDialogStore(
    useShallow((s) => ({ isOpen: s.isImportOpen, open: s.openImport, close: s.closeImport }))
  )
}
