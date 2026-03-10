import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface RegionDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedRegionId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useRegionDialogStore = create<RegionDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedRegionId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedRegionId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedRegionId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedRegionId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedRegionId: null }),
      openDeactivate: (id) =>
        set({ isDeactivateOpen: true, selectedRegionId: id }),
      closeDeactivate: () =>
        set({ isDeactivateOpen: false, selectedRegionId: null }),
    }),
    { name: 'RegionDialogStore' }
  )
)
