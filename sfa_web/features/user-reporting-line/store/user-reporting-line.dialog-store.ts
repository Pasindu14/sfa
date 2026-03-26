import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface UserReportingLineDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeactivateOpen: boolean
  isActivateOpen: boolean
  selectedId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
}

export const useUserReportingLineDialogStore = create<UserReportingLineDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeactivateOpen: false,
      isActivateOpen: false,
      selectedId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedId: null }),
      openDeactivate: (id) => set({ isDeactivateOpen: true, selectedId: id }),
      closeDeactivate: () => set({ isDeactivateOpen: false, selectedId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedId: null }),
    }),
    { name: 'UserReportingLineDialogStore' },
  ),
)
