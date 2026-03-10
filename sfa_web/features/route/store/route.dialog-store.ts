import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface RouteDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  selectedRouteId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

export const useRouteDialogStore = create<RouteDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      selectedRouteId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedRouteId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedRouteId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedRouteId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedRouteId: null }),
    }),
    { name: 'RouteDialogStore' }
  )
)
