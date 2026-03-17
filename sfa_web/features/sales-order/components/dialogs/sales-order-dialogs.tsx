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
  useSubmitSalesOrder,
  useRepApproveSalesOrder,
  useApproveSalesOrder,
  useAcknowledgeSalesOrder,
  useFinalizeSalesOrder,
} from '../../hooks/sales-order.hooks'

function SubmitDialog() {
  const { isOpen, selectedOrderId, close } = useSubmitDialog()
  const { mutate, isPending } = useSubmitSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Submit Order</AlertDialogTitle>
          <AlertDialogDescription>
            Submit this order for Sales Rep review? This cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Submit
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function RepApproveDialog() {
  const { isOpen, selectedOrderId, close } = useRepApproveDialog()
  const { mutate, isPending } = useRepApproveSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order and forward to Manager for final approval?
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function ApproveDialog() {
  const { isOpen, selectedOrderId, close } = useApproveDialog()
  const { mutate, isPending } = useApproveSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order? The distributor will be asked to finalize.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function AcknowledgeDialog() {
  const { isOpen, selectedOrderId, close } = useAcknowledgeDialog()
  const { mutate, isPending } = useAcknowledgeSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Acknowledge Rejection</AlertDialogTitle>
          <AlertDialogDescription>
            Confirm you have read the rejection reason. The order will be cancelled.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Acknowledge
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function FinalizeDialog() {
  const { isOpen, selectedOrderId, close } = useFinalizeDialog()
  const { mutate, isPending } = useFinalizeSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Finalize Order</AlertDialogTitle>
          <AlertDialogDescription>
            Finalize this order? This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Finalize
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

export function SalesOrderDialogs() {
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
