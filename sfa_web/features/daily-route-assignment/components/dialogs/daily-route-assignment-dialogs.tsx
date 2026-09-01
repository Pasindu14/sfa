'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
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
import { useCreateDialog, useDeleteDialog } from '../../store'
import {
  useCreateDailyRouteAssignment,
  useDeleteDailyRouteAssignment,
} from '../../hooks/daily-route-assignment.hooks'
import { DailyRouteAssignmentForm } from '../forms/daily-route-assignment-form'
import type { CreateDailyRouteAssignmentInput } from '../../schema/daily-route-assignment.schema'

// --- Create ---

function CreateDailyRouteAssignmentDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateDailyRouteAssignment()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Assign Route</DialogTitle>
          <DialogDescription>
            Assign a sales rep to a route for a specific date
          </DialogDescription>
        </DialogHeader>
        <DailyRouteAssignmentForm
          onSubmit={(data) => mutate(data as CreateDailyRouteAssignmentInput)}
          onCancel={() => { close(); clearFieldErrors() }}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Delete ---

function DeleteDailyRouteAssignmentDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteDailyRouteAssignment()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Remove Route Assignment</AlertDialogTitle>
          <AlertDialogDescription>
            This will remove the route assignment for the selected date. The record is
            retained for audit purposes.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Remove
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Combined export ---

export function DailyRouteAssignmentDialogs() {
  return (
    <>
      <CreateDailyRouteAssignmentDialog />
      <DeleteDailyRouteAssignmentDialog />
    </>
  )
}
