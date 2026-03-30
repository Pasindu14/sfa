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
  useCreateRegion,
  useUpdateRegion,
  useDeleteRegion,
  useActivateRegion,
  useDeactivateRegion,
  useRegion,
} from '../../hooks/region.hooks'
import { RegionForm } from '../forms/region-form'
import type { CreateRegionInput, UpdateRegionInput } from '../../schema/region.schema'

// --- Create ---

function CreateRegionDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateRegion()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Region</DialogTitle>
          <DialogDescription>Add a new region to the system.</DialogDescription>
        </DialogHeader>
        <RegionForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateRegionInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit ---

function EditRegionDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: region, isLoading: isLoadingRegion } = useRegion(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateRegion()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Region</DialogTitle>
          <DialogDescription>Update region information.</DialogDescription>
        </DialogHeader>
        {isLoadingRegion ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <RegionForm
            mode="edit"
            defaultValues={region}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateRegionInput })
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

function DeleteRegionDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteRegion()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Region</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently delete the region and cannot be undone.
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

function ActivateRegionDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateRegion()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Region</AlertDialogTitle>
          <AlertDialogDescription>
            The region will be marked as active and available for assignment.
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

function DeactivateRegionDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateRegion()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Region</AlertDialogTitle>
          <AlertDialogDescription>
            The region will be marked as inactive and unavailable for assignment.
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

export function RegionDialogs() {
  return (
    <>
      <CreateRegionDialog />
      <EditRegionDialog />
      <DeleteRegionDialog />
      <ActivateRegionDialog />
      <DeactivateRegionDialog />
    </>
  )
}
