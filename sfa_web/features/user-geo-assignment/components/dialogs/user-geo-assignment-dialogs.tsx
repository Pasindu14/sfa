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
  useDeactivateDialog,
  useActivateDialog,
} from "../../store";
import {
  useCreateUserAssignment,
  useUpdateUserAssignment,
  useDeactivateUserAssignment,
  useActivateUserAssignment,
  useUserAssignment,
} from "../../hooks/user-geo-assignment.hooks";
import { UserGeoAssignmentForm } from "../forms/user-geo-assignment-form";
import type {
  CreateUserGeoAssignmentInput,
  UpdateUserGeoAssignmentInput,
} from "../../schema/user-geo-assignment.schema";

// --- Create ---

function CreateUserGeoAssignmentDialog() {
  const { isOpen, close } = useCreateDialog();
  const { mutate, isPending, fieldErrors, clearFieldErrors } =
    useCreateUserAssignment();

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
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add geo assignment</DialogTitle>
          <DialogDescription>
            Assign a user to a geographic coverage area
          </DialogDescription>
        </DialogHeader>
        <UserGeoAssignmentForm
          mode="create"
          onSubmit={(data) => mutate(data as CreateUserGeoAssignmentInput)}
          onCancel={() => {
            close();
            clearFieldErrors();
          }}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  );
}

// --- Edit ---

function EditUserGeoAssignmentDialog() {
  const { isOpen, selectedId, close } = useEditDialog();
  const { data: assignment, isLoading: isLoadingAssignment } =
    useUserAssignment(selectedId);
  const { mutate, isPending, fieldErrors, clearFieldErrors } =
    useUpdateUserAssignment();

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
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Geo Assignment</DialogTitle>
          <DialogDescription>
            {assignment
              ? `Update assignment for ${assignment.userName}.`
              : "Update geo assignment."}
          </DialogDescription>
        </DialogHeader>
        {isLoadingAssignment ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <UserGeoAssignmentForm
            mode="edit"
            defaultValues={
              assignment
                ? {
                    regionId: assignment.regionId ?? undefined,
                    areaId: assignment.areaId ?? undefined,
                    territoryId: assignment.territoryId ?? undefined,
                    divisionId: assignment.divisionId ?? undefined,
                    effectiveFrom: assignment.effectiveFrom,
                    userName: assignment.userName,
                    userRole: assignment.userRole,
                    reportsToUserId: assignment.reportsToUserId ?? undefined,
                  }
                : undefined
            }
            onSubmit={(data) => {
              if (!selectedId) return;
              mutate({
                id: selectedId,
                data: data as UpdateUserGeoAssignmentInput,
              });
            }}
            onCancel={() => {
              close();
              clearFieldErrors();
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}

// --- Deactivate ---

function DeactivateUserGeoAssignmentDialog() {
  const { isOpen, selectedId, close } = useDeactivateDialog();
  const { mutate, isPending } = useDeactivateUserAssignment();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate Geo Assignment</AlertDialogTitle>
          <AlertDialogDescription>
            The geo assignment will be deactivated. The record is retained for
            audit purposes and can be reactivated at any time.
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
  );
}

// --- Activate ---

function ActivateUserGeoAssignmentDialog() {
  const { isOpen, selectedId, close } = useActivateDialog();
  const { mutate, isPending } = useActivateUserAssignment();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Activate Geo Assignment</AlertDialogTitle>
          <AlertDialogDescription>
            The geo assignment will be reactivated and the user will be assigned
            to their geographic coverage area again.
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

// --- Combined export ---

export function UserGeoAssignmentDialogs() {
  return (
    <>
      <CreateUserGeoAssignmentDialog />
      <EditUserGeoAssignmentDialog />
      <DeactivateUserGeoAssignmentDialog />
      <ActivateUserGeoAssignmentDialog />
    </>
  );
}
