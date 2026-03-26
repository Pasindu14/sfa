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
import { useCreateDialog, useEditDialog, useDeactivateDialog, useActivateDialog } from '../../store'
import {
  useCreateUserReportingLine,
  useUpdateUserReportingLine,
  useDeactivateUserReportingLine,
  useActivateUserReportingLine,
  useUserReportingLine,
} from '../../hooks/user-reporting-line.hooks'
import { UserReportingLineForm } from '../forms/user-reporting-line-form'
import type {
  CreateUserReportingLineInput,
  UpdateUserReportingLineInput,
} from '../../schema/user-reporting-line.schema'

// --- Create ---

function CreateUserReportingLineDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateUserReportingLine()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Add reporting line</DialogTitle>
          <DialogDescription>Define who a user directly reports to</DialogDescription>
        </DialogHeader>
        <UserReportingLineForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateUserReportingLineInput)}
          onCancel={() => { close(); clearFieldErrors() }}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit ---

function EditUserReportingLineDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: line, isLoading: isLoadingLine } = useUserReportingLine(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateUserReportingLine()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Reporting Line</DialogTitle>
          <DialogDescription>
            {line ? `Update who ${line.userName} reports to.` : 'Update reporting line.'}
          </DialogDescription>
        </DialogHeader>
        {isLoadingLine ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <UserReportingLineForm
            mode="edit"
            defaultValues={
              line
                ? {
                    reportsToUserId: line.reportsToUserId,
                    effectiveFrom: line.effectiveFrom,
                    userName: line.userName,
                    userRole: line.userRole,
                  }
                : undefined
            }
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateUserReportingLineInput })
            }}
            onCancel={() => { close(); clearFieldErrors() }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Deactivate ---

function DeactivateUserReportingLineDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateUserReportingLine()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Reporting Line</AlertDialogTitle>
          <AlertDialogDescription>
            The reporting line will be deactivated. The record is retained for audit
            purposes and can be reactivated at any time.
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
            Deactivate
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Activate ---

function ActivateUserReportingLineDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateUserReportingLine()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Reporting Line</AlertDialogTitle>
          <AlertDialogDescription>
            The reporting line will be reactivated and the user will appear in the active
            org chart again.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Activate
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Combined export ---

export function UserReportingLineDialogs() {
  return (
    <>
      <CreateUserReportingLineDialog />
      <EditUserReportingLineDialog />
      <DeactivateUserReportingLineDialog />
      <ActivateUserReportingLineDialog />
    </>
  )
}
