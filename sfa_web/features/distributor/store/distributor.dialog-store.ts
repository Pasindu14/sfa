import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DistributorDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedDistributorId: number | null
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

export const useDistributorDialogStore = create<DistributorDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedDistributorId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedDistributorId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedDistributorId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedDistributorId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedDistributorId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedDistributorId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedDistributorId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedDistributorId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedDistributorId: null }),
    }),
    { name: 'DistributorDialogStore' }
  )
)
