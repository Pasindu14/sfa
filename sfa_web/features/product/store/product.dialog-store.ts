import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface ProductDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  selectedProductId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

export const useProductDialogStore = create<ProductDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      selectedProductId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedProductId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedProductId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedProductId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedProductId: null }),
    }),
    { name: 'ProductDialogStore' }
  )
)
