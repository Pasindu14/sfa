'use client'

import { useState } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import { format } from 'date-fns'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Textarea } from '@/components/ui/textarea'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import { SalesOrderDialogs } from '../dialogs/sales-order-dialogs'
import {
  useSalesOrder,
  useRejectSalesOrder,
  useCancelSalesOrder,
} from '../../hooks/sales-order.hooks'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../../store'
import {
  rejectSalesOrderSchema,
  type RejectSalesOrderInput,
} from '../../schema/sales-order.schema'
import type { SalesOrderDto } from '../../types/sales-order.types'

const STATUS_DESCRIPTIONS: Record<number, string> = {
  0: 'Awaiting submission by distributor',
  1: 'Awaiting Sales Rep review',
  2: 'Awaiting Manager approval',
  3: 'Awaiting distributor finalization',
  4: 'Order has been finalized',
  5: 'Order has been cancelled',
  6: 'Distributor must acknowledge the rejection',
}

const HISTORY_ACTION_LABELS: Record<string, { label: string; dotClass: string }> = {
  Created: { label: 'Created', dotClass: 'bg-gray-400' },
  Submitted: { label: 'Submitted for review', dotClass: 'bg-blue-500' },
  RepApproved: { label: 'Approved by Sales Rep', dotClass: 'bg-green-500' },
  ManagerApproved: { label: 'Approved by Manager', dotClass: 'bg-green-600' },
  Rejected: { label: 'Rejected', dotClass: 'bg-red-500' },
  RejectionAcknowledged: { label: 'Rejection acknowledged', dotClass: 'bg-orange-500' },
  Finalized: { label: 'Finalized', dotClass: 'bg-purple-500' },
  Cancelled: { label: 'Cancelled', dotClass: 'bg-red-600' },
  ItemsEdited: { label: 'Items edited', dotClass: 'bg-gray-400' },
}

interface InlineReasonFormProps {
  label: string
  isPending: boolean
  onConfirm: (data: RejectSalesOrderInput) => void
  onCancel: () => void
}

function InlineReasonForm({ label, isPending, onConfirm, onCancel }: InlineReasonFormProps) {
  const form = useForm<RejectSalesOrderInput>({
    resolver: zodResolver(rejectSalesOrderSchema),
    defaultValues: { reason: '' },
  })

  return (
    <form onSubmit={form.handleSubmit(onConfirm)} className="flex flex-col gap-2 mt-2">
      <Textarea
        placeholder="Reason..."
        rows={3}
        {...form.register('reason')}
        className="text-sm"
      />
      {form.formState.errors.reason && (
        <p className="text-xs text-destructive">{form.formState.errors.reason.message}</p>
      )}
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={isPending} className="gap-1">
          {isPending && <Spinner className="h-3 w-3" />}
          Confirm {label}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={onCancel}>
          ← Back
        </Button>
      </div>
    </form>
  )
}

interface AuditRowProps {
  label: string
  value: string | number | null | undefined
}

function AuditRow({ label, value }: AuditRowProps) {
  if (value == null) return null
  return (
    <div className="flex justify-between text-xs gap-2">
      <span className="text-muted-foreground shrink-0">{label}</span>
      <span className="text-right break-all">{String(value)}</span>
    </div>
  )
}

interface OrderActionsProps {
  order: SalesOrderDto
  role: string
}

