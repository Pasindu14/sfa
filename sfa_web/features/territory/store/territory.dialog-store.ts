import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface TerritoryDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedTerritoryId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useTerritoryDialogStore = create<TerritoryDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedTerritoryId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedTerritoryId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedTerritoryId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedTerritoryId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedTerritoryId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedTerritoryId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedTerritoryId: null }),
    }),
    { name: 'TerritoryDialogStore' }
  )
)
