import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface AreaDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedAreaId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useAreaDialogStore = create<AreaDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedAreaId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedAreaId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedAreaId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedAreaId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedAreaId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedAreaId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedAreaId: null }),
    }),
    { name: 'AreaDialogStore' }
  )
)
