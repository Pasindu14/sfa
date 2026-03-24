"use client";

import { useRef, useState } from "react";
import { Upload, ArrowLeft } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Spinner } from "@/components/ui/spinner";
import { ScrollArea } from "@/components/ui/scroll-area";
import { parseExcelFile } from "../../lib/parse-excel";
import { InvoicePreview } from "./invoice-preview";
import { useImportDialog } from "../../store";
import { useImportSalesInvoices } from "../../hooks/sales-invoice.hooks";
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
  const statusVariant =
    result.status === "Completed"
      ? "default"
      : result.status === "PartialFailed"
        ? "secondary"
        : "destructive";

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <span className="text-sm font-medium text-muted-foreground">Batch</span>
        <code className="rounded bg-muted px-2 py-0.5 text-sm">
          {result.batchNumber}
        </code>
        <Badge variant={statusVariant}>{result.status}</Badge>
      </div>

      <div className="grid grid-cols-2 gap-3 rounded-lg border p-4 text-sm">
        <div>
          <p className="text-muted-foreground">Invoices Imported</p>
          <p className="text-2xl font-bold">
            {result.importedInvoices}
            <span className="text-base font-normal text-muted-foreground">
              {" "}
              / {result.totalInvoices}
            </span>
          </p>
        </div>
        <div>
          <p className="text-muted-foreground">Total Items</p>
          <p className="text-2xl font-bold">{result.totalItems}</p>
        </div>
        <div>
          <p className="text-muted-foreground">Skipped</p>
          <p className="text-2xl font-bold text-orange-500">
            {result.skippedInvoices}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground">Total Amount</p>
          <p className="text-lg font-bold">
            {result.totalAmount.toLocaleString("en-LK", {
              minimumFractionDigits: 2,
            })}
          </p>
        </div>
      </div>

      {result.errors.length > 0 && (
        <div className="space-y-1.5">
          <p className="text-sm font-medium text-destructive">
            {result.errors.length} skipped
          </p>
          <ScrollArea className="h-36 rounded-md border">
            <div className="space-y-1 p-3">
              {result.errors.map((err, i) => (
                <div key={i} className="flex items-start gap-2 text-sm">
                  <code className="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs font-medium">
                    {err.vchBillNo}
                  </code>
                  <span className="text-muted-foreground">{err.reason}</span>
                </div>
              ))}
            </div>
          </ScrollArea>
        </div>
      )}

      <DialogFooter>
        <Button onClick={onClose}>Done</Button>
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
    result: "The import batch has been processed.",
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
