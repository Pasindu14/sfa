"use client";

import { useRef, useState } from "react";
import {
  Upload,
  ArrowLeft,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  FileText,
  Package,
  Banknote,
  SkipForward,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { ScrollArea } from "@/components/ui/scroll-area";
import { parseExcelFile } from "../../lib/parse-excel";
import { InvoicePreview } from "./invoice-preview";
import { useImportDialog } from "../../store";
import { useImportSalesInvoices } from '../../hooks/sales-invoice.hooks';
import type {
  ImportBatchResult,
  ImportSalesInvoicesPayload,
} from "../../schema/sales-invoice.schema";

type View = "picker" | "preview" | "result";

// ── Batch result view ─────────────────────────────────────────────────────

function BatchResultView({
  result,
  onClose,
}: {
  result: ImportBatchResult;
  onClose: () => void;
}) {
  const isCompleted = result.status === "Completed";
  const isPartial = result.status === "PartialFailed";

  const StatusIcon = isCompleted
    ? CheckCircle2
    : isPartial
      ? AlertTriangle
      : XCircle;

  const statusColor = isCompleted
    ? "text-green-600"
    : isPartial
      ? "text-amber-600"
      : "text-destructive";

  const statusBg = isCompleted
    ? "bg-green-50 border-green-200"
    : isPartial
      ? "bg-amber-50 border-amber-200"
      : "bg-destructive/5 border-destructive/20";

  const statusLabel = isCompleted
    ? "All invoices imported successfully"
    : isPartial
      ? "Import completed with skipped invoices"
      : "Import failed";

  return (
    <div className="space-y-4">
      {/* Status banner */}
      <div
        className={`flex items-center gap-3 rounded-lg border px-4 py-3 ${statusBg}`}
      >
        <StatusIcon className={`h-5 w-5 shrink-0 ${statusColor}`} />
        <div className="min-w-0 flex-1">
          <p className={`text-sm font-semibold ${statusColor}`}>
            {statusLabel}
          </p>
          <p className="text-xs text-muted-foreground">
            Batch{" "}
            <code className="rounded bg-background/60 px-1.5 py-0.5 font-mono text-xs">
              {result.batchNumber}
            </code>
          </p>
        </div>
      </div>

      {/* Stat cards — same pattern as InvoicePreview SummaryCards */}
      <div className="grid grid-cols-4 gap-3">
        <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-blue-100 text-blue-600">
            <FileText className="h-4 w-4" />
          </div>
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Imported</p>
            <p className="text-xl font-bold leading-tight">
              {result.importedInvoices}
              <span className="ml-0.5 text-sm font-normal text-muted-foreground">
                /{result.totalInvoices}
              </span>
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-violet-100 text-violet-600">
            <Package className="h-4 w-4" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Line Items</p>
            <p className="text-xl font-bold">{result.totalItems}</p>
          </div>
        </div>

        <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-amber-100 text-amber-600">
            <SkipForward className="h-4 w-4" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Skipped</p>
            <p
              className={`text-xl font-bold ${result.skippedInvoices > 0 ? "text-amber-600" : ""}`}
            >
              {result.skippedInvoices}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-green-100 text-green-600">
            <Banknote className="h-4 w-4" />
          </div>
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Total Amount</p>
            <p className="text-base font-bold leading-tight">
              {result.totalAmount.toLocaleString("en-LK", {
                minimumFractionDigits: 2,
              })}
            </p>
          </div>
        </div>
      </div>

      {/* Skipped invoice table */}
      {result.errors.length > 0 && (
        <div className="rounded-lg border border-destructive/20 overflow-hidden">
          {/* Section header */}
          <div className="flex items-center gap-2 border-b border-destructive/20 bg-destructive/5 px-3 py-2">
            <XCircle className="h-3.5 w-3.5 shrink-0 text-destructive" />
            <span className="text-xs font-semibold text-destructive">
              Skipped Invoices
            </span>
            <span className="ml-auto rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
              {result.errors.length}
            </span>
          </div>

          {/* Column headers */}
          <div className="grid grid-cols-[2rem_10rem_1fr] gap-x-3 border-b bg-muted/50 px-3 py-1.5 text-xs font-medium text-muted-foreground">
            <span>#</span>
            <span>Voucher No</span>
            <span>Reason</span>
          </div>

          {/* Rows */}
          <ScrollArea className="h-96">
            <div className="divide-y divide-border/60">
              {result.errors.map((err, i) => (
                <div
                  key={i}
                  className="grid grid-cols-[2rem_10rem_1fr] items-start gap-x-3 px-3 py-2 text-sm transition-colors hover:bg-muted/30"
                >
                  <span className="text-xs tabular-nums text-muted-foreground/60 pt-0.5">
                    {i + 1}
                  </span>
                  <code className="truncate rounded bg-muted px-1.5 py-0.5 font-mono text-xs font-medium">
                    {err.vchBillNo}
                  </code>
                  <span className="text-xs text-muted-foreground leading-relaxed">
                    {err.reason}
                  </span>
                </div>
              ))}
            </div>
          </ScrollArea>
        </div>
      )}

      <DialogFooter>
        <Button onClick={onClose} className="min-w-24">
          Done
        </Button>
      </DialogFooter>
    </div>
  );
}

// ── File picker view ──────────────────────────────────────────────────────

function FilePicker({
  onParsed,
}: {
  onParsed: (payload: ImportSalesInvoicesPayload) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [parseError, setParseError] = useState<string | null>(null);
  const [isParsing, setIsParsing] = useState(false);

  async function handlePreview() {
    const file = inputRef.current?.files?.[0];
    if (!file) return;
    setIsParsing(true);
    setParseError(null);
    try {
      const buffer = await file.arrayBuffer();
      const payload = parseExcelFile(buffer, file.name);
      if (payload.invoices.length === 0) {
        setParseError(
          "No invoices found — check the file format or sheet layout.",
        );
        return;
      }
      onParsed(payload);
    } catch (err) {
      setParseError(
        "Failed to parse file. Make sure this is a valid BUSY ERP Excel export.",
      );
    } finally {
      setIsParsing(false);
    }
  }

  return (
    <div className="space-y-4">
      <div
        className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/25 p-12 text-center transition-colors hover:border-muted-foreground/50 hover:bg-muted/30"
        onClick={() => inputRef.current?.click()}
      >
        <Upload className="h-8 w-8 text-muted-foreground/50" />
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx"
          className="hidden"
          onChange={(e) => {
            setFileName(e.target.files?.[0]?.name ?? null);
            setParseError(null);
          }}
        />
        {fileName ? (
          <>
            <p className="font-medium">{fileName}</p>
            <p className="text-sm text-muted-foreground">
              Click to change file
            </p>
          </>
        ) : (
          <>
            <p className="font-medium">Click to select file</p>
            <p className="text-sm text-muted-foreground">
              BUSY ERP sales voucher export (.xlsx)
            </p>
          </>
        )}
      </div>

      {parseError && (
        <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {parseError}
        </p>
      )}

      <DialogFooter>
        <Button onClick={handlePreview} disabled={!fileName || isParsing}>
          {isParsing ? (
            <>
              <Spinner className="mr-2" />
              Parsing…
            </>
          ) : (
            "Preview Data"
          )}
        </Button>
      </DialogFooter>
    </div>
  );
}

// ── Main dialog ───────────────────────────────────────────────────────────

export function SalesInvoiceImportDialog() {
  const { isOpen, close } = useImportDialog();
  const { mutate, isPending, batchResult, closeAndReset } =
    useImportSalesInvoices();
  const [view, setView] = useState<View>("picker");
  const [payload, setPayload] = useState<ImportSalesInvoicesPayload | null>(
    null,
  );

  function handleParsed(p: ImportSalesInvoicesPayload) {
    setPayload(p);
    setView("preview");
  }

  function handleConfirmImport() {
    if (!payload) return;
    mutate(payload, {
      onSuccess: () => setView("result"),
    });
  }

  function handleOpenChange(open: boolean) {
    if (!open && !isPending) {
      closeAndReset();
      setView("picker");
      setPayload(null);
    }
  }

  const titles: Record<View, string> = {
    picker: "Import Sales Invoices",
    preview: "Preview Parsed Data",
    result: "Import Complete",
  };
  const descriptions: Record<View, string> = {
    picker: "Upload a BUSY ERP sales voucher Excel file (.xlsx).",
    preview: "Review the data extracted from the file before importing.",
    result: "Review the results of your import batch below.",
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="w-[80vw]! max-w-[96vw]! h-[75vh]! max-h-[92vh]! flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {view === "preview" && (
              <button
                onClick={() => setView("picker")}
                className="text-muted-foreground hover:text-foreground"
                disabled={isPending}
              >
                <ArrowLeft className="h-4 w-4" />
              </button>
            )}
            {titles[view]}
          </DialogTitle>
          <DialogDescription>{descriptions[view]}</DialogDescription>
        </DialogHeader>

        {view === "picker" && <FilePicker onParsed={handleParsed} />}

        {view === "preview" && payload && (
          <div className="flex min-h-0 flex-1 flex-col gap-4">
            <div className="min-h-0 flex-1 overflow-y-auto pr-1">
              <InvoicePreview payload={payload} />
            </div>
            <DialogFooter className="shrink-0 border-t pt-4">
              <Button
                variant="outline"
                onClick={() => setView("picker")}
                disabled={isPending}
              >
                Back
              </Button>
              <Button onClick={handleConfirmImport} disabled={isPending}>
                {isPending ? (
                  <>
                    <Spinner className="mr-2" />
                    Importing…
                  </>
                ) : (
                  `Import ${payload.invoices.length} Invoices`
                )}
              </Button>
            </DialogFooter>
          </div>
        )}

        {view === "result" && batchResult && (
          <BatchResultView
            result={batchResult}
            onClose={() => {
              closeAndReset();
              setView("picker");
              setPayload(null);
            }}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
