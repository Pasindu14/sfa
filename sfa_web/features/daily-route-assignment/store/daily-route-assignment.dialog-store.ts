import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DailyRouteAssignmentDialogState {
  isCreateOpen: boolean
  isDeleteOpen: boolean
  selectedId: number | null
  openCreate: () => void
  closeCreate: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

export const useDailyRouteAssignmentDialogStore = create<DailyRouteAssignmentDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isDeleteOpen: false,
      selectedId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedId: null }),
    }),
    { name: 'DailyRouteAssignmentDialogStore' },
  ),
)
