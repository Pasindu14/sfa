'use client'

import { useMemo } from 'react'
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
  useDuplicateDialog,
  useSetDefaultDialog,
  useActivateDialog,
  useDeactivateDialog,
  useDeleteDialog,
} from '../../store'
import {
  useCreatePricingStructure,
  useUpdatePricingStructure,
  useDuplicatePricingStructure,
  useSetDefaultPricingStructure,
  useActivatePricingStructure,
  useDeactivatePricingStructure,
  useDeletePricingStructure,
  usePricingStructure,
} from '../../hooks/pricing-structure.hooks'
import { PricingStructureForm } from '../forms/pricing-structure-form'
import type { CreatePricingStructureInput } from '../../schema/pricing-structure.schema'

/** The API stores a blank description as null; don't send an empty string for it. */
const toRequest = ({ name, description }: CreatePricingStructureInput) => ({
  name,
  description: description || undefined,
})

// --- Create Dialog ---

function CreatePricingStructureDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreatePricingStructure()

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
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Create Pricing Structure</DialogTitle>
          <DialogDescription>
            New structures start inactive with no prices. Fill in the prices, then activate it.
            To start from an existing price list instead, use Duplicate on that row.
          </DialogDescription>
        </DialogHeader>
        <PricingStructureForm
          mode="create"
          onSubmit={(data) => mutate(toRequest(data))}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit Dialog ---

function EditPricingStructureDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: structure, isFetching } = usePricingStructure(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdatePricingStructure()

  const defaultValues = useMemo(
    () =>
      structure
        ? {
            name: structure.name,
            description: structure.description ?? '',
            rowVersion: structure.rowVersion,
          }
        : undefined,
    [structure]
  )

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
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit Pricing Structure</DialogTitle>
          <DialogDescription>Rename the structure or change its description.</DialogDescription>
        </DialogHeader>
        {isFetching ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <PricingStructureForm
            key={selectedId ?? 0}
            mode="edit"
            defaultValues={defaultValues}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: { ...toRequest(data), rowVersion: data.rowVersion } })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Duplicate Dialog ---

function DuplicatePricingStructureDialog() {
  const { isOpen, selectedId, selectedName, close } = useDuplicateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useDuplicatePricingStructure()

  const defaultValues = useMemo(
    // Keep inside the 100-char name limit even when the source name is already long.
    () => (selectedName ? { name: `${selectedName} (copy)`.slice(0, 100) } : undefined),
    [selectedName]
  )

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
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Duplicate Pricing Structure</DialogTitle>
          <DialogDescription>
            Copies every price from <span className="font-medium">{selectedName}</span> into a new
            structure. The copy starts inactive and is not the default.
          </DialogDescription>
        </DialogHeader>
        <PricingStructureForm
          key={selectedId ?? 0}
          mode="duplicate"
          defaultValues={defaultValues}
          onSubmit={(data) => {
            if (!selectedId) return
            mutate({ id: selectedId, data: toRequest(data) })
          }}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Set Default Dialog ---

function SetDefaultPricingStructureDialog() {
  const { isOpen, selectedId, selectedName, selectedRowVersion, close } = useSetDefaultDialog()
  const { mutate, isPending } = useSetDefaultPricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Make &ldquo;{selectedName}&rdquo; the default?</AlertDialogTitle>
          <AlertDialogDescription>
            New bills and back-office purchase orders will be priced from this structure. Bills
            already submitted keep the prices they were written with.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() =>
              selectedId &&
              selectedRowVersion &&
              mutate({ id: selectedId, rowVersion: selectedRowVersion })
            }
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Set as default
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Activate Dialog ---

function ActivatePricingStructureDialog() {
  const { isOpen, selectedId, selectedName, close } = useActivateDialog()
  const { mutate, isPending } = useActivatePricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Pricing Structure</AlertDialogTitle>
          <AlertDialogDescription>
            &ldquo;{selectedName}&rdquo; will become available to reps for pricing bills.
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

function DeactivatePricingStructureDialog() {
  const { isOpen, selectedId, selectedName, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivatePricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Pricing Structure</AlertDialogTitle>
          <AlertDialogDescription>
            Reps will no longer be able to price bills from &ldquo;{selectedName}&rdquo;. Its
            prices are kept and you can re-activate it at any time.
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

// --- Delete Dialog ---

function DeletePricingStructureDialog() {
  const { isOpen, selectedId, selectedName, close } = useDeleteDialog()
  const { mutate, isPending } = useDeletePricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Pricing Structure</AlertDialogTitle>
          <AlertDialogDescription>
            &ldquo;{selectedName}&rdquo; and its prices will be removed from the list. Bills
            already priced from it keep their prices and still show its name.
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

// --- Combined Export ---

export function PricingStructureDialogs() {
  return (
    <>
      <CreatePricingStructureDialog />
      <EditPricingStructureDialog />
      <DuplicatePricingStructureDialog />
      <SetDefaultPricingStructureDialog />
      <ActivatePricingStructureDialog />
      <DeactivatePricingStructureDialog />
      <DeletePricingStructureDialog />
    </>
  )
}
