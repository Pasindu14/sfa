'use client'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Spinner } from '@/components/ui/spinner'
import { useRevokeDialog } from '../../store'
import { useRevokeExemptionFromList } from '../../hooks/proximity-exemption.hooks'

function RevokeExemptionDialog() {
  const { isOpen, selectedId, selectedRowVersion, selectedUserId, selectedName, close } =
    useRevokeDialog()
  const { mutate, isPending } = useRevokeExemptionFromList()

  const canSubmit =
    selectedId !== null && selectedRowVersion !== null && selectedUserId !== null

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Revoke proximity exemption</AlertDialogTitle>
          <AlertDialogDescription>
            {selectedName
              ? `${selectedName} will immediately be required to be near an outlet to bill it.`
              : 'This rep will immediately be required to be near an outlet to bill it.'}{' '}
            Any bill they submit from here on is re-checked by the server. Their app
            catches up on its next sync.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending || !canSubmit}
            onClick={(e) => {
              // Keep the dialog mounted until the mutation settles, so the pending
              // spinner is visible and a conflict can be surfaced here.
              e.preventDefault()
              if (!canSubmit) return
              mutate({
                exemptionId: selectedId!,
                rowVersion: selectedRowVersion!,
                userId: selectedUserId!,
              })
            }}
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Revoke
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

export function ProximityExemptionDialogs() {
  return <RevokeExemptionDialog />
}
