'use client'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Banknote, Calendar, ChevronRight, Store, User } from 'lucide-react'
import { formatColombo } from '@/lib/utils/datetime'
import { useRepBillDetail } from '../../hooks/rep-bill.hooks'
import { useRepBillDetailDialog } from '../../store'
import {
  DistributorStatusBadge,
  PaymentTypeBadge,
  RepStatusBadge,
  formatCurrency,
} from '../columns/rep-bill-columns'
import type { RepBillDetail, RepBillLineItem } from '../../schema/rep-bill.schema'

function ItemTypeBadge({ type }: { type: RepBillLineItem['billingItemType'] }) {
  if (type === 'FreeIssue')
    return (
      <Badge className="bg-amber-500 px-1.5 py-0 text-[10px] text-white hover:bg-amber-600">
        Free
      </Badge>
    )
  if (type === 'Return')
    return <Badge variant="destructive" className="px-1.5 py-0 text-[10px]">Return</Badge>
  return <Badge variant="secondary" className="px-1.5 py-0 text-[10px]">Sale</Badge>
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
      <div
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-md ${iconBg} ${iconColor}`}
      >
        <Icon className="h-4 w-4" />
      </div>
      <div className="min-w-0">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="truncate text-sm font-semibold leading-tight">{value}</p>
      </div>
    </div>
  )
}

/**
 * The reporting line as it stood *when the bill was written* — these IDs are denormalised onto
 * the bill row at write time, so they keep pointing at the right people even after someone is
 * later moved under a different manager. Staff-only: the portal detail payload has no org chain.
 */
function OrgChain({ bill }: { bill: RepBillDetail }) {
  const links = [
    bill.supervisorName && { role: 'Supervisor', name: bill.supervisorName },
    bill.asmName && { role: 'ASM', name: bill.asmName },
    bill.rsmName && { role: 'RSM', name: bill.rsmName },
    bill.nsmName && { role: 'NSM', name: bill.nsmName },
  ].filter(Boolean) as { role: string; name: string }[]

  if (links.length === 0) return null

  return (
    <div className="shrink-0 px-4 pt-3 sm:px-6">
      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        Reporting line at time of billing
      </p>
      <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
        {links.map((link, i) => (
          <div key={link.role} className="flex items-center gap-1.5">
            {i > 0 && <ChevronRight className="h-3 w-3 shrink-0 text-muted-foreground/50" />}
            <span className="rounded bg-muted px-1 py-px text-[10px] font-bold uppercase leading-none tracking-wider text-muted-foreground">
              {link.role}
            </span>
            <span className="text-xs leading-none">{link.name}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

function LoadingSkeleton() {
  return (
    <div className="flex-1 space-y-3 px-4 py-4 sm:px-6">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-[62px]" />
        ))}
      </div>
      <Skeleton className="h-56 w-full" />
    </div>
  )
}

export function RepBillDetailDialog() {
  const { isOpen, selectedId, close } = useRepBillDetailDialog()
  // Gated on `isOpen` rather than `selectedId` so closing the dialog does not immediately fire
  // a fetch for a stale id, and so re-opening the same bill serves from cache.
  const { data: bill, isLoading } = useRepBillDetail(isOpen ? selectedId : null)

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(v) => {
        if (!v) close()
      }}
    >
      <DialogContent className="flex h-[92dvh] w-[95vw] flex-col gap-0 overflow-hidden p-0 sm:h-[90vh]! sm:w-[90vw]! sm:max-w-5xl!">
        <DialogHeader className="shrink-0 border-b px-4 pb-4 pt-5 sm:px-6">
          <DialogTitle className="flex items-center gap-2 text-base">
            {isLoading ? (
              <Skeleton className="h-5 w-44" />
            ) : bill ? (
              <>
                Bill <span className="font-mono">{bill.billingNumber}</span>
              </>
            ) : (
              'Bill Detail'
            )}
          </DialogTitle>
          {bill && (
            <DialogDescription asChild>
              <div className="flex flex-wrap items-center gap-2 pt-0.5">
                <RepStatusBadge status={bill.repStatus} />
                <DistributorStatusBadge status={bill.distributorStatus} />
                <PaymentTypeBadge type={bill.paymentType} />
                <span className="text-xs text-muted-foreground">{bill.outletName}</span>
              </div>
            </DialogDescription>
          )}
        </DialogHeader>

        {isLoading ? (
          <LoadingSkeleton />
        ) : bill ? (
          <div className="flex min-h-0 flex-1 flex-col">
            <div className="grid shrink-0 grid-cols-2 gap-2 px-4 py-3 sm:grid-cols-4 sm:gap-3 sm:px-6 sm:py-4">
              <StatCard
                icon={Calendar}
                iconBg="bg-blue-100"
                iconColor="text-blue-600"
                label="Billing Date"
                value={formatColombo(bill.billingDate, 'd MMM yyyy')}
              />
              <StatCard
                icon={Banknote}
                iconBg="bg-green-100"
                iconColor="text-green-600"
                label="Total Amount"
                value={formatCurrency(bill.totalAmount)}
              />
              <StatCard
                icon={Store}
                iconBg="bg-violet-100"
                iconColor="text-violet-600"
                label="Outlet"
                value={bill.outletName}
              />
              <StatCard
                icon={User}
                iconBg="bg-slate-100"
                iconColor="text-slate-600"
                label="Sales Rep"
                value={bill.salesRepName}
              />
            </div>

            <Separator />

            <OrgChain bill={bill} />

            {bill.distributorStatus === 'Rejected' && bill.rejectionReason && (
              <div className="shrink-0 px-4 pt-3 sm:px-6">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Rejection reason
                </p>
                <p className="mt-1 whitespace-pre-wrap text-sm text-destructive">
                  {bill.rejectionReason}
                </p>
              </div>
            )}

            {bill.notes && (
              <div className="shrink-0 px-4 pt-3 sm:px-6">
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Remarks
                </p>
                <p className="mt-1 whitespace-pre-wrap text-sm">{bill.notes}</p>
              </div>
            )}

            <div className="shrink-0 px-4 pb-1 pt-3 sm:px-6">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Line Items ({bill.items.length})
              </p>
            </div>

            <ScrollArea className="min-h-0 flex-1 pb-3">
              <div className="px-4 sm:px-6">
                <div className="overflow-x-auto rounded-lg border">
                  <table className="w-full min-w-[480px] border-collapse text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="w-20 border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Type</th>
                        <th className="border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Product</th>
                        <th className="w-20 border-r px-3 py-2 text-left text-xs font-medium text-muted-foreground">Code</th>
                        <th className="w-16 border-r px-3 py-2 text-right text-xs font-medium text-muted-foreground">Qty</th>
                        <th className="w-24 border-r px-3 py-2 text-right text-xs font-medium text-muted-foreground">Unit Price</th>
                        <th className="w-24 px-3 py-2 text-right text-xs font-medium text-muted-foreground">Total</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {bill.items.map((item) => {
                        const rowBg =
                          item.billingItemType === 'FreeIssue'
                            ? 'bg-amber-50/50'
                            : item.billingItemType === 'Return'
                              ? 'bg-red-50/50'
                              : ''
                        return (
                          <tr
                            key={item.id}
                            className={`transition-colors hover:bg-muted/30 ${rowBg}`}
                          >
                            <td className="border-r px-3 py-2.5 align-top">
                              <ItemTypeBadge type={item.billingItemType} />
                            </td>
                            <td className="max-w-[200px] border-r px-3 py-2.5">
                              <p className="truncate font-medium" title={item.productDescription}>
                                {item.productDescription}
                              </p>
                              <div className="flex flex-wrap gap-x-2 text-[10px] text-muted-foreground">
                                {item.discountRate > 0 && <span>{item.discountRate}% disc</span>}
                                {item.returnType && <span>Return: {item.returnType}</span>}
                                {item.freeIssueSource && (
                                  <span>Free by: {item.freeIssueSource}</span>
                                )}
                              </div>
                            </td>
                            <td className="whitespace-nowrap border-r px-3 py-2.5 font-mono text-xs text-muted-foreground">
                              {item.productCode}
                            </td>
                            <td className="border-r px-3 py-2.5 text-right tabular-nums">
                              {item.quantity}
                            </td>
                            <td className="border-r px-3 py-2.5 text-right tabular-nums">
                              {formatCurrency(item.unitPrice)}
                            </td>
                            <td className="px-3 py-2.5 text-right font-semibold tabular-nums">
                              {formatCurrency(item.totalPrice)}
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            </ScrollArea>

            <div className="shrink-0 space-y-1 border-t bg-muted/30 px-4 py-3 sm:px-6">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Subtotal</span>
                <span className="tabular-nums">{formatCurrency(bill.subTotalAmount)}</span>
              </div>
              {bill.itemWiseTotalDiscount > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Item Discounts</span>
                  <span className="tabular-nums text-red-500">
                    − {formatCurrency(bill.itemWiseTotalDiscount)}
                  </span>
                </div>
              )}
              {bill.billDiscountAmount > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Bill Discount ({bill.billDiscountRate}%)</span>
                  <span className="tabular-nums text-red-500">
                    − {formatCurrency(bill.billDiscountAmount)}
                  </span>
                </div>
              )}
              {bill.returnValue > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Returns</span>
                  <span className="tabular-nums text-red-500">
                    − {formatCurrency(bill.returnValue)}
                  </span>
                </div>
              )}
              {bill.freeIssueValue > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Free Issue Value</span>
                  <span className="tabular-nums text-amber-600">
                    {formatCurrency(bill.freeIssueValue)}
                  </span>
                </div>
              )}
              <Separator className="my-1" />
              <div className="flex justify-between text-sm font-bold">
                <span>Total</span>
                <span className="tabular-nums">{formatCurrency(bill.totalAmount)}</span>
              </div>
            </div>
          </div>
        ) : (
          <p className="px-6 py-10 text-sm text-muted-foreground">Bill not found.</p>
        )}
      </DialogContent>
    </Dialog>
  )
}
