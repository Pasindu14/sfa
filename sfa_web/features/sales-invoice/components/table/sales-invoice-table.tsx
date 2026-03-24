"use client";

import { useCallback } from "react";
import { Upload } from "lucide-react";
import { DataTable } from "@/components/data-table/data-table";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useSalesInvoiceDialogStore, useImportDialog } from "../../store";
import { useSalesInvoiceDataTable } from "../../hooks/sales-invoice.hooks";
import { getSalesInvoiceColumns } from "../columns/sales-invoice-columns";

export function SalesInvoiceTable() {
  const openDetail = useSalesInvoiceDialogStore((s) => s.openDetail);
  const { open: openImport } = useImportDialog();

  const getColumns = useCallback(
    () => getSalesInvoiceColumns({ openDetail }),
    [openDetail],
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
        columnResizingTableId: "sales-invoices-table",
        searchPlaceholder: "Search by bill no, distributor...",
      }}
      getColumns={getColumns}
      fetchDataFn={useSalesInvoiceDataTable}
      exportConfig={{
        entityName: "sales-invoices",
        columnMapping: {
          vchBillNo: "Bill No",
          distributorName: "Distributor",
          invoiceDate: "Date",
          invoiceType: "Type",
          totalAmount: "Amount",
          status: "Status",
        },
        columnWidths: [
          { wch: 20 },
          { wch: 25 },
          { wch: 15 },
          { wch: 12 },
          { wch: 18 },
          { wch: 15 },
        ],
        headers: ["Bill No", "Distributor", "Date", "Type", "Amount", "Status"],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={filters?.status ?? "all"}
          onValueChange={(value) =>
            setFilters({ ...filters, status: value === "all" ? "" : value })
          }
        >
          <SelectTrigger className="h-8 w-40">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="GrnReceived">GRN Received</SelectItem>
            <SelectItem value="Disputed">Disputed</SelectItem>
          </SelectContent>
        </Select>
      )}
      renderToolbarContent={() => (
        <Button onClick={openImport} className="gap-2">
          <Upload className="h-4 w-4" />
          Import Excel
        </Button>
      )}
    />
  );
}
