import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface ProductDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeactivateOpen: boolean
  isDeleteOpen: boolean
  isActivateOpen: boolean
  selectedProductId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
}

export const useProductDialogStore = create<ProductDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeactivateOpen: false,
      isDeleteOpen: false,
      isActivateOpen: false,
      selectedProductId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedProductId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedProductId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedProductId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedProductId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedProductId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedProductId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedProductId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedProductId: null }),
    }),
    { name: 'ProductDialogStore' }
  )
)
