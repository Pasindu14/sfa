import { useShallow } from 'zustand/react/shallow'
import { useProximityExemptionDialogStore } from './proximity-exemption.dialog-store'

export { useProximityExemptionDialogStore }

export const useGrantDialog = () =>
  useProximityExemptionDialogStore(
    useShallow((s) => ({
      isOpen: s.isGrantOpen,
      open: s.openGrant,
      close: s.closeGrant,
    }))
  )

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
