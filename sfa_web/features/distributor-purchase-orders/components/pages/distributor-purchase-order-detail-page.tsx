'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Activity,
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Circle,
  Clock,
  FileEdit,
  ShoppingCart,
  XCircle,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form'
import {
  useMyPurchaseOrder,
  useMyProductCategoryPricings,
  useSubmitMyPurchaseOrder,
  useFinalizeMyPurchaseOrder,
  useAcknowledgeMyPurchaseOrder,
  useCancelMyPurchaseOrder,
} from '../../hooks/distributor-purchase-order.hooks'
import { DistributorPurchaseOrderStatusBadge } from '../distributor-purchase-order-status-badge'
import {
  PurchaseOrderStatus,
  cancelMyPurchaseOrderSchema,
  type MyPurchaseOrderDto,
  type MyPurchaseOrderHistoryDto,
  type CancelMyPurchaseOrderInput,
  type SnapshotItem,
  type PurchaseOrderStatusValue,
} from '../../schema/distributor-purchase-order.schema'
type ProductLike = { id: number; code: string; itemDescription: string }

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
  ItemsEdited: { color: 'bg-indigo-500', Icon: FileEdit },
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
    ItemsEdited: 'Items Edited',
  }
  return map[action] ?? action
}

function parseSnapshot(raw: string | null): SnapshotItem[] {
  if (!raw) return []
  const json = raw.startsWith('Before: ') ? raw.slice(8) : raw
  try {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const parsed: any[] = JSON.parse(json)
    return parsed.map((item) => ({
      productId: item.productId ?? item.ProductId,
      quantity: item.quantity ?? item.Quantity,
      unitPrice: item.unitPrice ?? item.UnitPrice,
      discount: item.discount ?? item.Discount,
    }))
  } catch {
    return []
  }
}

