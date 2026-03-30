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
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../../store'
import {
  useCreateArea,
  useUpdateArea,
  useDeleteArea,
  useActivateArea,
  useDeactivateArea,
  useArea,
} from '../../hooks/area.hooks'
import { AreaForm } from '../forms/area-form'
import type { CreateAreaInput, UpdateAreaInput } from '../../schema/area.schema'

// --- Create ---

function CreateAreaDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateArea()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Area</DialogTitle>
          <DialogDescription>Add a new area to the system.</DialogDescription>
        </DialogHeader>
        <AreaForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateAreaInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit ---

function EditAreaDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: area, isLoading: isLoadingArea } = useArea(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateArea()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Area</DialogTitle>
          <DialogDescription>Update area information.</DialogDescription>
        </DialogHeader>
        {isLoadingArea ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <AreaForm
            mode="edit"
            defaultValues={area}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateAreaInput })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Delete ---

function DeleteAreaDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteArea()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Area</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently delete the area and cannot be undone.
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
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Activate ---

function ActivateAreaDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateArea()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Area</AlertDialogTitle>
          <AlertDialogDescription>
            The area will be marked as active and available for assignment.
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

// --- Deactivate ---

function DeactivateAreaDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateArea()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Area</AlertDialogTitle>
          <AlertDialogDescription>
            The area will be marked as inactive and unavailable for assignment.
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

// --- Combined export ---

export function AreaDialogs() {
  return (
    <>
      <CreateAreaDialog />
      <EditAreaDialog />
      <DeleteAreaDialog />
      <ActivateAreaDialog />
      <DeactivateAreaDialog />
    </>
  )
}
