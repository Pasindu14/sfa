'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  AlertDialog,
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
import {
  useApproveUnlockDialog,
  useRejectUnlockDialog,
  useRevokeUnlockDialog,
  type SelectedUnlockRequest,
} from '../../store'
import {
  useApproveRouteUnlock,
  useRejectRouteUnlock,
  useRevokeRouteUnlock,
} from '../../hooks/route-unlock-request.hooks'
import {
  approveRouteUnlockSchema,
  routeUnlockReasonSchema,
  type ApproveRouteUnlockInput,
  type RouteUnlockReasonInput,
} from '../../schema/route-unlock-request.schema'

// ── Approve Dialog ─────────────────────────────────────────────────────────

function ApproveDialog() {
  const { isOpen, selected, close } = useApproveUnlockDialog()
  const { mutate, isPending } = useApproveRouteUnlock()

  const form = useForm<ApproveRouteUnlockInput>({
    resolver: zodResolver(approveRouteUnlockSchema),
    defaultValues: { note: '' },
  })

  useEffect(() => {
    if (!isOpen) form.reset()
  }, [isOpen, form])

  function onSubmit(data: ApproveRouteUnlockInput) {
    if (!selected) return
    mutate({ id: selected.id, rowVersion: selected.rowVersion, data })
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && !isPending && close()}>
      <AlertDialogContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <AlertDialogHeader>
              <AlertDialogTitle>Approve Route Unlock</AlertDialogTitle>
              <AlertDialogDescription>
                <span className="font-semibold text-foreground">{selected?.repName}</span> will be
                able to see and bill every outlet on{' '}
                <span className="font-semibold text-foreground">{selected?.routeName}</span> until
                midnight today (Sri Lanka time).
              </AlertDialogDescription>
            </AlertDialogHeader>

            <FormField
              control={form.control}
              name="note"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Note (optional)</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="e.g. Approved — supervisor unreachable."
                      className="resize-none"
                      rows={2}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <AlertDialogFooter>
              <AlertDialogCancel type="button" disabled={isPending}>
                Cancel
              </AlertDialogCancel>
              <Button
                type="submit"
                disabled={isPending}
                className="bg-green-600 hover:bg-green-700 focus-visible:ring-green-600"
              >
                {isPending && <Spinner className="mr-2" />}
                Approve
              </Button>
            </AlertDialogFooter>
          </form>
        </Form>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Reason Dialog (reject + revoke) ────────────────────────────────────────

interface ReasonDialogProps {
  isOpen: boolean
  selected: SelectedUnlockRequest | null
  close: () => void
  isPending: boolean
  onSubmit: (selected: SelectedUnlockRequest, data: RouteUnlockReasonInput) => void
  title: string
  description: React.ReactNode
  label: string
  placeholder: string
  confirmLabel: string
}

function ReasonDialog({
  isOpen,
  selected,
  close,
  isPending,
  onSubmit,
  title,
  description,
  label,
  placeholder,
  confirmLabel,
}: ReasonDialogProps) {
  const form = useForm<RouteUnlockReasonInput>({
    resolver: zodResolver(routeUnlockReasonSchema),
    defaultValues: { reason: '' },
  })

  useEffect(() => {
    if (!isOpen) form.reset()
  }, [isOpen, form])

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && !isPending && close()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit((data) => selected && onSubmit(selected, data))}
            className="space-y-4"
          >
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{label}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={placeholder}
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
                {confirmLabel}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

function RejectDialog() {
  const { isOpen, selected, close } = useRejectUnlockDialog()
  const { mutate, isPending } = useRejectRouteUnlock()

  return (
    <ReasonDialog
      isOpen={isOpen}
      selected={selected}
      close={close}
      isPending={isPending}
      onSubmit={(s, data) => mutate({ id: s.id, rowVersion: s.rowVersion, data })}
      title="Reject Unlock Request"
      description={
        <>
          Reject the unlock request from{' '}
          <span className="font-semibold text-foreground">{selected?.repName}</span>. The rep sees
          this reason and can request again.
        </>
      }
      label="Rejection Reason"
      placeholder="e.g. Stay within the planned outlets today."
      confirmLabel="Reject"
    />
  )
}

function RevokeDialog() {
  const { isOpen, selected, close } = useRevokeUnlockDialog()
  const { mutate, isPending } = useRevokeRouteUnlock()

  return (
    <ReasonDialog
      isOpen={isOpen}
      selected={selected}
      close={close}
      isPending={isPending}
      onSubmit={(s, data) => mutate({ id: s.id, rowVersion: s.rowVersion, data })}
      title="Revoke Route Unlock"
      description={
        <>
          End the live unlock for{' '}
          <span className="font-semibold text-foreground">{selected?.repName}</span> on{' '}
          <span className="font-semibold text-foreground">{selected?.routeName}</span>. The 1 km
          geofence applies again from the rep&apos;s next sync.
        </>
      }
      label="Revoke Reason"
      placeholder="e.g. Unlock no longer needed — GPS issue resolved."
      confirmLabel="Revoke"
    />
  )
}

// ── Combined export ────────────────────────────────────────────────────────

export function RouteUnlockRequestDialogs() {
  return (
    <>
      <ApproveDialog />
      <RejectDialog />
      <RevokeDialog />
    </>
  )
}
