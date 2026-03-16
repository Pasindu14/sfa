import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface PricingStructureDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  isActivateOpen: boolean
  isManageItemsOpen: boolean
  selectedPricingStructureId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openManageItems: (id: number) => void
  closeManageItems: () => void
}

export const usePricingStructureDialogStore = create<PricingStructureDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      isActivateOpen: false,
      isManageItemsOpen: false,
      selectedPricingStructureId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedPricingStructureId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedPricingStructureId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedPricingStructureId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedPricingStructureId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedPricingStructureId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedPricingStructureId: null }),
      openManageItems: (id) => set({ isManageItemsOpen: true, selectedPricingStructureId: id }),
      closeManageItems: () => set({ isManageItemsOpen: false, selectedPricingStructureId: null }),
    }),
    { name: 'PricingStructureDialogStore' }
  )
)