function OrderActions({ order, role }: OrderActionsProps) {
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [showCancelForm, setShowCancelForm] = useState(false)

  const { open: openSubmit } = useSubmitDialog()
  const { open: openRepApprove } = useRepApproveDialog()
  const { open: openApprove } = useApproveDialog()
  const { open: openAcknowledge } = useAcknowledgeDialog()
  const { open: openFinalize } = useFinalizeDialog()

  const { mutate: reject, isPending: isRejecting } = useRejectSalesOrder(order.id)
  const { mutate: cancel, isPending: isCancelling } = useCancelSalesOrder(order.id)

  const s = order.status

  const showSubmit = s === 0 && (role === 'Distributor' || role === 'Admin')
  const showCancel = s === 0 && (role === 'Distributor' || role === 'Admin')
  const showRepApprove = s === 1 && (role === 'SalesRep' || role === 'Admin')
  const showReject1 = s === 1 && (role === 'SalesRep' || role === 'Admin')
  const showApprove = s === 2 && (role === 'Manager' || role === 'Admin')
  const showReject2 = s === 2 && (role === 'Manager' || role === 'Admin')
  const showReject = showReject1 || showReject2
  const showAcknowledge = s === 6 && (role === 'Distributor' || role === 'Admin')
  const showFinalize = s === 3 && (role === 'Distributor' || role === 'Admin')

  const hasActions = showSubmit || showRepApprove || showApprove || showReject ||
    showAcknowledge || showFinalize || showCancel

  if (!hasActions) return null

  return (
    <div className="flex flex-col gap-2">
      {showSubmit && (
        <Button onClick={() => openSubmit(order.id)} className="w-full">
          Submit Order
        </Button>
      )}
      {showRepApprove && (
        <Button onClick={() => openRepApprove(order.id)} className="w-full">
          Rep Approve
        </Button>
      )}
      {showApprove && (
        <Button onClick={() => openApprove(order.id)} className="w-full">
          Approve Order
        </Button>
      )}
      {showAcknowledge && (
        <Button onClick={() => openAcknowledge(order.id)} variant="outline" className="w-full">
          Acknowledge Rejection
        </Button>
      )}
      {showFinalize && (
        <Button onClick={() => openFinalize(order.id)} className="w-full">
          Finalize Order
        </Button>
      )}
      {showReject && !showRejectForm && (
        <Button variant="destructive" className="w-full" onClick={() => setShowRejectForm(true)}>
          Reject
        </Button>
      )}
      {showReject && showRejectForm && (
        <InlineReasonForm
          label="Reject"
          isPending={isRejecting}
          onConfirm={(data) => reject(data, { onSuccess: () => setShowRejectForm(false) })}
          onCancel={() => setShowRejectForm(false)}
        />
      )}
      {showCancel && !showCancelForm && (
        <Button variant="outline" className="w-full text-destructive border-destructive hover:bg-destructive/10"
          onClick={() => setShowCancelForm(true)}>
          Cancel Order
        </Button>
      )}
      {showCancel && showCancelForm && (
        <InlineReasonForm
          label="Cancel"
          isPending={isCancelling}
          onConfirm={(data) => cancel(data, { onSuccess: () => setShowCancelForm(false) })}
          onCancel={() => setShowCancelForm(false)}
        />
      )}
    </div>
  )
}

interface SalesOrderDetailPageProps {
  orderId: number
}

