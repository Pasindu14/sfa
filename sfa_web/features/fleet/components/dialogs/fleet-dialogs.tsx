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
  useActivateDialog,
  useDeactivateDialog,
} from '../../store'
import {
  useCreateFleet,
  useUpdateFleet,
  useActivateFleet,
  useDeactivateFleet,
  useFleet,
} from '../../hooks/fleet.hooks'
import { FleetForm } from '../forms/fleet-form'
import type { CreateFleetInput, UpdateFleetInput } from '../../schema/fleet.schema'

// --- Create Dialog ---

function CreateFleetDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateFleet()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Fleet</DialogTitle>
          <DialogDescription>Add a new fleet to the system.</DialogDescription>
        </DialogHeader>
        <FleetForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateFleetInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit Dialog ---

function EditFleetDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: fleet, isLoading: isLoadingFleet } = useFleet(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateFleet()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Fleet</DialogTitle>
          <DialogDescription>Update fleet information.</DialogDescription>
        </DialogHeader>
        {isLoadingFleet ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <FleetForm
            mode="edit"
            defaultValues={fleet ? { name: fleet.name } : undefined}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateFleetInput })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Activate Dialog ---

function ActivateFleetDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateFleet()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Fleet</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the fleet as active. Products and distributors will be able to reference it.
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

function DeactivateFleetDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateFleet()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Fleet</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the fleet as inactive. It will no longer appear in product and distributor dropdowns.
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

export function FleetDialogs() {
  return (
    <>
      <CreateFleetDialog />
      <EditFleetDialog />
      <ActivateFleetDialog />
      <DeactivateFleetDialog />
    </>
  )
}