function ItemsDiffPanel({
  notes,
  itemsSnapshot,
  products,
}: {
  notes: string | null
  itemsSnapshot: string | null
  products: ProductLike[]
}) {
  const before = parseSnapshot(notes)
  const after = parseSnapshot(itemsSnapshot)

  if (before.length === 0 && after.length === 0) {
    return <p className="mt-2 text-xs text-muted-foreground italic">No item snapshot available.</p>
  }

  const getProductLabel = (productId: number) => {
    const p = products.find((p) => p.id === productId)
    return p ? `${p.code} — ${p.itemDescription}` : `Product #${productId}`
  }

  const allProductIds = [
    ...new Set([
      ...before.map((i) => i.productId),
      ...after.map((i) => i.productId),
    ]),
  ].filter((pid): pid is number => pid != null)

  return (
    <div className="mt-2 rounded border text-xs overflow-hidden">
      <table className="w-full">
        <thead>
          <tr className="bg-muted/60 text-muted-foreground">
            <th className="px-2 py-1.5 text-left font-medium">Product</th>
            <th className="px-2 py-1.5 text-right font-medium">Before Qty</th>
            <th className="px-2 py-1.5 text-right font-medium">After Qty</th>
            <th className="px-2 py-1.5 text-right font-medium">Before Price</th>
            <th className="px-2 py-1.5 text-right font-medium">After Price</th>
            <th className="px-2 py-1.5 text-center font-medium">Change</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {allProductIds.map((pid, rowIdx) => {
            const b = before.find((i) => i.productId === pid)
            const a = after.find((i) => i.productId === pid)
            const isAdded = !b && !!a
            const isRemoved = !!b && !a
            const isChanged =
              !!b &&
              !!a &&
              (b.quantity !== a.quantity ||
                b.unitPrice !== a.unitPrice ||
                b.discount !== a.discount)
            const rowClass = isAdded
              ? 'bg-green-50 text-green-800'
              : isRemoved
                ? 'bg-red-50 text-red-700 opacity-70'
                : isChanged
                  ? 'bg-amber-50'
                  : 'bg-background'
            return (
              <tr key={`${pid}-${rowIdx}`} className={rowClass}>
                <td className={`px-2 py-1.5 ${isRemoved ? 'line-through' : ''}`}>
                  {getProductLabel(pid)}
                </td>
                <td className="px-2 py-1.5 text-right">{b?.quantity ?? '—'}</td>
                <td className="px-2 py-1.5 text-right">{a?.quantity ?? '—'}</td>
                <td className="px-2 py-1.5 text-right">{b ? formatCurrency(b.unitPrice) : '—'}</td>
                <td className="px-2 py-1.5 text-right">{a ? formatCurrency(a.unitPrice) : '—'}</td>
                <td className="px-2 py-1.5 text-center">
                  {isAdded ? (
                    <span className="text-green-700 font-medium">+ Added</span>
                  ) : isRemoved ? (
                    <span className="text-red-600 font-medium">- Removed</span>
                  ) : isChanged ? (
                    <span className="text-amber-700 font-medium">~ Changed</span>
                  ) : (
                    <span className="text-muted-foreground">Unchanged</span>
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function HistoryTimeline({
  history,
  products,
}: {
  history: MyPurchaseOrderHistoryDto[]
  products: ProductLike[]
}) {
  const [expandedIds, setExpandedIds] = useState<Set<number>>(new Set())

  const toggle = (id: number) =>
    setExpandedIds((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })

  return (
    <div className="space-y-0">
      {history.map((entry, idx) => {
        const cfg = historyActionConfig[entry.action] ?? { color: 'bg-gray-300', Icon: Circle }
        const isLast = idx === history.length - 1
        const isItemEdit = entry.action === 'ItemsEdited'
        const isExpanded = expandedIds.has(entry.id)

        return (
          <div key={entry.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div
                className={`w-3 h-3 rounded-full mt-1 shrink-0 ${cfg.color} ${isLast ? 'ring-2 ring-offset-1 ring-orange-400' : ''}`}
              />
              {!isLast && <div className="w-px flex-1 bg-border mt-1" />}
            </div>
            <div className="pb-4 flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
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
                {isItemEdit && (
                  <button
                    onClick={() => toggle(entry.id)}
                    className="ml-auto text-xs text-indigo-600 hover:text-indigo-800 underline underline-offset-2"
                  >
                    {isExpanded ? 'Hide changes' : 'View changes'}
                  </button>
                )}
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">
                {formatDate(entry.performedAt)}
              </p>
              {!isItemEdit && entry.notes && (
                <p className="text-xs text-muted-foreground mt-1 italic bg-muted/50 px-2 py-1 rounded">
                  Reason: {entry.notes}
                </p>
              )}
              {isItemEdit && isExpanded && (
                <ItemsDiffPanel
                  notes={entry.notes}
                  itemsSnapshot={entry.itemsSnapshot}
                  products={products}
                />
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── Approval Progress ──────────────────────────────────────────────────────

type ProgressStep = {
  label: string
  subLabel: string
  state: 'done' | 'active' | 'pending'
}

function getApprovalSteps(order: MyPurchaseOrderDto): ProgressStep[] {
  const status = order.status

  const statusOrder = [
    PurchaseOrderStatus.Draft,
    PurchaseOrderStatus.PendingRepApproval,
    PurchaseOrderStatus.PendingManagerApproval,
    PurchaseOrderStatus.PendingDistributorFinalization,
    PurchaseOrderStatus.Finalized,
  ]
  const statusIdx = statusOrder.indexOf(status as (typeof statusOrder)[number])
  const isDone = (s: PurchaseOrderStatusValue) =>
    statusIdx > statusOrder.indexOf(s as (typeof statusOrder)[number]) &&
    status !== PurchaseOrderStatus.Cancelled
  const isActive = (s: PurchaseOrderStatusValue) => status === s

  const repDone = isDone(PurchaseOrderStatus.PendingRepApproval)
  const repActive = isActive(PurchaseOrderStatus.PendingRepApproval)
  const managerDone = isDone(PurchaseOrderStatus.PendingManagerApproval)
  const managerActive = isActive(PurchaseOrderStatus.PendingManagerApproval)
  const finalizationDone = status === PurchaseOrderStatus.Finalized
  const finalizationActive =
    status === PurchaseOrderStatus.PendingDistributorFinalization ||
    status === PurchaseOrderStatus.PendingDistributorAcknowledgement

  const createdEntry = order.history.find((h) => h.action === 'Created')

  return [
    {
      label: 'Order Created',
      subLabel: createdEntry
        ? `${formatDateShort(createdEntry.performedAt)} — ${createdEntry.performedByName ?? 'You'}`
        : 'Created',
      state: 'done',
    },
    {
      label: 'Rep Approval',
      subLabel: repDone
        ? `Approved${order.repApprovedAt ? ` — ${formatDateShort(order.repApprovedAt)}` : ''}`
        : repActive
          ? 'Awaiting rep review'
          : status === PurchaseOrderStatus.Draft
            ? 'Awaiting submission'
            : 'Pending rep action',
      state: repDone ? 'done' : repActive ? 'active' : 'pending',
    },
    {
      label: 'Manager Approval',
      subLabel: managerDone
        ? `Approved${order.managerApprovedAt ? ` — ${formatDateShort(order.managerApprovedAt)}` : ''}`
        : managerActive
          ? 'Awaiting manager review'
          : 'Pending rep action',
      state: managerDone ? 'done' : managerActive ? 'active' : 'pending',
    },
    {
      label: 'Finalization',
      subLabel: finalizationDone
        ? `Finalized${order.finalizedAt ? ` — ${formatDateShort(order.finalizedAt)}` : ''}`
        : finalizationActive
          ? 'Your action required'
          : 'Pending approval',
      state: finalizationDone ? 'done' : finalizationActive ? 'active' : 'pending',
    },
  ]
}

function ApprovalProgress({ order }: { order: MyPurchaseOrderDto }) {
  const steps = getApprovalSteps(order)

  return (
    <div className="space-y-3">
      {steps.map((step, idx) => (
        <div key={idx} className="flex gap-3 items-start">
          <div className="flex flex-col items-center">
            <div
              className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 ${
                step.state === 'done'
                  ? 'bg-orange-500 text-white'
                  : step.state === 'active'
                    ? 'border-2 border-orange-500 bg-white'
                    : 'border-2 border-muted-foreground/30 bg-white'
              }`}
            >
              {step.state === 'done' ? (
                <CheckCircle className="w-3.5 h-3.5" />
              ) : step.state === 'active' ? (
                <div className="w-2 h-2 rounded-full bg-orange-500" />
              ) : (
                <div className="w-2 h-2 rounded-full bg-muted-foreground/30" />
              )}
            </div>
            {idx < steps.length - 1 && (
              <div
                className={`w-px h-6 mt-1 ${step.state === 'done' ? 'bg-orange-300' : 'bg-muted-foreground/20'}`}
              />
            )}
          </div>
          <div className="pb-1">
            <p
              className={`text-sm font-medium leading-tight ${step.state === 'pending' ? 'text-muted-foreground' : ''}`}
            >
              {step.label}
            </p>
            <p className="text-xs text-muted-foreground mt-0.5">{step.subLabel}</p>
          </div>
        </div>
      ))}
    </div>
  )
}

// ── Cancel Dialog ─────────────────────────────────────────────────────────

function CancelOrderDialog({
  open,
  onOpenChange,
  orderId,
  onSuccess,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  orderId: number
  onSuccess: () => void
}) {
  const { mutate: cancel, isPending } = useCancelMyPurchaseOrder(orderId, () => {
    onOpenChange(false)
    onSuccess()
  })

  const form = useForm<CancelMyPurchaseOrderInput>({
    resolver: zodResolver(cancelMyPurchaseOrderSchema),
    defaultValues: { reason: '' },
  })

  const onSubmit = (data: CancelMyPurchaseOrderInput) => cancel(data)

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Cancel Order</AlertDialogTitle>
          <AlertDialogDescription>
            Please provide a reason for cancellation. This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormControl>
                    <Textarea
                      {...field}
                      placeholder="Enter cancellation reason (min 5 characters)..."
                      rows={3}
                      className="resize-none"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <AlertDialogFooter>
              <AlertDialogCancel type="button" disabled={isPending}>
                Keep Order
              </AlertDialogCancel>
              <Button type="submit" variant="destructive" disabled={isPending}>
                {isPending && <Spinner className="mr-2 h-3 w-3" />}
                Cancel Order
              </Button>
            </AlertDialogFooter>
          </form>
        </Form>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Order Actions Panel ────────────────────────────────────────────────────

function OrderActionsPanel({ order }: { order: MyPurchaseOrderDto }) {
  const router = useRouter()
  const [submitOpen, setSubmitOpen] = useState(false)
  const [finalizeOpen, setFinalizeOpen] = useState(false)
  const [acknowledgeOpen, setAcknowledgeOpen] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)

  const { mutate: submit, isPending: isSubmitting } = useSubmitMyPurchaseOrder(() => {
    router.refresh()
  })
  const { mutate: finalize, isPending: isFinalizing } = useFinalizeMyPurchaseOrder(() => {
    router.refresh()
  })
  const { mutate: acknowledge, isPending: isAcknowledging } = useAcknowledgeMyPurchaseOrder(() => {
    router.refresh()
  })

  const status = order.status

  return (
    <div className="space-y-2">
      {/* Draft */}
      {status === PurchaseOrderStatus.Draft && (
        <>
          <Button
            className="w-full"
            onClick={() => setSubmitOpen(true)}
          >
            Submit for Approval
          </Button>
          <Link href={`/distributor-purchase-orders/${order.id}/edit`} className="w-full block">
            <Button variant="outline" className="w-full gap-2">
              <FileEdit className="h-4 w-4" />
              Edit Order
            </Button>
          </Link>
          <Button
            variant="ghost"
            className="w-full text-destructive hover:text-destructive"
            onClick={() => setCancelOpen(true)}
          >
            <XCircle className="h-4 w-4 mr-2" />
            Cancel Order
          </Button>
        </>
      )}

      {/* Awaiting approval — info only */}
      {(status === PurchaseOrderStatus.PendingRepApproval ||
        status === PurchaseOrderStatus.PendingManagerApproval) && (
        <div className="flex items-start gap-2 rounded-md border bg-blue-50 p-3">
          <Clock className="h-4 w-4 text-blue-500 mt-0.5 shrink-0" />
          <p className="text-sm text-blue-800">
            {status === PurchaseOrderStatus.PendingRepApproval
              ? 'Awaiting sales rep review.'
              : 'Awaiting manager approval.'}
          </p>
        </div>
      )}

      {/* Pending Distributor Finalization */}
      {status === PurchaseOrderStatus.PendingDistributorFinalization && (
        <Button
          className="w-full gap-2"
          onClick={() => setFinalizeOpen(true)}
        >
          <CheckCircle className="h-4 w-4" />
          Finalize Order
        </Button>
      )}

      {/* Pending Distributor Acknowledgement */}
      {status === PurchaseOrderStatus.PendingDistributorAcknowledgement && (
        <>
          <Button
            className="w-full gap-2"
            onClick={() => setAcknowledgeOpen(true)}
          >
            Acknowledge &amp; Cancel
          </Button>
          <p className="text-xs text-muted-foreground text-center">
            Acknowledging confirms you have seen this rejection. The order will be cancelled.
          </p>
        </>
      )}

      {/* Terminal states */}
      {(status === PurchaseOrderStatus.Finalized ||
        status === PurchaseOrderStatus.Cancelled) && (
        <p className="text-sm text-center text-muted-foreground py-2">
          {status === PurchaseOrderStatus.Finalized
            ? 'This order has been finalized.'
            : 'This order has been cancelled.'}
        </p>
      )}

      {/* Submit confirm */}
      <AlertDialog open={submitOpen} onOpenChange={setSubmitOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Submit for Approval?</AlertDialogTitle>
            <AlertDialogDescription>
              Once submitted, the order will be sent to the sales rep for review.
              You will not be able to edit items after submission.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isSubmitting}>Back</AlertDialogCancel>
            <Button
              onClick={() => {
                submit(order.id)
                setSubmitOpen(false)
              }}
              disabled={isSubmitting}
            >
              {isSubmitting && <Spinner className="mr-2 h-3 w-3" />}
              Submit
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Finalize confirm */}
      <AlertDialog open={finalizeOpen} onOpenChange={setFinalizeOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Finalize Order?</AlertDialogTitle>
            <AlertDialogDescription>
              By finalizing, you confirm receipt and acceptance of this order.
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isFinalizing}>Back</AlertDialogCancel>
            <Button
              onClick={() => {
                finalize(order.id)
                setFinalizeOpen(false)
              }}
              disabled={isFinalizing}
            >
              {isFinalizing && <Spinner className="mr-2 h-3 w-3" />}
              Finalize
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Acknowledge confirm */}
      <AlertDialog open={acknowledgeOpen} onOpenChange={setAcknowledgeOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Acknowledge Rejection?</AlertDialogTitle>
            <AlertDialogDescription>
              This confirms you have seen the rejection. The order will be moved to Cancelled.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isAcknowledging}>Back</AlertDialogCancel>
            <Button
              onClick={() => {
                acknowledge(order.id)
                setAcknowledgeOpen(false)
              }}
              disabled={isAcknowledging}
            >
              {isAcknowledging && <Spinner className="mr-2 h-3 w-3" />}
              Acknowledge
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Cancel with reason */}
      <CancelOrderDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        orderId={order.id}
        onSuccess={() => router.refresh()}
      />
    </div>
  )
}

// ── Audit Row ──────────────────────────────────────────────────────────────

function AuditRow({ label, value }: { label: string; value: string | null | undefined }) {
  if (!value) return null
  return (
    <div className="flex justify-between items-start gap-4">
      <span className="text-muted-foreground text-xs shrink-0">{label}</span>
      <span className="text-xs font-medium text-right">{value}</span>
    </div>
  )
}

// ── Main Page ──────────────────────────────────────────────────────────────

interface Props {
  id: number
}

export function DistributorPurchaseOrderDetailPage({ id }: Props) {
  const { data: order, isLoading, isError } = useMyPurchaseOrder(id)
  const { data: categoryPricings = [] } = useMyProductCategoryPricings()
  const allProducts: ProductLike[] = categoryPricings.map((r) => ({
    id: r.productId,
    code: r.productCode,
    itemDescription: r.itemDescription,
  }))

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner className="size-4" />
      </div>
    )
  }

  if (isError || !order) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <p className="text-muted-foreground">Order not found.</p>
      </div>
    )
  }

  const subtotal = order.items.reduce((s, i) => s + i.lineTotal, 0)
  const rejectedEntry = order.history.findLast((h) => h.action === 'Rejected')
  const lastEditEntry = order.history.findLast((h) => h.action === 'ItemsEdited')
  const isPendingAck = order.status === PurchaseOrderStatus.PendingDistributorAcknowledgement

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto min-w-3/4">
      {/* Header banner */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div className="flex items-center gap-4">
          <Link href="/distributor-purchase-orders">
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div>
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="text-3xl font-bold tracking-tight">{order.orderNumber}</h1>
              <DistributorPurchaseOrderStatusBadge status={order.status} />
            </div>
            <p className="text-muted-foreground">
              Created {formatDateShort(order.createdAt)} · {order.distributorName}
            </p>
          </div>
        </div>
      </div>

      {/* Rejection banner */}
      {isPendingAck && rejectedEntry && (
        <div className="flex items-start gap-3 rounded-lg border border-orange-200 bg-orange-50 px-4 py-3">
          <AlertCircle className="h-4 w-4 text-orange-600 mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-medium text-orange-800">Order Rejected</p>
            <p className="text-sm text-orange-700 mt-0.5">
              Rejected by {rejectedEntry.performedByName ?? 'Unknown'}.
              {order.cancelReason ? ` Reason: ${order.cancelReason}` : ''}
            </p>
          </div>
        </div>
      )}

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-6 items-start">
        {/* Left column */}
        <div className="space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center gap-2">
                <div className="flex h-8 w-8 items-center justify-center rounded-md bg-blue-100 text-blue-600">
                  <ShoppingCart className="h-4 w-4" />
                </div>
                <div>
                  <CardTitle className="text-base">Order Items</CardTitle>
                  {lastEditEntry && (
                    <p className="text-xs text-muted-foreground mt-0.5">
                      Last edited {formatDateShort(lastEditEntry.performedAt)}
                    </p>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-sm border-collapse">
                  <thead>
                    <tr className="border-b bg-muted/40">
                      <th className="px-4 py-2.5 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide w-8">#</th>
                      <th className="px-4 py-2.5 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide">Product</th>
                      <th className="px-4 py-2.5 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide">Code</th>
                      <th className="px-4 py-2.5 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide">QTY</th>
                      <th className="px-4 py-2.5 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide">Unit Price</th>
                      <th className="px-4 py-2.5 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide">Disc%</th>
                      <th className="px-4 py-2.5 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide">Line Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {order.items.map((item, idx) => (
                      <tr key={item.id} className="hover:bg-muted/20">
                        <td className="px-4 py-3 text-muted-foreground text-xs">{idx + 1}</td>
                        <td className="px-4 py-3 font-medium">{item.productDescription}</td>
                        <td className="px-4 py-3">
                          <span className="font-mono text-xs bg-muted px-1.5 py-0.5 rounded text-muted-foreground">
                            {item.productCode}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right">{item.quantity}</td>
                        <td className="px-4 py-3 text-right tabular-nums">{formatCurrency(item.unitPrice)}</td>
                        <td className="px-4 py-3 text-right text-muted-foreground">{item.discount}%</td>
                        <td className="px-4 py-3 text-right font-semibold tabular-nums">{formatCurrency(item.lineTotal)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t">
                      <td colSpan={6} className="px-4 py-3 text-right text-sm font-bold">
                        Total (LKR)
                      </td>
                      <td className="px-4 py-3 text-right font-bold text-orange-600 tabular-nums">
                        {formatCurrency(order.totalAmount)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              {/* Subtotal summary */}
              <div className="border-t px-4 py-3 space-y-1">
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>Subtotal ({order.items.length} {order.items.length === 1 ? 'item' : 'items'})</span>
                  <span className="tabular-nums">{formatCurrency(subtotal)}</span>
                </div>
                <div className="flex justify-between text-sm font-bold">
                  <span>Total</span>
                  <span className="tabular-nums text-orange-600">{formatCurrency(order.totalAmount)}</span>
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
                <p className="text-sm text-muted-foreground whitespace-pre-wrap">{order.notes}</p>
              </CardContent>
            </Card>
          )}

          {/* Order History */}
          {order.history.length > 0 && (
            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <CardTitle className="text-base">Order History</CardTitle>
                </div>
                <p className="text-sm text-muted-foreground">Activity log for this order</p>
              </CardHeader>
              <CardContent>
                <HistoryTimeline history={order.history} products={allProducts ?? []} />
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right sidebar */}
        <div className="space-y-4 lg:sticky lg:top-6">
          {/* Order Info */}
          <Card>
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-sm">Order Info</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Order #</span>
                <span className="font-mono font-semibold">{order.orderNumber}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Status</span>
                <DistributorPurchaseOrderStatusBadge status={order.status} />
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Created</span>
                <span>{formatDateShort(order.createdAt)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Distributor</span>
                <span className="font-medium text-right max-w-[160px] truncate">{order.distributorName}</span>
              </div>
              <div className="flex justify-between border-t pt-3">
                <span className="font-semibold">Total</span>
                <span className="font-bold text-orange-600 tabular-nums">{formatCurrency(order.totalAmount)}</span>
              </div>
            </CardContent>
          </Card>

          {/* Rejection Details */}
          {isPendingAck && rejectedEntry && (
            <Card className="border-orange-200 bg-orange-50/30">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm text-orange-800">Rejection Details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <AuditRow label="Rejected By" value={rejectedEntry.performedByName ?? 'Unknown'} />
                <AuditRow label="Reason" value={order.cancelReason} />
                <AuditRow label="Rejected At" value={formatDate(rejectedEntry.performedAt)} />
              </CardContent>
            </Card>
          )}

          {/* Approval Progress */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-sm">Approval Progress</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <ApprovalProgress order={order} />
            </CardContent>
          </Card>

          {/* Actions */}
          <OrderActionsPanel order={order} />

          {/* Audit Trail */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-xs text-muted-foreground uppercase tracking-wide">Audit Trail</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <AuditRow label="Submitted At" value={formatDate(order.submittedAt)} />
              <AuditRow label="Rep Approved At" value={formatDate(order.repApprovedAt)} />
              <AuditRow label="Manager Approved At" value={formatDate(order.managerApprovedAt)} />
              <AuditRow label="Finalized At" value={formatDate(order.finalizedAt)} />
              <AuditRow label="Acknowledged At" value={formatDate(order.acknowledgedAt)} />
              <AuditRow label="Cancelled At" value={formatDate(order.cancelledAt)} />
              {order.cancelReason && order.status === PurchaseOrderStatus.Cancelled && (
                <AuditRow label="Cancel Reason" value={order.cancelReason} />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
