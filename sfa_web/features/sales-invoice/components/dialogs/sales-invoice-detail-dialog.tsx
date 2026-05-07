'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Calendar, Banknote, Package, Hash, FileText, Tag } from 'lucide-react'
import { useDetailDialog } from '../../store'
import { useSalesInvoiceDetail } from '../../hooks/sales-invoice.hooks'
import type { SalesInvoiceStatus, SalesInvoiceType } from '../../schema/sales-invoice-list.schema'

// ── Helpers ────────────────────────────────────────────────────────────────

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

// ── Badges ─────────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: SalesInvoiceStatus }) {
  if (status === 'GrnReceived')
    return <Badge className="bg-green-600 hover:bg-green-700">GRN Received</Badge>
  if (status === 'Disputed')
    return <Badge variant="destructive">Disputed</Badge>
  return <Badge variant="outline">Pending</Badge>
}

function InvoiceTypeBadge({ type }: { type: SalesInvoiceType }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white">Free Issue</Badge>
  return <Badge variant="secondary">Regular</Badge>
}

// ── Skeleton loading ───────────────────────────────────────────────────────

function LoadingSkeleton() {
  return (
    <div className="space-y-5 px-6 py-5">
      <div className="grid grid-cols-4 gap-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-[4.5rem] rounded-lg" />
        ))}
      </div>
      <Skeleton className="h-px w-full" />
      <div className="space-y-2">
        <Skeleton className="h-4 w-28" />
        <Skeleton className="h-48 w-full rounded-lg" />
      </div>
    </div>
  )
}

// ── Stat card ──────────────────────────────────────────────────────────────

