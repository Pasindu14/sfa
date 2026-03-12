"use client";

import { useCallback } from "react";
import { DataTable } from "@/components/data-table/data-table";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import {
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
  useTerritoryDialogStore,
} from "../../store";
import { useTerritoryDataTable } from "../../hooks/territory.hooks";
import { getTerritoryColumns } from "../columns/territory-columns";

export function TerritoryTable() {
  const openCreate = useTerritoryDialogStore((s) => s.openCreate);
  const { open: openEdit } = useEditDialog();
  const { open: openActivate } = useActivateDialog();
  const { open: openDeactivate } = useDeactivateDialog();

  const getColumns = useCallback(
    () => getTerritoryColumns({ openEdit, openActivate, openDeactivate }),
    [openEdit, openActivate, openDeactivate],
  );

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: "territories-table",
        searchPlaceholder: "Search territories...",
      }}
      getColumns={getColumns}
      fetchDataFn={useTerritoryDataTable}
      exportConfig={{
        entityName: "territories",
        columnMapping: {
          name: "Name",
          areaName: "Area",
          isActive: "Status",
          createdAt: "Created At",
        },
        columnWidths: [{ wch: 30 }, { wch: 25 }, { wch: 12 }, { wch: 20 }],
        headers: ["Name", "Area", "Status", "Created At"],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Territory
        </Button>
      )}
    />
  );
}
