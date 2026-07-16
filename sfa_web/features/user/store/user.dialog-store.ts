import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface UserDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  isResetPasswordOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  selectedUserId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openResetPassword: (id: number) => void
  closeResetPassword: () => void
  openActivate: (id: number) => void
  closeActivate: () => void
  openDeactivate: (id: number) => void
  closeDeactivate: () => void
}

export const useUserDialogStore = create<UserDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      isResetPasswordOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      selectedUserId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedUserId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedUserId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedUserId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedUserId: null }),
      openResetPassword: (id) =>
        set({ isResetPasswordOpen: true, selectedUserId: id }),
      closeResetPassword: () =>
        set({ isResetPasswordOpen: false, selectedUserId: null }),
      openActivate: (id) => set({ isActivateOpen: true, selectedUserId: id }),
      closeActivate: () => set({ isActivateOpen: false, selectedUserId: null }),
      openDeactivate: (id) =>
        set({ isDeactivateOpen: true, selectedUserId: id }),
      closeDeactivate: () =>
        set({ isDeactivateOpen: false, selectedUserId: null }),
    }),
    { name: 'UserDialogStore' }
  )
)
