"use client";

import { useState } from "react";
import {
  ChevronDown,
  ChevronRight,
  Package,
  FileText,
  Banknote,
  Tag,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type {
  ImportSalesInvoicesPayload,
  ImportInvoicePayload,
} from "../../schema/sales-invoice.schema";

// ── Helpers ───────────────────────────────────────────────────────────────

function fmtAmount(n: number) {
  return n.toLocaleString("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function fmtDate(iso: string) {
  const [y, m, d] = iso.split("-");
  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];
  return `${d} ${months[Number(m) - 1]} ${y}`;
}

// ── Summary cards ─────────────────────────────────────────────────────────

function SummaryCards({ payload }: { payload: ImportSalesInvoicesPayload }) {
  const totalItems = payload.invoices.reduce((s, i) => s + i.items.length, 0);
  const totalAmount = payload.invoices.reduce((s, i) => s + i.totalAmount, 0);
  const freeIssueCount = payload.invoices.filter(
    (i) => i.invoiceType === "FreeIssue",
  ).length;

  return (
    <div className="grid grid-cols-4 gap-3">
      <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-md bg-blue-100 text-blue-600">
          <FileText className="h-4 w-4" />
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Invoices</p>
          <p className="text-xl font-bold">{payload.invoices.length}</p>
        </div>
      </div>
      <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-md bg-violet-100 text-violet-600">
          <Package className="h-4 w-4" />
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Line Items</p>
          <p className="text-xl font-bold">{totalItems}</p>
        </div>
      </div>
      <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-md bg-green-100 text-green-600">
          <Banknote className="h-4 w-4" />
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Total Amount</p>
          <p className="text-lg font-bold leading-tight">
            {fmtAmount(totalAmount)}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-md bg-amber-100 text-amber-600">
          <Tag className="h-4 w-4" />
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Free Issue</p>
          <p className="text-xl font-bold">{freeIssueCount}</p>
        </div>
      </div>
    </div>
  );
}

// ── Single expandable invoice row ─────────────────────────────────────────

function InvoiceRow({
  invoice,
}: {
  invoice: ImportInvoicePayload;
  index: number;
}) {
  const [open, setOpen] = useState(false);
  const isFree = invoice.invoiceType === "FreeIssue";

  return (
    <div className="border-b last:border-0">
      {/* Header row */}
      <button
        onClick={() => setOpen((o) => !o)}
        className="flex w-full items-center gap-3 px-3 py-2.5 text-left hover:bg-muted/50 transition-colors"
      >
        <span className="text-muted-foreground w-4 shrink-0">
          {open ? (
            <ChevronDown className="h-3.5 w-3.5" />
          ) : (
            <ChevronRight className="h-3.5 w-3.5" />
          )}
        </span>

        {/* VchBillNo */}
        <span
          className="w-36 shrink-0 font-mono text-sm font-medium truncate"
          title={invoice.vchBillNo}
        >
          {invoice.vchBillNo}
        </span>

        {/* Date */}
        <span className="w-28 shrink-0 text-xs text-muted-foreground">
          {fmtDate(invoice.invoiceDate)}
        </span>

        {/* Distributor alias */}
        <span className="w-20 shrink-0 text-xs text-muted-foreground">
          #{invoice.distributorAlias}
        </span>

        {/* Type badge */}
        <span className="w-24 shrink-0">
          <Badge
            variant={isFree ? "secondary" : "outline"}
            className={cn(
              "text-xs",
              isFree && "border-amber-300 bg-amber-50 text-amber-700",
            )}
          >
            {isFree ? "Free Issue" : "Regular"}
          </Badge>
        </span>

        {/* SFA PO */}
        <span
          className="flex-1 truncate text-xs text-muted-foreground"
          title={invoice.sfaPoNumber ?? ""}
        >
          {invoice.sfaPoNumber ?? (
            <span className="italic opacity-50">no PO</span>
          )}
        </span>

        {/* Items count */}
        <span className="w-16 shrink-0 text-right text-xs text-muted-foreground">
          {invoice.items.length} item{invoice.items.length !== 1 ? "s" : ""}
        </span>

        {/* Amount */}
        <span className="w-28 shrink-0 text-right text-sm font-semibold tabular-nums">
          {fmtAmount(invoice.totalAmount)}
        </span>
      </button>

      {/* Expanded items */}
      {open && (
        <div className="border-t bg-muted/30 px-6 pb-3 pt-2">
          <table className="w-full text-xs">
            <thead>
              <tr className="text-muted-foreground">
                <th className="pb-1.5 pr-3 text-left font-medium">#</th>
                <th className="pb-1.5 pr-3 text-left font-medium">Code</th>
                <th className="pb-1.5 pr-3 text-left font-medium">
                  Description
                </th>
                <th className="pb-1.5 pr-3 text-right font-medium">Qty</th>
                <th className="pb-1.5 pr-3 text-left font-medium">Unit</th>
                <th className="pb-1.5 pr-3 text-right font-medium">Rate</th>
                <th className="pb-1.5 text-right font-medium">Amount</th>
              </tr>
            </thead>
            <tbody>
              {invoice.items.map((item) => (
                <tr
                  key={item.lineNumber}
                  className={cn(
                    "border-t border-border/50",
                    item.isFreeIssue && "bg-amber-50/60",
                  )}
                >
                  <td className="py-1 pr-3 text-muted-foreground">
                    {item.lineNumber}
                  </td>
                  <td className="py-1 pr-3 font-mono font-medium">
                    {item.itemErpCode}
                  </td>
                  <td
                    className="py-1 pr-3 text-muted-foreground max-w-[240px] truncate"
                    title={item.itemDescription}
                  >
                    {item.itemDescription}
                    {item.isFreeIssue && (
                      <span className="ml-1.5 rounded bg-amber-100 px-1 text-[10px] text-amber-700">
                        FREE
                      </span>
                    )}
                  </td>
                  <td className="py-1 pr-3 text-right tabular-nums">
                    {item.quantity}
                  </td>
                  <td className="py-1 pr-3 text-muted-foreground">
                    {item.unit}
                  </td>
                  <td className="py-1 pr-3 text-right tabular-nums">
                    {fmtAmount(item.unitPrice)}
                  </td>
                  <td className="py-1 text-right font-medium tabular-nums">
                    {fmtAmount(item.totalPrice)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ── Main preview ──────────────────────────────────────────────────────────

export function InvoicePreview({
  payload,
}: {
  payload: ImportSalesInvoicesPayload;
}) {
  return (
    <div className="space-y-3">
      <SummaryCards payload={payload} />

      <div className="rounded-lg border text-sm">
        {/* Table header */}
        <div className="flex items-center gap-3 border-b bg-muted/50 px-3 py-2 text-xs font-medium text-muted-foreground">
          <span className="w-4 shrink-0" />
          <span className="w-36 shrink-0">Voucher No</span>
          <span className="w-28 shrink-0">Date</span>
          <span className="w-20 shrink-0">Alias</span>
          <span className="w-24 shrink-0">Type</span>
          <span className="flex-1">SFA PO</span>
          <span className="w-16 shrink-0 text-right">Items</span>
          <span className="w-28 shrink-0 text-right">Amount</span>
        </div>

        <div>
          {payload.invoices.map((inv, i) => (
            <InvoiceRow key={`${i}-${inv.vchBillNo}`} invoice={inv} index={i} />
          ))}
        </div>
      </div>

      <p className="text-xs text-muted-foreground">
        File: <span className="font-medium">{payload.fileName}</span>
        {" · "}Click any row to expand its line items.
      </p>
    </div>
  );
}
