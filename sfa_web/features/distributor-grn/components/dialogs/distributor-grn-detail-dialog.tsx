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
import { Calendar, ClipboardCheck, FileText, User } from 'lucide-react'
import { useMyGrnDetail } from '../../hooks/distributor-grn.hooks'
import type { MyGrnListItem } from '../../schema/distributor-grn.schema'
import { formatColombo } from '@/lib/utils/datetime'

function GrnStatusBadge({ status }: { status: MyGrnListItem['status'] }) {
  if (status === 'Confirmed')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Confirmed</Badge>
  if (status === 'Disputed')
    return <Badge variant="destructive" className="text-xs">Disputed</Badge>
  return <Badge variant="outline" className="text-xs">Pending</Badge>
}

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

function LoadingSkeleton() {
  return (
    <div className="space-y-5 px-6 py-5">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
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

function formatDate(dateStr: string | null) {
  return formatColombo(dateStr, 'd MMM yyyy')
}

interface Props {
  id: number | null
  onClose: () => void
}

export function DistributorGrnDetailDialog({ id, onClose }: Props) {
  const { data: grn, isLoading } = useMyGrnDetail(id)

  return (
    <Dialog open={id !== null} onOpenChange={(v) => { if (!v) onClose() }}>
      <DialogContent className="w-[95vw] sm:w-[90vw]! sm:max-w-4xl! h-[92dvh] sm:h-[88vh]! flex flex-col gap-0 p-0 overflow-hidden">

        {/* Header */}
        <DialogHeader className="shrink-0 border-b px-4 sm:px-6 pb-4 pt-5">
          <DialogTitle className="flex items-center gap-2 text-base">
            {isLoading ? (
              <Skeleton className="h-5 w-44" />
            ) : grn ? (
              <>GRN <span className="font-mono">{grn.grnNumber}</span></>
            ) : (
              'GRN Detail'
            )}
          </DialogTitle>
          {grn && (
            <DialogDescription asChild>
              <div className="flex flex-wrap items-center gap-2 pt-0.5">
                <GrnStatusBadge status={grn.status} />
                <span className="font-mono text-xs text-muted-foreground">
                  Invoice: {grn.salesInvoiceVchBillNo}
                </span>
              </div>
            </DialogDescription>
          )}
        </DialogHeader>

        {/* Body */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : grn ? (
          <div className="flex min-h-0 flex-1 flex-col">

            {/* Stat cards */}
            <div className="grid shrink-0 grid-cols-2 gap-2 px-4 py-3 sm:gap-3 sm:px-6 sm:py-4 sm:grid-cols-4">
              <StatCard
                icon={FileText}
                iconBg="bg-blue-100"
                iconColor="text-blue-600"
                label="Invoice No"
                value={<span className="font-mono">{grn.salesInvoiceVchBillNo}</span>}
              />
              <StatCard
                icon={Calendar}
                iconBg="bg-green-100"
                iconColor="text-green-600"
                label="Received At"
                value={formatDate(grn.receivedAt)}
              />
              <StatCard
                icon={User}
                iconBg="bg-violet-100"
                iconColor="text-violet-600"
                label="Confirmed By"
                value={grn.confirmedByName ?? '—'}
              />
              <StatCard
                icon={ClipboardCheck}
                iconBg="bg-slate-100"
                iconColor="text-slate-600"
                label="Created"
                value={formatDate(grn.createdAt)}
              />
            </div>

            {grn.notes && (
              <div className="shrink-0 px-4 sm:px-6 pb-2">
                <p className="text-xs text-muted-foreground">
                  <span className="font-medium">Notes:</span> {grn.notes}
                </p>
              </div>
            )}

            <Separator />

            {/* Items label */}
            <div className="shrink-0 px-4 sm:px-6 pb-1 pt-3">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Stock Items ({grn.items.length})
              </p>
            </div>

            {/* Scrollable items table */}
            <ScrollArea className="min-h-0 flex-1 pb-3">
              <div className="px-4 sm:px-6">
                <div className="overflow-x-auto rounded-lg border">
                  <table className="w-full min-w-[480px] border-collapse text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Product</th>
                        <th className="border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Code</th>
                        <th className="border-r px-3 py-2 text-right text-xs font-medium text-muted-foreground">Quantity</th>
                        <th className="border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Unit</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-muted-foreground">Notes</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {grn.items.map((item) => (
                        <tr key={item.id} className="transition-colors hover:bg-muted/30">
                          <td className="max-w-[220px] border-r px-3 py-2.5">
                            <p className="truncate font-medium" title={item.productName}>
                              {item.productName}
                            </p>
                          </td>
                          <td className="border-r px-3 py-2.5 font-mono text-xs text-muted-foreground whitespace-nowrap">
                            {item.productCode}
                          </td>
                          <td className="border-r px-3 py-2.5 text-right tabular-nums">
                            {item.quantity}
                          </td>
                          <td className="border-r px-3 py-2.5 text-muted-foreground whitespace-nowrap">
                            {item.unit}
                          </td>
                          <td className="px-3 py-2.5 text-xs text-muted-foreground">
                            {item.notes ?? '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </ScrollArea>

            {/* Footer: total item count */}
            <div className="shrink-0 border-t bg-muted/30 px-4 sm:px-6 py-3">
              <div className="flex justify-between text-sm font-semibold">
                <span>Total Items</span>
                <span className="tabular-nums">{grn.items.length}</span>
              </div>
            </div>

          </div>
        ) : (
          <p className="px-6 py-10 text-sm text-muted-foreground">GRN not found.</p>
        )}

      </DialogContent>
    </Dialog>
  )
}