function StatCard({
  icon: Icon,
  iconBg,
  iconColor,
  label,
  value,
}: {
  icon: React.ElementType
  iconBg: string
  iconColor: string
  label: string
  value: React.ReactNode
}) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-card p-3">
      <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-md ${iconBg} ${iconColor}`}>
        <Icon className="h-4 w-4" />
      </div>
      <div className="min-w-0">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="truncate text-sm font-semibold leading-tight">{value}</p>
      </div>
    </div>
  )
}

// ── Main dialog ────────────────────────────────────────────────────────────

export function SalesInvoiceDetailDialog() {
  const { isOpen, selectedId, close } = useDetailDialog()
  const { data: invoice, isLoading } = useSalesInvoiceDetail(isOpen ? selectedId : null)

  return (
    <Dialog open={isOpen} onOpenChange={(v) => { if (!v) close() }}>
      <DialogContent className="w-[90vw]! max-w-5xl! h-[90vh]! flex flex-col gap-0 p-0 overflow-hidden">

        {/* Header */}
        <DialogHeader className="shrink-0 border-b px-6 pb-4 pt-5">
          <DialogTitle className="flex items-center gap-2 text-base">
            <FileText className="h-4 w-4 shrink-0 text-muted-foreground" />
            {isLoading ? (
              <Skeleton className="h-5 w-44" />
            ) : invoice ? (
              <>Invoice <span className="font-mono">{invoice.vchBillNo}</span></>
            ) : (
              'Invoice Detail'
            )}
          </DialogTitle>
          {invoice && (
            <DialogDescription asChild>
              <div className="flex flex-wrap items-center gap-2 pt-0.5">
                <StatusBadge status={invoice.status} />
                <InvoiceTypeBadge type={invoice.hasFreeIssueItems ? 'FreeIssue' : invoice.invoiceType} />
                <span className="text-xs text-muted-foreground">{invoice.distributorName}</span>
              </div>
            </DialogDescription>
          )}
        </DialogHeader>

        {/* Body */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : invoice ? (
          <div className="flex min-h-0 flex-1 flex-col">

            {/* Stat cards */}
            <div className="grid shrink-0 grid-cols-4 gap-3 px-6 py-4">
              <StatCard
                icon={Calendar}
                iconBg="bg-blue-100"
                iconColor="text-blue-600"
                label="Invoice Date"
                value={formatDate(invoice.invoiceDate)}
              />
              <StatCard
                icon={Banknote}
                iconBg="bg-green-100"
                iconColor="text-green-600"
                label="Total Amount"
                value={formatCurrency(invoice.totalAmount)}
              />
              <StatCard
                icon={Package}
                iconBg="bg-violet-100"
                iconColor="text-violet-600"
                label="Line Items"
                value={`${invoice.items.length} item${invoice.items.length !== 1 ? 's' : ''}`}
              />
              <StatCard
                icon={Hash}
                iconBg="bg-slate-100"
                iconColor="text-slate-600"
                label="Batch"
                value={invoice.batchNumber}
              />
            </div>

            {/* Optional PO / Busy order ref */}
            {(invoice.sfaPoNumber || invoice.busyOrderRequestNo) && (
              <>
                <Separator />
                <div className="flex shrink-0 flex-wrap items-center gap-6 px-6 py-2.5 text-sm">
                  {invoice.sfaPoNumber && (
                    <div className="flex items-center gap-1.5">
                      <Tag className="h-3.5 w-3.5 text-muted-foreground" />
                      <span className="text-xs text-muted-foreground">SFA PO</span>
                      <span className="font-mono text-xs font-medium">{invoice.sfaPoNumber}</span>
                    </div>
                  )}
                  {invoice.busyOrderRequestNo && (
                    <div className="flex items-center gap-1.5">
                      <Hash className="h-3.5 w-3.5 text-muted-foreground" />
                      <span className="text-xs text-muted-foreground">Busy Order Req</span>
                      <span className="font-mono text-xs font-medium">{invoice.busyOrderRequestNo}</span>
                    </div>
                  )}
                </div>
              </>
            )}

            <Separator />

            {/* Line items label */}
            <div className="shrink-0 px-6 pb-1 pt-3">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Line Items ({invoice.items.length})
              </p>
            </div>

            {/* Scrollable table */}
            <ScrollArea className="min-h-0 flex-1 px-6 pb-5">
              <div className="overflow-hidden rounded-lg border">

                {/* Column header row */}
                <div className="grid grid-cols-[2.5rem_1fr_8rem_4.5rem_4rem_7rem_7rem] gap-x-3 border-b bg-muted/50 px-3 py-2 text-xs font-medium text-muted-foreground">
                  <span>#</span>
                  <span>Description</span>
                  <span>Code</span>
                  <span className="text-right">Qty</span>
                  <span>Unit</span>
                  <span className="text-right">Unit Price</span>
                  <span className="text-right">Total</span>
                </div>

                {/* Data rows */}
                <div className="divide-y">
                  {invoice.items.map((item) => {
                    const isFreeRow = invoice.hasFreeIssueItems || item.isFreeIssue
                    return (
                    <div
                      key={item.id}
                      className={`grid grid-cols-[2.5rem_1fr_8rem_4.5rem_4rem_7rem_7rem] items-start gap-x-3 px-3 py-2.5 transition-colors hover:bg-muted/30 ${isFreeRow ? 'bg-amber-50/50' : ''}`}
                    >
                      <span className="pt-0.5 text-xs tabular-nums text-muted-foreground/60">
                        {item.lineNumber}
                      </span>
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium" title={item.itemDescription}>
                          {item.itemDescription}
                        </p>
                        {isFreeRow && (
                          <span className="mt-0.5 inline-block rounded bg-amber-100 px-1 text-[10px] font-medium text-amber-700">
                            FREE
                          </span>
                        )}
                      </div>
                      <span className="pt-0.5 font-mono text-xs text-muted-foreground">
                        {item.itemErpCode}
                      </span>
                      <span className="pt-0.5 text-right tabular-nums text-sm">{item.quantity}</span>
                      <span className="pt-0.5 text-xs text-muted-foreground">{item.unit}</span>
                      <span className="pt-0.5 text-right tabular-nums text-sm">
                        {formatCurrency(item.unitPrice)}
                      </span>
                      <span className="pt-0.5 text-right tabular-nums text-sm font-semibold">
                        {formatCurrency(item.totalPrice)}
                      </span>
                    </div>
                  )})}
                </div>

                {/* Total footer */}
                <div className="grid grid-cols-[2.5rem_1fr_8rem_4.5rem_4rem_7rem_7rem] gap-x-3 border-t bg-muted/30 px-3 py-2.5">
                  <span />
                  <span className="col-span-5 text-right text-sm font-semibold text-muted-foreground">
                    Total
                  </span>
                  <span className="text-right tabular-nums text-sm font-bold">
                    {formatCurrency(invoice.totalAmount)}
                  </span>
                </div>
              </div>
            </ScrollArea>
          </div>
        ) : (
          <p className="px-6 py-10 text-sm text-muted-foreground">Invoice not found.</p>
        )}
      </DialogContent>
    </Dialog>
  )
}
