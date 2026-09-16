import { useShallow } from 'zustand/react/shallow'
import { useProximityExemptionDialogStore } from './proximity-exemption.dialog-store'

export { useProximityExemptionDialogStore }

export const useRevokeDialog = () =>
  useProximityExemptionDialogStore(
    useShallow((s) => ({
      isOpen: s.isRevokeOpen,
      selectedId: s.selectedId,
      selectedRowVersion: s.selectedRowVersion,
      selectedUserId: s.selectedUserId,
      selectedName: s.selectedName,
      open: s.openRevoke,
      close: s.closeRevoke,
    }))
  )
