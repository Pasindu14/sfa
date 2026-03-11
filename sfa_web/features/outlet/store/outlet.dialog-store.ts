import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface OutletDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedOutletId: number | null
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

export const useOutletDialogStore = create<OutletDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedOutletId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedOutletId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedOutletId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedOutletId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedOutletId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedOutletId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedOutletId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedOutletId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedOutletId: null }),
    }),
    { name: 'OutletDialogStore' }
  )
)
