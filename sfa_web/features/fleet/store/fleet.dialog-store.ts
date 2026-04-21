import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface FleetDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedFleetId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useFleetDialogStore = create<FleetDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedFleetId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedFleetId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedFleetId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedFleetId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedFleetId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedFleetId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedFleetId: null }),
    }),
    { name: 'FleetDialogStore' }
  )
)
