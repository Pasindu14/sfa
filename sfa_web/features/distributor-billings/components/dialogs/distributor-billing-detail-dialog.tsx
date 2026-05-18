'use client'

import { useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Calendar, Banknote, Store, User, Loader2, CheckCircle2, XCircle } from 'lucide-react'
import { useMyBillingDetail, useApproveBilling, useRejectBilling } from '../../hooks/distributor-billing.hooks'
import type { DistributorBillingDetail } from '../../schema/distributor-billing.schema'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function RepStatusBadge({ status }: { status: DistributorBillingDetail['repStatus'] }) {
  if (status === 'Cancelled')
    return <Badge variant="destructive" className="text-xs">Cancelled</Badge>
  return <Badge variant="outline" className="text-xs">Submitted</Badge>
}

function DistributorStatusBadge({ status }: { status: DistributorBillingDetail['distributorStatus'] }) {
  if (status === 'Approved')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Approved</Badge>
  if (status === 'Rejected')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Rejected</Badge>
  return <Badge variant="secondary" className="text-xs">Pending</Badge>
}

function ItemTypeBadge({ type }: { type: 'Sale' | 'Return' | 'FreeIssue' }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-[10px] px-1.5 py-0">Free</Badge>
  if (type === 'Return')
    return <Badge variant="destructive" className="text-[10px] px-1.5 py-0">Return</Badge>
  return <Badge variant="secondary" className="text-[10px] px-1.5 py-0">Sale</Badge>
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

interface ActionsProps {
  billing: DistributorBillingDetail
  onClose: () => void
}

function BillingActionsPanel({ billing, onClose }: ActionsProps) {
  const [showApproveConfirm, setShowApproveConfirm] = useState(false)
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [rejectReason, setRejectReason] = useState('')

  const approveMutation = useApproveBilling(onClose)
  const rejectMutation = useRejectBilling(onClose)

  const canAct = billing.repStatus === 'Submitted' && billing.distributorStatus === 'Pending'
  if (!canAct) return null

  return (
    <>
      <div className="shrink-0 border-t bg-muted/20 px-6 py-3">
        {!showRejectForm ? (
          <div className="flex items-center justify-end gap-2">
            <Button
              size="sm"
              variant="outline"
              className="gap-1.5 border-amber-400 text-amber-700 hover:bg-amber-50"
              onClick={() => setShowRejectForm(true)}
              disabled={approveMutation.isPending || rejectMutation.isPending}
            >
              <XCircle className="h-3.5 w-3.5" />
              Reject
            </Button>
            <Button
              size="sm"
              className="gap-1.5 bg-green-600 hover:bg-green-700 text-white"
              onClick={() => setShowApproveConfirm(true)}
              disabled={approveMutation.isPending || rejectMutation.isPending}
            >
              <CheckCircle2 className="h-3.5 w-3.5" />
              Approve
            </Button>
          </div>
        ) : (
          <div className="flex items-end justify-end gap-3">
            <div className="w-72 space-y-2">
              <Textarea
                placeholder="Reason for rejection (optional)"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                rows={2}
                className="text-sm resize-none"
              />
              <div className="flex items-center justify-end gap-2">
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => { setShowRejectForm(false); setRejectReason('') }}
                  disabled={rejectMutation.isPending}
                >
                  Cancel
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  className="gap-1.5"
                  disabled={rejectMutation.isPending}
                  onClick={() => rejectMutation.mutate({ id: billing.id, reason: rejectReason || undefined })}
                >
                  {rejectMutation.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                  Confirm Reject
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>

      <AlertDialog open={showApproveConfirm} onOpenChange={setShowApproveConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Approve this billing?</AlertDialogTitle>
            <AlertDialogDescription>
              Billing <span className="font-mono font-semibold">{billing.billingNumber}</span> from {billing.salesRepName} will be marked as approved.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={approveMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-green-600 hover:bg-green-700"
              disabled={approveMutation.isPending}
              onClick={() => approveMutation.mutate(billing.id)}
            >
              {approveMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Approve
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}

interface Props {
  id: number | null
  onClose: () => void
}

export function DistributorBillingDetailDialog({ id, onClose }: Props) {
  const { data: billing, isLoading } = useMyBillingDetail(id)

  return (
    <Dialog open={id !== null} onOpenChange={(v) => { if (!v) onClose() }}>
      <DialogContent className="w-[90vw]! max-w-5xl! h-[90vh]! flex flex-col gap-0 p-0 overflow-hidden">

        {/* Header */}
        <DialogHeader className="shrink-0 border-b px-6 pb-4 pt-5">
          <DialogTitle className="flex items-center gap-2 text-base">
            {isLoading ? (
              <Skeleton className="h-5 w-44" />
            ) : billing ? (
              <>Bill <span className="font-mono">{billing.billingNumber}</span></>
            ) : (
              'Billing Detail'
            )}
          </DialogTitle>
          {billing && (
            <DialogDescription asChild>
              <div className="flex flex-wrap items-center gap-2 pt-0.5">
                <RepStatusBadge status={billing.repStatus} />
                <DistributorStatusBadge status={billing.distributorStatus} />
                <span className="text-xs text-muted-foreground">{billing.outletName}</span>
              </div>
            </DialogDescription>
          )}
        </DialogHeader>

        {/* Body */}
        {isLoading ? (
          <LoadingSkeleton />
        ) : billing ? (
          <div className="flex min-h-0 flex-1 flex-col">

            {/* Stat cards */}
            <div className="grid shrink-0 grid-cols-2 gap-3 px-6 py-4 sm:grid-cols-4">
              <StatCard
                icon={Calendar}
                iconBg="bg-blue-100"
                iconColor="text-blue-600"
                label="Billing Date"
                value={new Date(billing.billingDate).toLocaleDateString('en-US', {
                  day: 'numeric', month: 'short', year: 'numeric',
                })}
              />
              <StatCard
                icon={Banknote}
                iconBg="bg-green-100"
                iconColor="text-green-600"
                label="Total Amount"
                value={formatCurrency(billing.totalAmount)}
              />
              <StatCard
                icon={Store}
                iconBg="bg-violet-100"
                iconColor="text-violet-600"
                label="Outlet"
                value={billing.outletName}
              />
              <StatCard
                icon={User}
                iconBg="bg-slate-100"
                iconColor="text-slate-600"
                label="Sales Rep"
                value={billing.salesRepName}
              />
            </div>

            <Separator />

            {/* Line items label */}
            <div className="shrink-0 px-6 pb-1 pt-3">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Line Items ({billing.items.length})
              </p>
            </div>

            {/* Scrollable items table */}
            <ScrollArea className="min-h-0 flex-1 px-6 pb-3">
              <div className="overflow-hidden rounded-lg border">

                {/* Column headers */}
                <div className="grid grid-cols-[5rem_1fr_6rem_4.5rem_6rem_6rem] gap-x-3 border-b bg-muted/50 px-3 py-2 text-xs font-medium text-muted-foreground">
                  <span>Type</span>
                  <span>Product</span>
                  <span>Code</span>
                  <span className="text-right">Qty</span>
                  <span className="text-right">Unit Price</span>
                  <span className="text-right">Total</span>
                </div>

                {/* Data rows */}
                <div className="divide-y">
                  {billing.items.map((item) => {
                    const rowBg =
                      item.billingItemType === 'FreeIssue'
                        ? 'bg-amber-50/50'
                        : item.billingItemType === 'Return'
                        ? 'bg-red-50/50'
                        : ''
                    return (
                      <div
                        key={item.id}
                        className={`grid grid-cols-[5rem_1fr_6rem_4.5rem_6rem_6rem] items-start gap-x-3 px-3 py-2.5 transition-colors hover:bg-muted/30 ${rowBg}`}
                      >
                        <div className="pt-0.5">
                          <ItemTypeBadge type={item.billingItemType} />
                        </div>
                        <div className="min-w-0">
                          <p className="truncate text-sm font-medium" title={item.productDescription}>
                            {item.productDescription}
                          </p>
                          {item.discountRate > 0 && (
                            <span className="text-[10px] text-muted-foreground">
                              {item.discountRate}% disc
                            </span>
                          )}
                        </div>
                        <span className="pt-0.5 font-mono text-xs text-muted-foreground">
                          {item.productCode}
                        </span>
                        <span className="pt-0.5 text-right tabular-nums text-sm">
                          {item.quantity}
                        </span>
                        <span className="pt-0.5 text-right tabular-nums text-sm">
                          {formatCurrency(item.unitPrice)}
                        </span>
                        <span className="pt-0.5 text-right tabular-nums text-sm font-semibold">
                          {formatCurrency(item.totalPrice)}
                        </span>
                      </div>
                    )
                  })}
                </div>
              </div>
            </ScrollArea>

            {/* Footer totals */}
            <div className="shrink-0 border-t bg-muted/30 px-6 py-3 space-y-1">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Subtotal</span>
                <span className="tabular-nums">{formatCurrency(billing.subTotalAmount)}</span>
              </div>
              {billing.billDiscountAmount > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Bill Discount ({billing.billDiscountRate}%)</span>
                  <span className="tabular-nums text-red-500">− {formatCurrency(billing.billDiscountAmount)}</span>
                </div>
              )}
              {billing.returnValue > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Returns</span>
                  <span className="tabular-nums text-red-500">− {formatCurrency(billing.returnValue)}</span>
                </div>
              )}
              {billing.freeIssueValue > 0 && (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Free Issue Value</span>
                  <span className="tabular-nums text-amber-600">{formatCurrency(billing.freeIssueValue)}</span>
                </div>
              )}
              <Separator className="my-1" />
              <div className="flex justify-between text-sm font-bold">
                <span>Total</span>
                <span className="tabular-nums">{formatCurrency(billing.totalAmount)}</span>
              </div>
            </div>

            {/* Action footer */}
            <BillingActionsPanel billing={billing} onClose={onClose} />

          </div>
        ) : (
          <p className="px-6 py-10 text-sm text-muted-foreground">Billing not found.</p>
        )}
      </DialogContent>
    </Dialog>
  )
}