export function SalesOrderDetailPage({ orderId }: SalesOrderDetailPageProps) {
  const { data: session } = useSession()
  const router = useRouter()
  const role = session?.user?.role ?? ''

  const { data: order, isLoading } = useSalesOrder(orderId)

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <Spinner />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="flex flex-col items-center gap-4 p-6">
        <p className="text-muted-foreground">Order not found.</p>
        <Button variant="outline" onClick={() => router.push('/sales-orders')}>
          ← Back to Sales Orders
        </Button>
      </div>
    )
  }

  const formatDate = (d: string | null | undefined) =>
    d ? format(new Date(d), 'dd MMM yyyy HH:mm') : null

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">{order.orderNumber}</span>
      </div>

      <div className="flex flex-row gap-6">
        {/* Left column */}
        <div className="flex-1 flex flex-col gap-4">
          {/* Order header */}
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start justify-between">
                <div>
                  <h1 className="text-2xl font-bold">{order.orderNumber}</h1>
                  <p className="text-muted-foreground text-sm mt-1">{order.distributorName}</p>
                  <p className="text-muted-foreground text-xs mt-1">
                    Created {formatDate(order.createdAt)}
                  </p>
                </div>
                <SalesOrderStatusBadge status={order.status} className="text-sm" />
              </div>
            </CardContent>
          </Card>

          {/* Line items */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Line Items</CardTitle>
            </CardHeader>
            <CardContent>
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-xs text-muted-foreground">
                    <th className="text-left py-2">Product</th>
                    <th className="text-left py-2">SKU</th>
                    <th className="text-right py-2">Qty</th>
                    <th className="text-right py-2">Unit Price</th>
                    <th className="text-right py-2">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {order.items.map((item) => (
                    <tr key={item.id} className="border-b last:border-0">
                      <td className="py-2">{item.productDescription}</td>
                      <td className="py-2 font-mono text-xs text-muted-foreground">{item.productCode}</td>
                      <td className="py-2 text-right">{item.quantity}</td>
                      <td className="py-2 text-right">{formatLKR(item.unitPrice)}</td>
                      <td className="py-2 text-right font-medium">{formatLKR(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={4} className="pt-3 text-right font-semibold text-sm">Total</td>
                    <td className="pt-3 text-right font-bold">{formatLKR(order.totalAmount)}</td>
                  </tr>
                </tfoot>
              </table>
            </CardContent>
          </Card>

          {/* Notes */}
          {order.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Notes</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{order.notes}</p>
              </CardContent>
            </Card>
          )}

          {/* History timeline */}
          {order.history && order.history.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">History</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col gap-4">
                  {order.history.map((entry) => {
                    const cfg = HISTORY_ACTION_LABELS[entry.action] ?? {
                      label: entry.action,
                      dotClass: 'bg-gray-400',
                    }
                    return (
                      <div key={entry.id} className="flex gap-3 items-start">
                        <div className={`mt-1 h-2.5 w-2.5 rounded-full shrink-0 ${cfg.dotClass}`} />
                        <div className="flex flex-col gap-0.5">
                          <div className="text-sm font-medium">
                            {cfg.label}
                            <span className="text-muted-foreground font-normal ml-2 text-xs">
                              by {entry.performedByName ?? `User #${entry.performedBy}`}
                            </span>
                            <span className="text-muted-foreground font-normal ml-2 text-xs">
                              · {format(new Date(entry.performedAt), 'dd MMM yyyy HH:mm')}
                            </span>
                          </div>
                          {entry.notes && (
                            <p className="text-xs text-muted-foreground italic">{entry.notes}</p>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right sidebar */}
        <div className="w-80 flex flex-col gap-4 sticky top-6 self-start">
          {/* Status card */}
          <Card>
            <CardContent className="pt-6 flex flex-col gap-3">
              <SalesOrderStatusBadge status={order.status} className="self-start text-sm" />
              <p className="text-xs text-muted-foreground">
                {STATUS_DESCRIPTIONS[order.status]}
              </p>
              <Separator />
              <OrderActions order={order} role={role} />
            </CardContent>
          </Card>

          {/* Audit Trail */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Audit Trail</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-2">
              <AuditRow label="Created by" value={order.createdBy} />
              <AuditRow label="Created at" value={formatDate(order.createdAt)} />
              <AuditRow label="Submitted by" value={order.submittedBy} />
              <AuditRow label="Submitted at" value={formatDate(order.submittedAt)} />
              <AuditRow label="Rep approved by" value={order.repApprovedBy} />
              <AuditRow label="Rep approved at" value={formatDate(order.repApprovedAt)} />
              <AuditRow label="Manager approved by" value={order.managerApprovedBy} />
              <AuditRow label="Manager approved at" value={formatDate(order.managerApprovedAt)} />
              <AuditRow label="Rejection reason" value={order.cancelReason} />
              <AuditRow label="Acknowledged by" value={order.acknowledgedBy} />
              <AuditRow label="Acknowledged at" value={formatDate(order.acknowledgedAt)} />
              <AuditRow label="Finalized by" value={order.finalizedBy} />
              <AuditRow label="Finalized at" value={formatDate(order.finalizedAt)} />
              <AuditRow label="Cancelled by" value={order.cancelledBy} />
              <AuditRow label="Cancelled at" value={formatDate(order.cancelledAt)} />
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Action dialogs (portaled) */}
      <SalesOrderDialogs />
    </div>
  )
}
