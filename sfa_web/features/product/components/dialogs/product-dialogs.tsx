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
  useDeactivateDialog,
  useDeleteDialog,
  useActivateDialog,
} from '../../store'
import {
  useCreateProduct,
  useUpdateProduct,
  useDeactivateProduct,
  useDeleteProduct,
  useActivateProduct,
  useProduct,
} from '../../hooks/product.hooks'
import { ProductForm } from '../forms/product-form'
import type { CreateProductInput, UpdateProductInput } from '../../schema/product.schema'

// --- Create Dialog ---

function CreateProductDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateProduct()

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
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create Product</DialogTitle>
          <DialogDescription>Add a new product to the catalogue.</DialogDescription>
        </DialogHeader>
        <ProductForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateProductInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit Dialog ---

function EditProductDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: product, isLoading: isLoadingProduct } = useProduct(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateProduct()

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
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Product</DialogTitle>
          <DialogDescription>Update product information.</DialogDescription>
        </DialogHeader>
        {isLoadingProduct ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <ProductForm
            mode="edit"
            defaultValues={
              product
                ? {
                    code: product.code,
                    itemDescription: product.itemDescription,
                    printDescription: product.printDescription ?? '',
                    piecesPerPack: product.piecesPerPack,
                    imageUrl: product.imageUrl ?? '',
                    remarks: product.remarks ?? '',
                  }
                : undefined
            }
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateProductInput })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// --- Deactivate Dialog ---

function DeactivateProductDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog()
  const { mutate, isPending } = useDeactivateProduct()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Product</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the product as inactive. It will no longer appear as an active catalogue
            item. You can re-activate it at any time.
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

function DeleteProductDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteProduct()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Product</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently delete the product and cannot be undone.
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

function ActivateProductDialog() {
  const { isOpen, selectedId, close } = useActivateDialog()
  const { mutate, isPending } = useActivateProduct()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Product</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the product as active and make it available in the catalogue again.
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

// --- Combined Export ---

export function ProductDialogs() {
  return (
    <>
      <CreateProductDialog />
      <EditProductDialog />
      <DeactivateProductDialog />
      <DeleteProductDialog />
      <ActivateProductDialog />
    </>
  )
}
