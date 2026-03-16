"use client";

import { useCallback } from "react";
import { DataTable } from "@/components/data-table/data-table";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import {
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useManageItemsDialog,
  usePricingStructureDialogStore,
} from "../../store";
import { usePricingStructureDataTable } from "../../hooks/pricing-structure.hooks";
import { getPricingStructureColumns } from "../columns/pricing-structure-columns";

export function PricingStructureTable() {
  const openCreate = usePricingStructureDialogStore((s) => s.openCreate);
  const { open: openEdit } = useEditDialog();
  const { open: openDelete } = useDeleteDialog();
  const { open: openActivate } = useActivateDialog();
  const { open: openManageItems } = useManageItemsDialog();

  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      getPricingStructureColumns({
        openEdit,
        openDelete,
        openActivate,
        openManageItems,
      }),
    [openEdit, openDelete, openActivate, openManageItems],
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
        columnResizingTableId: "pricing-structures-table",
        searchPlaceholder: "Search pricing structures...",
      }}
      getColumns={getColumns}
      fetchDataFn={usePricingStructureDataTable}
      exportConfig={{
        entityName: "pricing-structures",
        columnMapping: {
          name: "Name",
          description: "Description",
          itemCount: "Products",
          isActive: "Status",
        },
        columnWidths: [{ wch: 30 }, { wch: 40 }, { wch: 12 }, { wch: 12 }],
        headers: ["Name", "Description", "Products", "Status"],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Pricing Structure
        </Button>
      )}
    />
  );
}
