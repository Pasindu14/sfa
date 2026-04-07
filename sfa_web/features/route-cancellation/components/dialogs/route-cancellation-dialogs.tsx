'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useApproveDialog, useRejectDialog } from '../../store'
import {
  useApproveCancellation,
  useRejectCancellation,
} from '../../hooks/route-cancellation.hooks'
import {
  rejectCancellationSchema,
  type RejectCancellationInput,
} from '../../schema/route-cancellation.schema'

// ── Approve Dialog ─────────────────────────────────────────────────────────

function ApproveDialog() {
  const { isOpen, selectedId, selectedRepName, close } = useApproveDialog()
  const { mutate, isPending } = useApproveCancellation()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Cancellation</AlertDialogTitle>
          <AlertDialogDescription>
            Approve the route cancellation request for{' '}
            <span className="font-semibold text-foreground">{selectedRepName}</span>? Their
            assignment will be permanently deleted and they will be free to receive a new route.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            className="bg-green-600 hover:bg-green-700 focus:ring-green-600"
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

// ── Reject Dialog ──────────────────────────────────────────────────────────

function RejectDialog() {
  const { isOpen, selectedId, selectedRepName, close } = useRejectDialog()
  const { mutate, isPending } = useRejectCancellation()

  const form = useForm<RejectCancellationInput>({
    resolver: zodResolver(rejectCancellationSchema),
    defaultValues: { reason: '' },
  })

  useEffect(() => {
    if (!isOpen) form.reset()
  }, [isOpen, form])

  function onSubmit(data: RejectCancellationInput) {
    if (!selectedId) return
    mutate({ id: selectedId, data })
  }

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Reject Cancellation</DialogTitle>
          <DialogDescription>
            Reject the cancellation request for{' '}
            <span className="font-semibold text-foreground">{selectedRepName}</span>. Provide a
            reason so the supervisor understands why.
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Rejection Reason</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="e.g. Route disruption is temporary — rep should continue as planned."
                      className="resize-none"
                      rows={3}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={close} disabled={isPending}>
                Cancel
              </Button>
              <Button type="submit" variant="destructive" disabled={isPending}>
                {isPending && <Spinner className="mr-2" />}
                Reject
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

// ── Combined export ────────────────────────────────────────────────────────

export function RouteCancellationDialogs() {
  return (
    <>
      <ApproveDialog />
      <RejectDialog />
    </>
  )
}
