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
  useCreateDistributor,
  useUpdateDistributor,
  useDeleteDistributor,
  useDistributor,
  useActivateDistributor,
  useDeactivateDistributor,
} from '../../hooks/distributor.hooks'
import { DistributorForm } from '../forms/distributor-form'
import type { CreateDistributorInput, UpdateDistributorInput } from '../../schema/distributor.schema'

// --- Create Dialog ---

function CreateDistributorDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateDistributor()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) {
          close()
          clearFieldErrors()
        }
      }}
    >
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create Distributor</DialogTitle>
          <DialogDescription>Add a new distributor to the system.</DialogDescription>
        </DialogHeader>
        <DistributorForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateDistributorInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit Dialog ---

function EditDistributorDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: distributor, isLoading: isLoadingDistributor } = useDistributor(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateDistributor()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) {
          close()
          clearFieldErrors()
        }
      }}
    >
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Distributor</DialogTitle>
          <DialogDescription>Update distributor information.</DialogDescription>
        </DialogHeader>
        {isLoadingDistributor ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <DistributorForm
            mode="edit"
            defaultValues={distributor}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateDistributorInput })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Delete Dialog ---

function DeleteDistributorDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteDistributor()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Distributor</AlertDialogTitle>
          <AlertDialogDescription>
            This action cannot be undone. The distributor will be permanently removed from the system.
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

// --- Activate Dialog ---

function ActivateDistributorDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateDistributor()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Distributor</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the distributor as active and allow them to participate in business operations.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={() => selectedId && mutate(selectedId)}>
            {isPending ? <Spinner className="mr-2" /> : null}
            Activate
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Deactivate Dialog ---

function DeactivateDistributorDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateDistributor()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Distributor</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the distributor as inactive. They will no longer participate in active business
            operations.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
            className="bg-orange-600 text-white hover:bg-orange-700"
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Deactivate
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Combined Export ---

export function DistributorDialogs() {
  return (
    <>
      <CreateDistributorDialog />
      <EditDistributorDialog />
      <DeleteDistributorDialog />
      <ActivateDistributorDialog />
      <DeactivateDistributorDialog />
    </>
  )
}
