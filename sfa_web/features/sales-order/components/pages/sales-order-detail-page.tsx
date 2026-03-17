'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Pencil,
  CheckCircle,
  XCircle,
  AlertCircle,
  Clock,
  Circle,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import { SalesOrderDialogs } from '../dialogs/sales-order-dialogs'
import {
  useSalesOrder,
  useSubmitSalesOrder,
  useRepApproveSalesOrder,
  useApproveSalesOrder,
  useRejectSalesOrder,
  useAcknowledgeSalesOrder,
  useFinalizeSalesOrder,
  useCancelSalesOrder,
} from '../../hooks/sales-order.hooks'
import { useSalesOrderDialogStore } from '../../store'
import {
  SalesOrderStatus,
  rejectSalesOrderSchema,
  type SalesOrderDto,
  type SalesOrderHistoryDto,
  type RejectSalesOrderInput,
} from '../../schema/sales-order.schema'

// ── Helpers ────────────────────────────────────────────────────────────────

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(dateStr: string | null | undefined) {
  if (!dateStr) return null
  return new Date(dateStr).toLocaleDateString('en-LK', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatDateShort(dateStr: string | null | undefined) {
  if (!dateStr) return null
  return new Date(dateStr).toLocaleDateString('en-LK', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

// ── History Timeline ───────────────────────────────────────────────────────

const historyActionConfig: Record<string, { color: string; Icon: React.ElementType }> = {
  Created: { color: 'bg-gray-400', Icon: Circle },
  Submitted: { color: 'bg-blue-500', Icon: Clock },
  RepApproved: { color: 'bg-green-500', Icon: CheckCircle },
  ManagerApproved: { color: 'bg-green-600', Icon: CheckCircle },
  Rejected: { color: 'bg-red-500', Icon: XCircle },
  RejectionAcknowledged: { color: 'bg-orange-500', Icon: AlertCircle },
  Cancelled: { color: 'bg-red-600', Icon: XCircle },
  PendingDistributorAcknowledgement: { color: 'bg-orange-400', Icon: AlertCircle },
  PendingDistributorFinalization: { color: 'bg-purple-500', Icon: Clock },
  Finalized: { color: 'bg-emerald-600', Icon: CheckCircle },
}

function historyActionLabel(action: string): string {
  const map: Record<string, string> = {
    Created: 'Order Created',
    Submitted: 'Submitted',
    RepApproved: 'Approved by Rep',
    ManagerApproved: 'Approved by Manager',
    Rejected: 'Rejected',
    RejectionAcknowledged: 'Rejection Acknowledged',
    Cancelled: 'Cancelled',
    Finalized: 'Finalized',
    PendingDistributorFinalization: 'Sent for Finalization',
    PendingDistributorAcknowledgement: 'Pending Distributor Acknowledgement',
  }
  return map[action] ?? action
}

function HistoryTimeline({ history }: { history: SalesOrderHistoryDto[] }) {
  return (
    <div className="space-y-0">
      {history.map((entry, idx) => {
        const cfg = historyActionConfig[entry.action] ?? { color: 'bg-gray-300', Icon: Circle }
        const isLast = idx === history.length - 1

        return (
          <div key={entry.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className={`w-3 h-3 rounded-full mt-1 shrink-0 ${cfg.color} ${isLast ? 'ring-2 ring-offset-1 ring-orange-400' : ''}`} />
              {!isLast && <div className="w-px flex-1 bg-border mt-1" />}
            </div>
            <div className="pb-4">
              <div className="flex items-center gap-2">
                <p className="text-sm font-medium leading-tight">
                  {entry.action === 'Rejected'
                    ? `Rejected by ${entry.performedByName ?? 'Unknown'}`
                    : entry.performedByName
                    ? `${historyActionLabel(entry.action)} — ${entry.performedByName}`
                    : historyActionLabel(entry.action)}
                </p>
                {isLast && (
                  <span className="text-[10px] font-semibold uppercase bg-orange-100 text-orange-700 px-1.5 py-0.5 rounded">
                    CURRENT
                  </span>
                )}
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">{formatDate(entry.performedAt)}</p>
              {entry.notes && (
                <p className="text-xs text-muted-foreground mt-1 italic bg-muted/50 px-2 py-1 rounded">
                  Reason: {entry.notes}
                </p>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── Audit row helper ───────────────────────────────────────────────────────

function AuditRow({ label, value }: { label: string; value: string | null | undefined }) {
  if (!value) return null
  return (
    <div>
      <p className="text-[10px] font-semibold uppercase tracking-wide text-orange-600">{label}</p>
      <p className="text-sm text-foreground mt-0.5">{value}</p>
    </div>
  )
}

// ── Inline reason form ────────────────────────────────────────────────────

interface InlineReasonFormProps {
  label: string
  isPending: boolean
  onSubmit: (data: RejectSalesOrderInput) => void
  onCancel: () => void
}

function InlineReasonForm({ label, isPending, onSubmit, onCancel }: InlineReasonFormProps) {
  const form = useForm<RejectSalesOrderInput>({
    resolver: zodResolver(rejectSalesOrderSchema),
    defaultValues: { reason: '' },
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-2 mt-2">
        <FormField
          control={form.control}
          name="reason"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <Textarea
                  {...field}
                  placeholder="Enter reason..."
                  rows={3}
                  className="text-sm resize-none"
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <div className="flex gap-2">
          <Button type="submit" variant="destructive" size="sm" disabled={isPending} className="flex-1">
            {isPending && <Spinner className="mr-2 h-3 w-3" />}
            {label}
          </Button>
          <Button type="button" variant="outline" size="sm" onClick={onCancel} className="flex-1">
            Cancel
          </Button>
        </div>
      </form>
    </Form>
  )
}

// ── Order Actions Panel ────────────────────────────────────────────────────

function OrderActionsPanel({ order }: { order: SalesOrderDto }) {
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [showCancelForm, setShowCancelForm] = useState(false)

  const store = useSalesOrderDialogStore()
  const { mutate: submit, isPending: isSubmitting } = useSubmitSalesOrder()
  const { mutate: repApprove, isPending: isRepApproving } = useRepApproveSalesOrder()
  const { mutate: approve, isPending: isApproving } = useApproveSalesOrder()
  const { mutate: reject, isPending: isRejecting } = useRejectSalesOrder(order.id)
  const { mutate: acknowledge, isPending: isAcknowledging } = useAcknowledgeSalesOrder()
  const { mutate: finalize, isPending: isFinalizing } = useFinalizeSalesOrder()
  const { mutate: cancel, isPending: isCancelling } = useCancelSalesOrder(order.id)

  const status = order.status

  return (
    <div className="space-y-2">
      {/* Draft */}
      {status === SalesOrderStatus.Draft && (
        <>
          <Button className="w-full" onClick={() => store.openSubmit(order.id)} disabled={isSubmitting}>
            {isSubmitting && <Spinner className="mr-2" />}
            Submit for Approval
          </Button>
          {!showCancelForm ? (
            <Button variant="outline" className="w-full" onClick={() => setShowCancelForm(true)}>
              Cancel Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Cancel Order"
              isPending={isCancelling}
              onSubmit={(data) => { cancel(data); setShowCancelForm(false) }}
              onCancel={() => setShowCancelForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Rep Approval */}
      {status === SalesOrderStatus.PendingRepApproval && (
        <>
          <Button className="w-full" onClick={() => store.openRepApprove(order.id)} disabled={isRepApproving}>
            {isRepApproving && <Spinner className="mr-2" />}
            Approve (Rep)
          </Button>
          {!showRejectForm ? (
            <Button variant="outline" className="w-full" onClick={() => setShowRejectForm(true)}>
              Reject
            </Button>
          ) : (
            <InlineReasonForm
              label="Reject Order"
              isPending={isRejecting}
              onSubmit={(data) => { reject(data); setShowRejectForm(false) }}
              onCancel={() => setShowRejectForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Manager Approval */}
      {status === SalesOrderStatus.PendingManagerApproval && (
        <>
          <Button className="w-full" onClick={() => store.openApprove(order.id)} disabled={isApproving}>
            {isApproving && <Spinner className="mr-2" />}
            Approve
          </Button>
          {!showRejectForm ? (
            <Button variant="outline" className="w-full" onClick={() => setShowRejectForm(true)}>
              Reject
            </Button>
          ) : (
            <InlineReasonForm
              label="Reject Order"
              isPending={isRejecting}
              onSubmit={(data) => { reject(data); setShowRejectForm(false) }}
              onCancel={() => setShowRejectForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Distributor Acknowledgement */}
      {status === SalesOrderStatus.PendingDistributorAcknowledgement && (
        <>
          <Button
            className="w-full bg-orange-600 hover:bg-orange-700 text-white"
            onClick={() => store.openAcknowledge(order.id)}
            disabled={isAcknowledging}
          >
            {isAcknowledging && <Spinner className="mr-2" />}
            + Acknowledge Rejection
          </Button>
          {!showCancelForm ? (
            <Button variant="outline" className="w-full" onClick={() => setShowCancelForm(true)}>
              Cancel Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Cancel Order"
              isPending={isCancelling}
              onSubmit={(data) => { cancel(data); setShowCancelForm(false) }}
              onCancel={() => setShowCancelForm(false)}
            />
          )}
          <p className="text-xs text-muted-foreground text-center">
            Acknowledging confirms you have seen this rejection. Order will be moved to Cancelled.
          </p>
        </>
      )}

      {/* Pending Distributor Finalization */}
      {status === SalesOrderStatus.PendingDistributorFinalization && (
        <Button className="w-full" onClick={() => store.openFinalize(order.id)} disabled={isFinalizing}>
          {isFinalizing && <Spinner className="mr-2" />}
          Finalize Order
        </Button>
      )}

      {/* Terminal states */}
      {(status === SalesOrderStatus.Finalized || status === SalesOrderStatus.Cancelled) && (
        <p className="text-sm text-center text-muted-foreground py-2">
          {status === SalesOrderStatus.Finalized ? 'This order has been finalized.' : 'This order has been cancelled.'}
        </p>
      )}
    </div>
  )
}

// ── Main Page ──────────────────────────────────────────────────────────────

interface SalesOrderDetailPageProps {
  orderId: number
}

export function SalesOrderDetailPage({ orderId }: SalesOrderDetailPageProps) {
  const { data: order, isLoading, isError } = useSalesOrder(orderId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-100">
        <Spinner className="size-8" />
      </div>
    )
  }

  if (isError || !order) {
    return (
      <div className="flex items-center justify-center min-h-100">
        <p className="text-muted-foreground">Order not found.</p>
      </div>
    )
  }

  const subtotal = order.items.reduce((s, i) => s + i.lineTotal, 0)
  const tax = order.totalAmount - subtotal
  const rejectedEntry = order.history.findLast((h) => h.action === 'Rejected')

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{order.orderNumber}</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Sales order details — {order.distributorName}
        </p>
      </div>

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-6 items-start">

        {/* Left column */}
        <div className="space-y-6">

          {/* Order Items */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Order Items</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/40">
                    <th className="text-left px-6 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">Product</th>
                    <th className="text-left px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">SKU</th>
                    <th className="text-right px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">QTY</th>
                    <th className="text-right px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">Unit Price</th>
                    <th className="text-right px-6 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">Line Total</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {order.items.map((item) => (
                    <tr key={item.id} className="hover:bg-muted/20">
                      <td className="px-6 py-3 font-medium">{item.productDescription}</td>
                      <td className="px-4 py-3">
                        <span className="font-mono text-xs bg-muted px-1.5 py-0.5 rounded text-muted-foreground">
                          {item.productCode}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">{item.quantity}</td>
                      <td className="px-4 py-3 text-right">{formatCurrency(item.unitPrice)}</td>
                      <td className="px-6 py-3 text-right font-semibold tabular-nums">{formatCurrency(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Totals */}
              <div className="border-t px-6 py-4 space-y-1">
                <div className="flex justify-between text-sm text-muted-foreground">
                  <span>Subtotal</span>
                  <span className="tabular-nums">{formatCurrency(subtotal)}</span>
                </div>
                {tax > 0 && (
                  <div className="flex justify-between text-sm text-muted-foreground">
                    <span>Tax (8%)</span>
                    <span className="tabular-nums">{formatCurrency(tax)}</span>
                  </div>
                )}
                <div className="flex justify-between text-base font-bold pt-1 border-t mt-1">
                  <span>Total (LKR)</span>
                  <span className="tabular-nums">{formatCurrency(order.totalAmount)}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Notes */}
          {order.notes && (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-base">Notes</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{order.notes}</p>
              </CardContent>
            </Card>
          )}

          {/* Order History */}
          {order.history.length > 0 && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">Order History</CardTitle>
              </CardHeader>
              <CardContent>
                <HistoryTimeline history={order.history} />
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right sidebar */}
        <div className="space-y-4 lg:sticky lg:top-6">

          {/* Order Info */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Order Info</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Order #</span>
                <span className="font-medium">{order.orderNumber}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Created</span>
                <span>{formatDateShort(order.createdAt)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Distributor</span>
                <span className="font-medium">{order.distributorName}</span>
              </div>
              {order.salesRepName && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Sales Rep</span>
                  <span className="font-medium">{order.salesRepName}</span>
                </div>
              )}
              {order.managerApprovedBy && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Manager</span>
                  <span className="font-medium">{order.managerApprovedBy}</span>
                </div>
              )}
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Status</span>
                <SalesOrderStatusBadge status={order.status} />
              </div>
            </CardContent>
          </Card>

          {/* Rejection Details */}
          {order.status === SalesOrderStatus.PendingDistributorAcknowledgement && rejectedEntry && (
            <Card className="border-orange-200 bg-orange-50/30">
              <CardHeader className="pb-2">
                <CardTitle className="text-base text-orange-800">Rejection Details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <AuditRow
                  label="Rejected By"
                  value={`${rejectedEntry.performedByName ?? 'Unknown'}${order.managerApprovedBy ? ' (Manager)' : ' (Rep)'}`}
                />
                <AuditRow label="Reason" value={order.cancelReason} />
                <AuditRow label="Rejected At" value={formatDate(rejectedEntry.performedAt)} />
              </CardContent>
            </Card>
          )}

          {/* Audit Trail */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Audit Trail</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <AuditRow label="Created" value={formatDate(order.createdAt)} />
              <AuditRow label="Submitted At" value={formatDate(order.submittedAt)} />
              <AuditRow label="Rep Approved At" value={formatDate(order.repApprovedAt)} />
              <AuditRow label="Manager Approved At" value={formatDate(order.managerApprovedAt)} />
              <AuditRow label="Finalized At" value={formatDate(order.finalizedAt)} />
              <AuditRow label="Acknowledged At" value={formatDate(order.acknowledgedAt)} />
              <AuditRow label="Cancelled At" value={formatDate(order.cancelledAt)} />
              {order.cancelReason && order.status === SalesOrderStatus.Cancelled && (
                <AuditRow label="Cancel Reason" value={order.cancelReason} />
              )}
            </CardContent>
          </Card>

          {/* Actions */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <OrderActionsPanel order={order} />
              {order.status === SalesOrderStatus.Draft && (
                <Button variant="outline" className="w-full" asChild>
                  <Link href={`/sales-orders/${order.id}/edit`} className="flex items-center gap-2">
                    <Pencil className="h-4 w-4" />
                    Edit Order
                  </Link>
                </Button>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      <SalesOrderDialogs />
    </div>
  )
}
