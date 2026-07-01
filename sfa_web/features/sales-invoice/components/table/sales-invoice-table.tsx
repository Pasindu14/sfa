"use client";

import { useCallback, useState } from "react";
import { format } from "date-fns";
import {
  Upload,
  Search,
  RotateCcw,
  CalendarIcon,
  Building2,
  Loader2,
} from "lucide-react";
import { DataTable } from "@/components/data-table/data-table";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { AsyncSelect } from "@/components/async-select";
import { cn } from "@/lib/utils";
import {
  useSalesInvoiceDialogStore,
  useImportDialog,
  useDeleteDialog,
  useCreateGrnDialog,
  useSalesInvoiceFilters,
} from "../../store";
import { useSalesInvoiceDataTable } from "../../hooks/sales-invoice.hooks";
import { getSalesInvoiceColumns } from "../columns/sales-invoice-columns";
import { SalesInvoiceCreateGrnDialog } from "../dialogs/sales-invoice-create-grn-dialog";
import { getDistributorsAction } from "@/features/distributor/actions/distributor.actions";
import type { DistributorDto } from "@/features/distributor/schema/distributor.schema";
import { toColomboDateStr } from "@/lib/utils/datetime";

// ── Distributor fetcher ───────────────────────────────────────────────────
// Returns [] until user types at least 1 character — avoids loading all
// distributors on mount and is consistent with server-side search.

async function fetchDistributors(search?: string): Promise<DistributorDto[]> {
  if (!search || search.trim().length === 0) return []
  const result = await getDistributorsAction(1, 50, search.trim())
  if (!result.success) return []
  return result.data.distributors
}

// ── Date picker ───────────────────────────────────────────────────────────

function DatePicker({
  value,
  onChange,
}: {
  value: string        // "YYYY-MM-DD"
  onChange: (date: string) => void
}) {
  const [open, setOpen] = useState(false)
  // Parse as local midnight to avoid UTC timezone shifts
  const selected = value ? new Date(value + "T00:00:00") : undefined

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          className={cn(
            "h-8 w-44 justify-start text-left font-normal",
            !value && "text-muted-foreground"
          )}
        >
          <CalendarIcon className="mr-2 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
          {value ? format(selected!, "d MMM yyyy") : "Pick a date"}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={selected}
          onSelect={(day) => {
            if (day) {
              // Convert back to YYYY-MM-DD in Sri Lanka time
              onChange(toColomboDateStr(day))
            }
            setOpen(false)
          }}
          autoFocus
        />
      </PopoverContent>
    </Popover>
  )
}

// ── Filter form ───────────────────────────────────────────────────────────

