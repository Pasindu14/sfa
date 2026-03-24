'use client'

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { useSalesInvoiceDetail } from '../../hooks/sales-invoice-list.hooks'
import type { SalesInvoiceStatus, SalesInvoiceType } from '../../schema/sales-invoice-list.schema'

// ── Badge helpers ──────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: SalesInvoiceStatus }) {
  if (status === 'GrnReceived') {
    return <Badge variant="default" className="bg-green-600 hover:bg-green-700">GRN Received</Badge>
  }
  if (status === 'Disputed') {
    return <Badge variant="destructive">Disputed</Badge>
  }
  return <Badge variant="outline">Pending</Badge>
}

function InvoiceTypeBadge({ type }: { type: SalesInvoiceType }) {
  if (type === 'FreeIssue') {
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white">Free Issue</Badge>
  }
  return <Badge variant="secondary">Regular</Badge>
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', minimumFractionDigits: 2 }).format(amount)
}

// ── Props ──────────────────────────────────────────────────────────────────

interface SalesInvoiceDetailDrawerProps {
  invoiceId: number | null
  open: boolean
  onClose: () => void
}

// ── Component ─────────────────────────────────────────────────────────────

export function SalesInvoiceDetailDrawer({ invoiceId, open, onClose }: SalesInvoiceDetailDrawerProps) {
  const { data: invoice, isLoading } = useSalesInvoiceDetail(open ? invoiceId : null)

  return (
    <Sheet open={open} onOpenChange={(v) => { if (!v) onClose() }}>
      <SheetContent side="right" className="w-full sm:max-w-2xl p-0 flex flex-col">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <SheetTitle>
            {isLoading ? (
              <Skeleton className="h-6 w-48" />
            ) : invoice ? (
              <>Invoice #{invoice.vchBillNo}</>
            ) : (
              'Invoice Detail'
            )}
          </SheetTitle>
          {invoice && (
            <SheetDescription asChild>
              <div className="flex items-center gap-2 flex-wrap">
                <StatusBadge status={invoice.status} />
                <InvoiceTypeBadge type={invoice.invoiceType} />
                <span className="text-xs text-muted-foreground">{invoice.distributorName}</span>
              </div>
            </SheetDescription>
          )}
        </SheetHeader>

        <ScrollArea className="flex-1 px-6 py-4">
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="h-5 w-full" />
              ))}
            </div>
          ) : invoice ? (
            <div className="space-y-6">
              {/* Header info */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-muted-foreground text-xs uppercase tracking-wider mb-1">Invoice Date</p>
                  <p className="font-medium">{new Date(invoice.invoiceDate).toLocaleDateString()}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-xs uppercase tracking-wider mb-1">Batch Number</p>
                  <p className="font-medium">{invoice.batchNumber}</p>
                </div>
                <div>
                  <p className="text-muted-foreground text-xs uppercase tracking-wider mb-1">Total Amount</p>
                  <p className="font-semibold text-base">{formatCurrency(invoice.totalAmount)}</p>
                </div>
                {invoice.sfaPoNumber && (
                  <div>
                    <p className="text-muted-foreground text-xs uppercase tracking-wider mb-1">SFA PO Number</p>
                    <p className="font-medium">{invoice.sfaPoNumber}</p>
                  </div>
                )}
                {invoice.busyOrderRequestNo && (
                  <div>
                    <p className="text-muted-foreground text-xs uppercase tracking-wider mb-1">Busy Order Req No</p>
                    <p className="font-medium">{invoice.busyOrderRequestNo}</p>
                  </div>
                )}
              </div>

              {/* Items table */}
              <div>
                <h3 className="text-sm font-semibold mb-3">Line Items ({invoice.items.length})</h3>
                <div className="rounded-md border overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-muted/50">
                      <tr>
                        <th className="text-left px-3 py-2 font-medium text-muted-foreground">#</th>
                        <th className="text-left px-3 py-2 font-medium text-muted-foreground">Description</th>
                        <th className="text-left px-3 py-2 font-medium text-muted-foreground">Code</th>
                        <th className="text-right px-3 py-2 font-medium text-muted-foreground">Qty</th>
                        <th className="text-right px-3 py-2 font-medium text-muted-foreground">Unit Price</th>
                        <th className="text-right px-3 py-2 font-medium text-muted-foreground">Total</th>
                        <th className="text-center px-3 py-2 font-medium text-muted-foreground">Free</th>
                      </tr>
                    </thead>
                    <tbody>
                      {invoice.items.map((item) => (
                        <tr key={item.id} className="border-t hover:bg-muted/30 transition-colors">
                          <td className="px-3 py-2 text-muted-foreground">{item.lineNumber}</td>
                          <td className="px-3 py-2">{item.itemDescription}</td>
                          <td className="px-3 py-2 text-muted-foreground text-xs">{item.itemErpCode}</td>
                          <td className="px-3 py-2 text-right">{item.quantity} {item.unit}</td>
                          <td className="px-3 py-2 text-right">{formatCurrency(item.unitPrice)}</td>
                          <td className="px-3 py-2 text-right font-medium">{formatCurrency(item.totalPrice)}</td>
                          <td className="px-3 py-2 text-center">
                            {item.isFreeIssue ? (
                              <Badge className="bg-amber-100 text-amber-800 text-xs">Yes</Badge>
                            ) : (
                              <span className="text-muted-foreground text-xs">—</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot className="border-t bg-muted/30">
                      <tr>
                        <td colSpan={5} className="px-3 py-2 text-right font-semibold text-sm">Total</td>
                        <td className="px-3 py-2 text-right font-bold">{formatCurrency(invoice.totalAmount)}</td>
                        <td />
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground text-sm">Invoice not found.</p>
          )}
        </ScrollArea>
      </SheetContent>
    </Sheet>
  )
}
