import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface ProductCategoryDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedProductCategoryId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useProductCategoryDialogStore = create<ProductCategoryDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedProductCategoryId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedProductCategoryId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedProductCategoryId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedProductCategoryId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedProductCategoryId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedProductCategoryId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedProductCategoryId: null }),
    }),
    { name: 'ProductCategoryDialogStore' }
  )
)
