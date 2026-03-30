'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
  useDeleteDialog,
  useDistributorDialogStore,
} from "../../store";
import { useDistributorDataTable } from "../../hooks/distributor.hooks";
import { getDistributorColumns } from "../columns/distributor-columns";

export function DistributorTable() {
  const openCreate = useDistributorDialogStore((s) => s.openCreate);
  const { open: openEdit } = useEditDialog();
  const { open: openActivate } = useActivateDialog();
  const { open: openDeactivate } = useDeactivateDialog();
  const { open: openDelete } = useDeleteDialog();

  const getColumns = useCallback(
    () => getDistributorColumns({ openEdit, openActivate, openDeactivate, openDelete }),
    [openEdit, openActivate, openDeactivate, openDelete],
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
        columnResizingTableId: "distributors-table",
        searchPlaceholder: "Search distributors...",
      }}
      getColumns={getColumns}
      fetchDataFn={useDistributorDataTable}
      exportConfig={{
        entityName: "distributors",
        columnMapping: {
          name: "Name",
          alias: "Alias",
          email: "Email",
          phone: "Phone",
          address: "Address",
          tradeDiscount: "Trade Discount (%)",
          commission: "Commission (%)",
          vatRegNo: "VAT Reg No",
          isActive: "Status",
        },
        columnWidths: [
          { wch: 25 },
          { wch: 15 },
          { wch: 25 },
          { wch: 20 },
          { wch: 35 },
          { wch: 15 },
          { wch: 15 },
          { wch: 20 },
          { wch: 12 },
        ],
        headers: [
          "Name",
          "Alias",
          "Email",
          "Phone",
          "Address",
          "Trade Discount (%)",
          "Commission (%)",
          "VAT Reg No",
          "Status",
        ],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Distributor
        </Button>
      )}
    />
  );
}
