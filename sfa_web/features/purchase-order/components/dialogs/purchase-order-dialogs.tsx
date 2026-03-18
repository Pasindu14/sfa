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
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../../store'
import {
  useSubmitPurchaseOrder,
  useRepApprovePurchaseOrder,
  useApprovePurchaseOrder,
  useAcknowledgePurchaseOrder,
  useFinalizePurchaseOrder,
} from '../../hooks/purchase-order.hooks'

// ── Submit ─────────────────────────────────────────────────────────────────

function SubmitDialog() {
  const { isOpen, selectedId, close } = useSubmitDialog()
  const { mutate, isPending } = useSubmitPurchaseOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Submit Order</AlertDialogTitle>
          <AlertDialogDescription>
            Submit this order for Sales Rep approval? You won't be able to edit it once submitted.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Submit
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Rep Approve ────────────────────────────────────────────────────────────

function RepApproveDialog() {
  const { isOpen, selectedId, close } = useRepApproveDialog()
  const { mutate, isPending } = useRepApprovePurchaseOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order (Rep)</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order and forward it to the Manager for final approval?
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Manager Approve ────────────────────────────────────────────────────────

function ApproveDialog() {
  const { isOpen, selectedId, close } = useApproveDialog()
  const { mutate, isPending } = useApprovePurchaseOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order? It will be sent to the Distributor for finalization.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Acknowledge ────────────────────────────────────────────────────────────

function AcknowledgeDialog() {
  const { isOpen, selectedId, close } = useAcknowledgeDialog()
  const { mutate, isPending } = useAcknowledgePurchaseOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Acknowledge Rejection</AlertDialogTitle>
          <AlertDialogDescription>
            Confirm that you have seen the rejection reason. The order will be moved to Cancelled.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Acknowledge
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Finalize ───────────────────────────────────────────────────────────────

function FinalizeDialog() {
  const { isOpen, selectedId, close } = useFinalizeDialog()
  const { mutate, isPending } = useFinalizePurchaseOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Finalize Order</AlertDialogTitle>
          <AlertDialogDescription>
            Finalize this order? This is the last step and cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Finalize
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Combined export ────────────────────────────────────────────────────────

export function PurchaseOrderDialogs() {
  return (
    <>
      <SubmitDialog />
      <RepApproveDialog />
      <ApproveDialog />
      <AcknowledgeDialog />
      <FinalizeDialog />
    </>
  )
}