function SalesInvoiceFilterForm({
  dateFrom,
  dateTo,
  distributorId,
  hasLoaded,
  isLoading,
  onDateFromChange,
  onDateToChange,
  onDistributorChange,
  onLoad,
  onReset,
  onImport,
}: {
  dateFrom: string
  dateTo: string
  distributorId: number | null
  hasLoaded: boolean
  isLoading: boolean
  onDateFromChange: (date: string) => void
  onDateToChange: (date: string) => void
  onDistributorChange: (id: number | null) => void
  onLoad: () => void
  onReset: () => void
  onImport: () => void
}) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card px-4 py-3">
      {/* Date From */}
      <div className="flex flex-col gap-1.5">
        <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
          <CalendarIcon className="h-3 w-3" />
          From Date
        </label>
        <DatePicker value={dateFrom} onChange={onDateFromChange} />
      </div>

      {/* Date To */}
      <div className="flex flex-col gap-1.5">
        <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
          <CalendarIcon className="h-3 w-3" />
          To Date
        </label>
        <DatePicker value={dateTo} onChange={onDateToChange} />
      </div>

      {/* Distributor */}
      <div className="flex flex-col gap-1.5">
        <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
          <Building2 className="h-3 w-3" />
          Distributor
          <span className="text-muted-foreground/50">(optional)</span>
        </label>
        <AsyncSelect<DistributorDto>
          label="Distributor"
          placeholder="All distributors"
          fetcher={fetchDistributors}
          value={distributorId?.toString() ?? ""}
          onChange={(val) => onDistributorChange(val ? Number(val) : null)}
          getOptionValue={(d) => d.id.toString()}
          getDisplayValue={(d) => (
            <span className="text-sm">{d.name}</span>
          )}
          renderOption={(d) => (
            <div className="flex flex-col gap-0.5 py-0.5">
              <span className="text-sm font-medium">{d.name}</span>
              {d.phone && (
                <span className="text-xs text-muted-foreground">{d.phone}</span>
              )}
            </div>
          )}
          notFound={
            <div className="py-4 text-center text-sm text-muted-foreground">
              Type to search distributors…
            </div>
          }
          noResultsMessage="No distributors found"
          width="240px"
          triggerClassName="h-8"
          clearable
        />
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2">
        <Button onClick={onLoad} disabled={isLoading} className="h-8 gap-2">
          {isLoading
            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
            : <Search className="h-3.5 w-3.5" />}
          {isLoading ? "Loading..." : hasLoaded ? "Reload" : "Load Data"}
        </Button>
        {hasLoaded && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onReset}
            className="h-8 gap-1.5 text-muted-foreground"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Reset
          </Button>
        )}
      </div>

      {/* Import button — right-aligned, always accessible */}
      <div className="ml-auto">
        <Button onClick={onImport} variant="outline" className="h-8 gap-2">
          <Upload className="h-3.5 w-3.5" />
          Import Excel
        </Button>
      </div>
    </div>
  );
}

// ── Table ─────────────────────────────────────────────────────────────────

export function SalesInvoiceTable() {
  const openDetail = useSalesInvoiceDialogStore((s) => s.openDetail);
  const { open: openImport } = useImportDialog();
  const { open: openDelete } = useDeleteDialog();
  const { open: openCreateGrn } = useCreateGrnDialog();
  const {
    dateFrom,
    dateTo,
    distributorId,
    appliedFilters,
    isFetching,
    setDateFrom,
    setDateTo,
    setDistributorId,
    applyFilters,
    reset,
  } = useSalesInvoiceFilters();

  const getColumns = useCallback(
    () => getSalesInvoiceColumns({ openDetail, openDelete, openCreateGrn }),
    [openDetail, openDelete, openCreateGrn],
  );

  return (
    <div className="flex flex-col gap-4">
      <SalesInvoiceFilterForm
        dateFrom={dateFrom}
        dateTo={dateTo}
        distributorId={distributorId}
        hasLoaded={!!appliedFilters}
        isLoading={isFetching}
        onDateFromChange={setDateFrom}
        onDateToChange={setDateTo}
        onDistributorChange={setDistributorId}
        onLoad={applyFilters}
        onReset={reset}
        onImport={openImport}
      />

      {appliedFilters ? (
        <DataTable
          key={`${appliedFilters.dateFrom}-${appliedFilters.dateTo}-${appliedFilters.distributorId ?? 'all'}`}
          config={{
            enableRowSelection: false,
            enableSearch: true,
            enableDateFilter: false,
            enableExport: false,
            enableColumnResizing: true,
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
            headers: [
              "Bill No",
              "Distributor",
              "Date",
              "Type",
              "Amount",
              "Status",
            ],
          }}
          idField="id"
          renderCustomFilters={(filters, setFilters) => (
            <Select
              value={(filters?.status as string) ?? "all"}
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
        />
      ) : (
        <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
          <CalendarIcon className="h-8 w-8 text-muted-foreground/40" />
          <p className="text-sm font-medium text-muted-foreground">
            Select a date range and click{" "}
            <span className="font-semibold">Load Data</span> to view invoices
          </p>
          <p className="text-xs text-muted-foreground/60">
            Optionally filter by distributor to narrow results
          </p>
        </div>
      )}

      <SalesInvoiceCreateGrnDialog />
    </div>
  );
}
