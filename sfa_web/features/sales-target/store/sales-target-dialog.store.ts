import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { useShallow } from 'zustand/react/shallow'

interface SalesTargetDialogState {
  isImportOpen: boolean
  openImport: () => void
  closeImport: () => void

  isEditOpen: boolean
  editTargetId: number | null
  openEdit: (id: number) => void
  closeEdit: () => void
}

export const useSalesTargetDialogStore = create<SalesTargetDialogState>()(
  devtools(
    (set) => ({
      isImportOpen: false,
      openImport: () => set({ isImportOpen: true }),
      closeImport: () => set({ isImportOpen: false }),

      isEditOpen: false,
      editTargetId: null,
      openEdit: (id) => set({ isEditOpen: true, editTargetId: id }),
      closeEdit: () => set({ isEditOpen: false, editTargetId: null }),
    }),
    { name: 'SalesTargetDialogStore' }
  )
)

export function useImportTargetDialog() {
  return useSalesTargetDialogStore(
    useShallow((s) => ({ isOpen: s.isImportOpen, open: s.openImport, close: s.closeImport }))
  )
}

export function useEditTargetDialog() {
  return useSalesTargetDialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.editTargetId,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )
}
