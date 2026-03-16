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
} from '../../store'
import {
  useCreatePricingStructure,
  useUpdatePricingStructure,
  useDeletePricingStructure,
  useActivatePricingStructure,
  usePricingStructure,
} from '../../hooks/pricing-structure.hooks'
import { PricingStructureForm } from '../forms/pricing-structure-form'
import { ManageItemsDialog } from './manage-items-dialog'
import type { CreatePricingStructureInput, UpdatePricingStructureInput } from '../../schema/pricing-structure.schema'

// --- Create Dialog ---

function CreatePricingStructureDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreatePricingStructure()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Pricing Structure</DialogTitle>
          <DialogDescription>Add a new pricing structure for products.</DialogDescription>
        </DialogHeader>
        <PricingStructureForm
          mode="create"
          onSubmit={(data) => mutate(data as CreatePricingStructureInput)}
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
  const { data: structure, isLoading: isLoadingStructure } = usePricingStructure(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdatePricingStructure()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Pricing Structure</DialogTitle>
          <DialogDescription>Update pricing structure details.</DialogDescription>
        </DialogHeader>
        {isLoadingStructure ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <PricingStructureForm
            mode="edit"
            defaultValues={{
              name: structure?.name ?? '',
              description: structure?.description ?? '',
              isDefault: structure?.isDefault ?? false,
            }}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdatePricingStructureInput })
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

function DeletePricingStructureDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeletePricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Pricing Structure</AlertDialogTitle>
          <AlertDialogDescription>
            This pricing structure will be deactivated and will no longer appear in invoice creation.
            You can reactivate it at any time.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {isPending && <Spinner className="mr-2" />}
            Deactivate
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Activate Dialog ---

function ActivatePricingStructureDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivatePricingStructure()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Pricing Structure</AlertDialogTitle>
          <AlertDialogDescription>
            This pricing structure will become active and available for invoice creation.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending && <Spinner className="mr-2" />}
            Activate
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
      <DeletePricingStructureDialog />
      <ActivatePricingStructureDialog />
      <ManageItemsDialog />
    </>
  )
}
