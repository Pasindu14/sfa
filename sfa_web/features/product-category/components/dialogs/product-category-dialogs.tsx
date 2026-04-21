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
  useCreateProductCategory,
  useUpdateProductCategory,
  useActivateProductCategory,
  useDeactivateProductCategory,
  useProductCategory,
} from '../../hooks/product-category.hooks'
import { ProductCategoryForm } from '../forms/product-category-form'
import type { CreateProductCategoryInput, UpdateProductCategoryInput } from '../../schema/product-category.schema'

// --- Create Dialog ---

function CreateProductCategoryDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateProductCategory()

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
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Product Category</DialogTitle>
          <DialogDescription>Add a new product category to the system.</DialogDescription>
        </DialogHeader>
        <ProductCategoryForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateProductCategoryInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit Dialog ---

function EditProductCategoryDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: category, isLoading: isLoadingCategory } = useProductCategory(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateProductCategory()

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
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Product Category</DialogTitle>
          <DialogDescription>Update the category name.</DialogDescription>
        </DialogHeader>
        {isLoadingCategory ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <ProductCategoryForm
            mode="edit"
            defaultValues={category ? { name: category.name } : undefined}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateProductCategoryInput })
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

function ActivateProductCategoryDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateProductCategory()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Product Category</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the category as active. Products will be able to reference it.
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

// --- Deactivate Dialog ---

function DeactivateProductCategoryDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateProductCategory()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Product Category</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the category as inactive. It will no longer appear in the category
            selector when creating or editing products.
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

// --- Combined Export ---

export function ProductCategoryDialogs() {
  return (
    <>
      <CreateProductCategoryDialog />
      <EditProductCategoryDialog />
      <ActivateProductCategoryDialog />
      <DeactivateProductCategoryDialog />
    </>
  )
}
