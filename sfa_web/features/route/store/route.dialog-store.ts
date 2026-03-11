import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface RouteDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedRouteId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useRouteDialogStore = create<RouteDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedRouteId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedRouteId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedRouteId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedRouteId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedRouteId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedRouteId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedRouteId: null }),
    }),
    { name: 'RouteDialogStore' }
  )
)
