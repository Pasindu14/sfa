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
} from '../../store'
import {
  useCreateRoute,
  useUpdateRoute,
  useDeleteRoute,
  useRoute,
} from '../../hooks/route.hooks'
import { RouteForm } from '../forms/route-form'
import type { CreateRouteInput, UpdateRouteInput } from '../../schema/route.schema'

// --- Create ---

function CreateRouteDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreateRoute()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create Route</DialogTitle>
          <DialogDescription>Add a new route to the system.</DialogDescription>
        </DialogHeader>
        <RouteForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateRouteInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

// --- Edit ---

function EditRouteDialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: route, isLoading: isLoadingRoute } = useRoute(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdateRoute()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Route</DialogTitle>
          <DialogDescription>Update route information.</DialogDescription>
        </DialogHeader>
        {isLoadingRoute ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <RouteForm
            mode="edit"
            defaultValues={route ? { ...route, description: route.description ?? undefined } : undefined}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as UpdateRouteInput })
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

function DeleteRouteDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDeleteRoute()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Route</AlertDialogTitle>
          <AlertDialogDescription>
            This action cannot be undone. The route will be permanently removed.
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

// --- Combined export ---

export function RouteDialogs() {
  return (
    <>
      <CreateRouteDialog />
      <EditRouteDialog />
      <DeleteRouteDialog />
    </>
  )
}
