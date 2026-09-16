import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface ProximityExemptionDialogState {
  isGrantOpen: boolean
  isRevokeOpen: boolean
  /// The grant being acted on. rowVersion is kept alongside the id because the
  /// revoke endpoint needs the concurrency token the row was read with.
  selectedId: number | null
  selectedRowVersion: number | null
  selectedUserId: number | null
  selectedName: string | null
  openRevoke: (payload: {
    id: number
    rowVersion: number
    userId: number
    name: string
  }) => void
  closeRevoke: () => void
  openGrant: () => void
  closeGrant: () => void
}

export const useProximityExemptionDialogStore = create<ProximityExemptionDialogState>()(
  devtools(
    (set) => ({
      isGrantOpen: false,
      isRevokeOpen: false,
      selectedId: null,
      selectedRowVersion: null,
      selectedUserId: null,
      selectedName: null,
      openRevoke: ({ id, rowVersion, userId, name }) =>
        set({
          isRevokeOpen: true,
          selectedId: id,
          selectedRowVersion: rowVersion,
          selectedUserId: userId,
          selectedName: name,
        }),
      closeRevoke: () =>
        set({
          isRevokeOpen: false,
          selectedId: null,
          selectedRowVersion: null,
          selectedUserId: null,
          selectedName: null,
        }),
      openGrant: () => set({ isGrantOpen: true }),
      closeGrant: () => set({ isGrantOpen: false }),
    }),
    { name: 'ProximityExemptionDialogStore' }
  )
)
