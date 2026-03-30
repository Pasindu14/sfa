import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DivisionDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedDivisionId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useDivisionDialogStore = create<DivisionDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedDivisionId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedDivisionId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedDivisionId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedDivisionId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedDivisionId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedDivisionId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedDivisionId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedDivisionId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedDivisionId: null }),
    }),
    { name: 'DivisionDialogStore' }
  )
)
