"use client";

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Spinner } from "@/components/ui/spinner";
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
} from "../../store";
import {
  useCreateOutlet,
  useUpdateOutlet,
  useDeleteOutlet,
  useOutlet,
  useActivateOutlet,
  useDeactivateOutlet,
} from "../../hooks/outlet.hooks";
import { OutletForm } from "../forms/outlet-form";
import type {
  CreateOutletInput,
  UpdateOutletInput,
} from "../../schema/outlet.schema";

// --- Create Dialog ---

function CreateOutletDialog() {
  const { isOpen, close } = useCreateDialog();
  const { mutate, isPending, fieldErrors, clearFieldErrors } =
    useCreateOutlet();

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) {
          close();
          clearFieldErrors();
        }
      }}
    >
      <DialogContent className="min-w-4xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create Outlet</DialogTitle>
          <DialogDescription>Add a new outlet to the system.</DialogDescription>
        </DialogHeader>
        <OutletForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateOutletInput)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  );
}

// --- Edit Dialog ---

function EditOutletDialog() {
  const { isOpen, selectedId, close } = useEditDialog();
  const { data: outlet, isLoading: isLoadingOutlet } = useOutlet(selectedId);
  const { mutate, isPending, fieldErrors, clearFieldErrors } =
    useUpdateOutlet();

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open && !isPending) {
          close();
          clearFieldErrors();
        }
      }}
    >
      <DialogContent className="min-w-4xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Outlet</DialogTitle>
          <DialogDescription>Update outlet information.</DialogDescription>
        </DialogHeader>
        {isLoadingOutlet ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <OutletForm
            mode="edit"
            initialRouteName={outlet?.routeName}
            defaultValues={
              outlet
                ? {
                    name: outlet.name,
                    address: outlet.address,
                    tel: outlet.tel,
                    email: outlet.email ?? undefined,
                    contactPerson: outlet.contactPerson ?? undefined,
                    nicNo: outlet.nicNo,
                    vatNo: outlet.vatNo ?? undefined,
                    creditLimit: outlet.creditLimit,
                    latitude: outlet.latitude,
                    longitude: outlet.longitude,
                    ownerDOB: outlet.ownerDOB ?? undefined,
                    remarks: outlet.remarks ?? undefined,
                    image: outlet.image ?? undefined,
                    outletType:
                      outlet.outletType as CreateOutletInput["outletType"],
                    outletCategory:
                      outlet.outletCategory as CreateOutletInput["outletCategory"],
                    billingPriceType:
                      (outlet.billingPriceType as CreateOutletInput["billingPriceType"]) ??
                      undefined,
                    provinceCode: outlet.provinceCode ?? undefined,
                    districtCode: outlet.districtCode ?? undefined,
                    routeId: outlet.routeId,
                    rowVersion: outlet.rowVersion,
                  }
                : undefined
            }
            onSubmit={(data) => {
              if (!selectedId) return;
              mutate({ id: selectedId, data: data as UpdateOutletInput });
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}

// --- Delete Dialog ---

function DeleteOutletDialog() {
  const { isOpen, selectedId, close } = useDeleteDialog();
  const { mutate, isPending } = useDeleteOutlet();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete Outlet</AlertDialogTitle>
          <AlertDialogDescription>
            This action cannot be undone. The outlet will be permanently removed
            from the system.
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
  );
}

// --- Activate Dialog ---

function ActivateOutletDialog() {
  const { isOpen, selectedId, close } = useActivateDialog();
  const { mutate, isPending } = useActivateOutlet();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Outlet</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the outlet as active and allow it to participate in
            business operations.
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
  );
}

// --- Deactivate Dialog ---

function DeactivateOutletDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog();
  const { mutate, isPending } = useDeactivateOutlet();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Outlet</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the outlet as inactive. It will no longer participate
            in active business operations.
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
  );
}

// --- Combined Export ---

export function OutletDialogs() {
  return (
    <>
      <CreateOutletDialog />
      <EditOutletDialog />
      <DeleteOutletDialog />
      <ActivateOutletDialog />
      <DeactivateOutletDialog />
    </>
  );
}
