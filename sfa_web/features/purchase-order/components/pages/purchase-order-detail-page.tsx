'use client'

import { useState, useCallback, useEffect } from "react";
import { useRouter } from 'next/navigation'
import { useForm, useFieldArray, useWatch, Controller } from "react-hook-form";
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Pencil,
  CheckCircle,
  XCircle,
  AlertCircle,
  Clock,
  Circle,
  ChevronLeft,
  Activity,
  ShoppingCart,
  Plus,
  Trash2,
  Save,
  X,
  FileEdit,
} from "lucide-react";
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form'
import { useSession } from "next-auth/react";
import { PurchaseOrderStatusBadge } from '../purchase-order-status-badge'
import { PurchaseOrderDialogs } from '../dialogs/purchase-order-dialogs'
import { useAllActiveProducts } from "@/features/product/hooks/product.hooks";
import {
  usePurchaseOrder,
  useSubmitPurchaseOrder,
  useRepApprovePurchaseOrder,
  useApprovePurchaseOrder,
  useRejectPurchaseOrder,
  useAcknowledgePurchaseOrder,
  useFinalizePurchaseOrder,
  useCancelPurchaseOrder,
  useUpdatePurchaseOrder,
  useDefaultPricingStructure,
} from "../../hooks/purchase-order.hooks";
import { usePurchaseOrderDialogStore } from '../../store'
import {
  PurchaseOrderStatus,
  rejectPurchaseOrderSchema,
  updatePurchaseOrderSchema,
  type PurchaseOrderDto,
  type PurchaseOrderHistoryDto,
  type RejectPurchaseOrderInput,
  type UpdatePurchaseOrderInput,
} from "../../schema/purchase-order.schema";

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

const historyActionConfig: Record<
  string,
  { color: string; Icon: React.ElementType }
> = {
  Created: { color: "bg-gray-400", Icon: Circle },
  Submitted: { color: "bg-blue-500", Icon: Clock },
  RepApproved: { color: "bg-green-500", Icon: CheckCircle },
  ManagerApproved: { color: "bg-green-600", Icon: CheckCircle },
  Rejected: { color: "bg-red-500", Icon: XCircle },
  RejectionAcknowledged: { color: "bg-orange-500", Icon: AlertCircle },
  Cancelled: { color: "bg-red-600", Icon: XCircle },
  PendingDistributorAcknowledgement: {
    color: "bg-orange-400",
    Icon: AlertCircle,
  },
  PendingDistributorFinalization: { color: "bg-purple-500", Icon: Clock },
  Finalized: { color: "bg-emerald-600", Icon: CheckCircle },
  ItemsEdited: { color: "bg-indigo-500", Icon: FileEdit },
};

function historyActionLabel(action: string): string {
  const map: Record<string, string> = {
    Created: "Order Created",
    Submitted: "Submitted",
    RepApproved: "Approved by Rep",
    ManagerApproved: "Approved by Manager",
    Rejected: "Rejected",
    RejectionAcknowledged: "Rejection Acknowledged",
    Cancelled: "Cancelled",
    Finalized: "Finalized",
    PendingDistributorFinalization: "Sent for Finalization",
    PendingDistributorAcknowledgement: "Pending Distributor Acknowledgement",
    ItemsEdited: "Items Edited by Admin",
  };
  return map[action] ?? action
}

function HistoryTimeline({ history }: { history: PurchaseOrderHistoryDto[] }) {
  return (
    <div className="space-y-0">
      {history.map((entry, idx) => {
        const cfg = historyActionConfig[entry.action] ?? { color: 'bg-gray-300', Icon: Circle }
        const isLast = idx === history.length - 1

        return (
          <div key={entry.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div
                className={`w-3 h-3 rounded-full mt-1 shrink-0 ${cfg.color} ${isLast ? "ring-2 ring-offset-1 ring-orange-400" : ""}`}
              />
              {!isLast && <div className="w-px flex-1 bg-border mt-1" />}
            </div>
            <div className="pb-4">
              <div className="flex items-center gap-2">
                <p className="text-sm font-medium leading-tight">
                  {entry.action === "Rejected"
                    ? `Rejected by ${entry.performedByName ?? "Unknown"}`
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
              <p className="text-xs text-muted-foreground mt-0.5">
                {formatDate(entry.performedAt)}
              </p>
              {entry.notes && (
                <p className="text-xs text-muted-foreground mt-1 italic bg-muted/50 px-2 py-1 rounded">
                  {entry.action === "ItemsEdited"
                    ? "Admin updated order items"
                    : `Reason: ${entry.notes}`}
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  )
}

// ── Audit row helper ───────────────────────────────────────────────────────

function AuditRow({ label, value }: { label: string; value: string | null | undefined }) {
  if (!value) return null
  return (
    <div className="flex justify-between items-start gap-4">
      <span className="text-muted-foreground text-sm shrink-0">{label}</span>
      <span className="text-sm font-medium text-right">{value}</span>
    </div>
  )
}

// ── Inline reason form ────────────────────────────────────────────────────

interface InlineReasonFormProps {
  label: string
  isPending: boolean
  onSubmit: (data: RejectPurchaseOrderInput) => void
  onCancel: () => void
}

function InlineReasonForm({ label, isPending, onSubmit, onCancel }: InlineReasonFormProps) {
  const form = useForm<RejectPurchaseOrderInput>({
    resolver: zodResolver(rejectPurchaseOrderSchema),
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

// ── Approval Progress ──────────────────────────────────────────────────────

type ProgressStep = {
  label: string
  subLabel: string
  state: 'done' | 'active' | 'pending'
}

function getApprovalSteps(order: PurchaseOrderDto): ProgressStep[] {
  const status = order.status

  const isDone = (s: number) => status > s && status !== PurchaseOrderStatus.Cancelled
  const isActive = (s: number) => status === s

  const createdDone = true
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
        ? `${formatDateShort(createdEntry.performedAt)} — ${createdEntry.performedByName ?? 'Unknown'}`
        : 'Created',
      state: createdDone ? 'done' : 'active',
    },
    {
      label: 'Rep Approval',
      subLabel: repDone
        ? `Approved${order.repApprovedAt ? ` — ${formatDateShort(order.repApprovedAt)}` : ''}`
        : repActive
        ? 'Awaiting your action'
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
        ? 'Awaiting manager action'
        : 'Pending rep action',
      state: managerDone ? 'done' : managerActive ? 'active' : 'pending',
    },
    {
      label: 'Finalization',
      subLabel: finalizationDone
        ? `Finalized${order.finalizedAt ? ` — ${formatDateShort(order.finalizedAt)}` : ''}`
        : finalizationActive
        ? 'Awaiting distributor'
        : 'Pending approval',
      state: finalizationDone ? 'done' : finalizationActive ? 'active' : 'pending',
    },
  ]
}

function ApprovalProgress({ order }: { order: PurchaseOrderDto }) {
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
              <div className={`w-px h-6 mt-1 ${step.state === 'done' ? 'bg-orange-300' : 'bg-muted-foreground/20'}`} />
            )}
          </div>
          <div className="pb-1">
            <p className={`text-sm font-medium leading-tight ${step.state === 'pending' ? 'text-muted-foreground' : ''}`}>
              {step.label}
            </p>
            <p className="text-xs text-muted-foreground mt-0.5">{step.subLabel}</p>
          </div>
        </div>
      ))}
    </div>
  )
}

// ── Pending Action Banner ──────────────────────────────────────────────────

function getPendingBannerMessage(status: number): string | null {
  switch (status) {
    case PurchaseOrderStatus.PendingRepApproval:
      return "This order is awaiting your approval as Sales Rep. Review the items below before approving or rejecting.";
    case PurchaseOrderStatus.PendingManagerApproval:
      return "This order is awaiting manager approval. Review the items below before approving or rejecting.";
    case PurchaseOrderStatus.PendingDistributorFinalization:
      return "This order has been approved and is awaiting distributor finalization.";
    case PurchaseOrderStatus.PendingDistributorAcknowledgement:
      return "This order was rejected. The distributor must acknowledge the rejection before it can be cancelled.";
    default:
      return null;
  }
}

function getStepLabel(status: number): string {
  switch (status) {
    case PurchaseOrderStatus.PendingRepApproval:
      return 'Step 2 of 4'
    case PurchaseOrderStatus.PendingManagerApproval:
      return 'Step 3 of 4'
    case PurchaseOrderStatus.PendingDistributorFinalization:
      return 'Step 4 of 4'
    default:
      return ''
  }
}

// ── Admin Inline Items Editor ──────────────────────────────────────────────

interface AdminItemsEditorProps {
  order: PurchaseOrderDto
  onClose: () => void
}

function AdminItemsEditor({ order, onClose }: AdminItemsEditorProps) {
  const { data: products } = useAllActiveProducts()
  const { data: pricing } = useDefaultPricingStructure()
  const { mutate: updateOrder, isPending } = useUpdatePurchaseOrder(order.id)

  const form = useForm<UpdatePurchaseOrderInput>({
    resolver: zodResolver(updatePurchaseOrderSchema),
    defaultValues: {
      notes: order.notes ?? '',
      items: order.items.map((i) => ({
        productId: i.productId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        discount: i.discount,
      })),
    },
  })

  const { fields, append, remove } = useFieldArray({ control: form.control, name: 'items' })
  const watchedItems = useWatch({ control: form.control, name: 'items' })

  // When pricing loads, back-fill unit prices for all existing items
  useEffect(() => {
    if (!pricing?.items) return
    order.items.forEach((item, index) => {
      const entry = pricing.items.find((p) => p.productId === item.productId)
      const price = entry?.dealerCasePrice ?? entry?.dealerPackPrice ?? item.unitPrice
      form.setValue(`items.${index}.unitPrice`, price)
    })
  }, [pricing])  // eslint-disable-line react-hooks/exhaustive-deps

  const getPricingEntry = useCallback(
    (productId: number | undefined) => {
      if (!pricing?.items || !productId) return null
      return pricing.items.find((i) => i.productId === productId) ?? null
    },
    [pricing]
  )

  const getUnitPrice = useCallback(
    (productId: number | undefined): number => {
      const entry = getPricingEntry(productId)
      return entry?.dealerCasePrice ?? entry?.dealerPackPrice ?? 0
    },
    [getPricingEntry]
  )

  const handleProductChange = (index: number, productId: number) => {
    const price = getUnitPrice(productId)
    form.setValue(`items.${index}.productId`, productId)
    form.setValue(`items.${index}.unitPrice`, price)
  }

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const onSubmit = (data: UpdatePurchaseOrderInput) => {
    updateOrder(
      { ...data, items: data.items.filter((i) => i.productId > 0) },
      { onSuccess: onClose }
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        {/* Column headers */}
        <div className="grid grid-cols-[1fr_72px_120px_100px_32px] gap-2 px-6 py-2 border-b bg-indigo-50/60">
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Product</span>
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-center">QTY</span>
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Unit Price</span>
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Line Total</span>
          <span />
        </div>

        <div className="divide-y">
          {fields.map((field, index) => {
            const watchedItem = watchedItems[index]
            const pid = watchedItem?.productId
            const entry = getPricingEntry(pid)
            const hasCasePrice = entry?.dealerCasePrice != null
            const hasPackPrice = entry?.dealerPackPrice != null
            const price = entry?.dealerCasePrice ?? entry?.dealerPackPrice ?? null
            const priceLabel = hasCasePrice ? 'Case' : hasPackPrice ? 'Pack' : null
            const lineTotal =
              (watchedItem?.unitPrice ?? 0) *
              (watchedItem?.quantity ?? 0) *
              (1 - (watchedItem?.discount ?? 0) / 100)

            return (
              <div key={field.id} className="grid grid-cols-[1fr_72px_120px_100px_32px] gap-2 px-6 py-2.5 items-center bg-indigo-50/20">
                {/* Product select */}
                <Controller
                  control={form.control}
                  name={`items.${index}.productId`}
                  render={({ field: f }) => (
                    <Select
                      value={f.value ? String(f.value) : ''}
                      onValueChange={(v) => handleProductChange(index, Number(v))}
                    >
                      <SelectTrigger className="h-8 text-sm">
                        <SelectValue placeholder="Select product..." />
                      </SelectTrigger>
                      <SelectContent>
                        {products?.map((p) => (
                          <SelectItem key={p.id} value={String(p.id)}>
                            <span className="font-medium">{p.itemDescription}</span>
                            <span className="ml-2 text-xs text-muted-foreground font-mono">{p.code}</span>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />

                {/* Quantity */}
                <Controller
                  control={form.control}
                  name={`items.${index}.quantity`}
                  render={({ field: f }) => (
                    <Input
                      type="number"
                      min={1}
                      className="h-8 text-sm text-center"
                      value={f.value}
                      onChange={(e) => f.onChange(Number(e.target.value))}
                    />
                  )}
                />

                {/* Unit Price — read-only, auto-filled from pricing structure */}
                <div className="flex flex-col items-end gap-0.5">
                  {pid && !entry ? (
                    <span className="text-xs text-amber-600 font-medium">No pricing</span>
                  ) : price != null ? (
                    <>
                      <span className="text-sm font-medium tabular-nums">{formatCurrency(price)}</span>
                      {priceLabel && (
                        <span className="text-[10px] text-muted-foreground leading-none">{priceLabel} price</span>
                      )}
                    </>
                  ) : (
                    <span className="text-sm text-muted-foreground">—</span>
                  )}
                </div>

                {/* Line Total */}
                <span className="text-sm font-semibold text-right tabular-nums">
                  {formatCurrency(lineTotal)}
                </span>

                {/* Delete */}
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-destructive hover:text-destructive"
                  onClick={() => remove(index)}
                  disabled={fields.length === 1}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            )
          })}
        </div>

        {/* Totals + actions */}
        <div className="border-t px-6 py-3 bg-indigo-50/30 space-y-3">
          <div className="flex justify-between text-sm font-bold">
            <span>Subtotal (LKR)</span>
            <span className="tabular-nums text-orange-600">{formatCurrency(subtotal)}</span>
          </div>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="flex-1"
              onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })}
            >
              <Plus className="h-3.5 w-3.5 mr-1" />
              Add Item
            </Button>
            <Button type="submit" size="sm" className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white" disabled={isPending}>
              {isPending ? <Spinner className="mr-1.5 h-3 w-3" /> : <Save className="h-3.5 w-3.5 mr-1.5" />}
              Save Changes
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={onClose} disabled={isPending}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </form>
    </Form>
  )
}

// ── Order Actions Panel ────────────────────────────────────────────────────

function OrderActionsPanel({
  order,
  isAdminEditing,
  onToggleAdminEdit,
}: {
  order: PurchaseOrderDto;
  isAdminEditing: boolean;
  onToggleAdminEdit: () => void;
}) {
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [showCancelForm, setShowCancelForm] = useState(false);

  const store = usePurchaseOrderDialogStore();
  const { mutate: submit, isPending: isSubmitting } = useSubmitPurchaseOrder();
  const { mutate: repApprove, isPending: isRepApproving } =
    useRepApprovePurchaseOrder();
  const { mutate: approve, isPending: isApproving } = useApprovePurchaseOrder();
  const { mutate: reject, isPending: isRejecting } = useRejectPurchaseOrder(
    order.id,
  );
  const { mutate: acknowledge, isPending: isAcknowledging } =
    useAcknowledgePurchaseOrder();
  const { mutate: finalize, isPending: isFinalizing } =
    useFinalizePurchaseOrder();
  const { mutate: cancel, isPending: isCancelling } = useCancelPurchaseOrder(
    order.id,
  );

  const { data: session } = useSession();
  const isAdmin = session?.user?.role === "Admin";

  const status = order.status;
  const isActiveOrder =
    status !== PurchaseOrderStatus.Finalized &&
    status !== PurchaseOrderStatus.Cancelled;

  return (
    <div className="space-y-2">
      {/* Admin edit items button — visible at all active statuses */}
      {isAdmin && isActiveOrder && (
        <Button
          variant={isAdminEditing ? "default" : "outline"}
          size="sm"
          className={`w-full gap-2 ${isAdminEditing ? "bg-indigo-600 hover:bg-indigo-700 text-white" : ""}`}
          onClick={onToggleAdminEdit}
        >
          <Pencil className="h-3.5 w-3.5" />
          {isAdminEditing ? "Cancel Item Edit" : "Edit Items (Admin)"}
        </Button>
      )}

      {/* Draft */}
      {status === PurchaseOrderStatus.Draft && (
        <>
          <Button
            className="w-full bg-orange-600 hover:bg-orange-700 text-white"
            onClick={() => store.openSubmit(order.id)}
            disabled={isSubmitting}
          >
            {isSubmitting && <Spinner className="mr-2" />}
            Submit for Approval
          </Button>
          {!showCancelForm ? (
            <Button
              variant="ghost"
              className="w-full text-destructive hover:text-destructive"
              onClick={() => setShowCancelForm(true)}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Cancel Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Cancel Order"
              isPending={isCancelling}
              onSubmit={(data) => {
                cancel(data);
                setShowCancelForm(false);
              }}
              onCancel={() => setShowCancelForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Rep Approval */}
      {status === PurchaseOrderStatus.PendingRepApproval && (
        <>
          <Button
            className="w-full bg-orange-600 hover:bg-orange-700 text-white"
            onClick={() => store.openRepApprove(order.id)}
            disabled={isRepApproving}
          >
            {isRepApproving && <Spinner className="mr-2" />}
            <CheckCircle className="h-4 w-4 mr-2" />
            Approve (Rep)
          </Button>
          {!showRejectForm ? (
            <Button
              variant="destructive"
              className="w-full text-destructive hover:text-destructive border"
              onClick={() => setShowRejectForm(true)}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Reject Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Reject Order"
              isPending={isRejecting}
              onSubmit={(data) => {
                reject(data);
                setShowRejectForm(false);
              }}
              onCancel={() => setShowRejectForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Manager Approval */}
      {status === PurchaseOrderStatus.PendingManagerApproval && (
        <>
          <Button
            className="w-full bg-orange-600 hover:bg-orange-700 text-white"
            onClick={() => store.openApprove(order.id)}
            disabled={isApproving}
          >
            {isApproving && <Spinner className="mr-2" />}
            <CheckCircle className="h-4 w-4 mr-2" />
            Approve
          </Button>
          {!showRejectForm ? (
            <Button
              variant="destructive"
              className="w-full text-destructive hover:text-destructive border"
              onClick={() => setShowRejectForm(true)}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Reject Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Reject Order"
              isPending={isRejecting}
              onSubmit={(data) => {
                reject(data);
                setShowRejectForm(false);
              }}
              onCancel={() => setShowRejectForm(false)}
            />
          )}
        </>
      )}

      {/* Pending Distributor Acknowledgement */}
      {status === PurchaseOrderStatus.PendingDistributorAcknowledgement && (
        <>
          <Button
            className="w-full bg-orange-600 hover:bg-orange-700 text-white"
            onClick={() => store.openAcknowledge(order.id)}
            disabled={isAcknowledging}
          >
            {isAcknowledging && <Spinner className="mr-2" />}
            Acknowledge Rejection
          </Button>
          {!showCancelForm ? (
            <Button
              variant="ghost"
              className="w-full text-destructive hover:text-destructive"
              onClick={() => setShowCancelForm(true)}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Cancel Order
            </Button>
          ) : (
            <InlineReasonForm
              label="Cancel Order"
              isPending={isCancelling}
              onSubmit={(data) => {
                cancel(data);
                setShowCancelForm(false);
              }}
              onCancel={() => setShowCancelForm(false)}
            />
          )}
          <p className="text-xs text-muted-foreground text-center">
            Acknowledging confirms you have seen this rejection. Order will be
            moved to Cancelled.
          </p>
        </>
      )}

      {/* Pending Distributor Finalization */}
      {status === PurchaseOrderStatus.PendingDistributorFinalization && (
        <Button
          className="w-full bg-orange-600 hover:bg-orange-700 text-white"
          onClick={() => store.openFinalize(order.id)}
          disabled={isFinalizing}
        >
          {isFinalizing && <Spinner className="mr-2" />}
          Finalize Order
        </Button>
      )}

      {/* Terminal states */}
      {(status === PurchaseOrderStatus.Finalized ||
        status === PurchaseOrderStatus.Cancelled) && (
        <p className="text-sm text-center text-muted-foreground py-2">
          {status === PurchaseOrderStatus.Finalized
            ? "This order has been finalized."
            : "This order has been cancelled."}
        </p>
      )}
    </div>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────

interface PurchaseOrderDetailPageProps {
  orderId: number
}

export function PurchaseOrderDetailPage({ orderId }: PurchaseOrderDetailPageProps) {
  const router = useRouter()
  const { data: order, isLoading, isError } = usePurchaseOrder(orderId)
  const [isAdminEditing, setIsAdminEditing] = useState(false);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6">
        {/* Header skeleton */}
        <div className="bg-muted/90 p-10 rounded-lg flex items-center justify-between">
          <div className="space-y-2">
            <div className="flex items-center gap-3">
              <Skeleton className="h-9 w-48" />
              <Skeleton className="h-5 w-32 rounded-full" />
            </div>
            <Skeleton className="h-4 w-72" />
          </div>
          <Skeleton className="h-9 w-20 rounded-md" />
        </div>

        {/* Two-column skeleton */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-6">
          {/* Left */}
          <div className="space-y-6">
            <Card>
              <CardHeader className="pb-3">
                <Skeleton className="h-5 w-28" />
                <Skeleton className="h-4 w-40 mt-1" />
              </CardHeader>
              <CardContent className="p-0">
                <div className="border-b bg-muted/40 px-6 py-3 flex gap-8">
                  {[120, 60, 40, 80, 80].map((w, i) => (
                    <Skeleton key={i} className="h-3" style={{ width: w }} />
                  ))}
                </div>
                {[1, 2, 3].map((i) => (
                  <div key={i} className="flex gap-8 px-6 py-4 border-b">
                    {[140, 48, 32, 72, 72].map((w, j) => (
                      <Skeleton key={j} className="h-4" style={{ width: w }} />
                    ))}
                  </div>
                ))}
                <div className="px-6 py-4 space-y-2">
                  {[1, 2, 3].map((i) => (
                    <div key={i} className="flex justify-between">
                      <Skeleton className="h-4 w-24" />
                      <Skeleton className="h-4 w-20" />
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-4 w-48 mt-1" />
              </CardHeader>
              <CardContent className="space-y-4">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="flex gap-3">
                    <Skeleton className="h-3 w-3 rounded-full mt-1 shrink-0" />
                    <div className="space-y-1.5 pb-3">
                      <Skeleton className="h-4 w-48" />
                      <Skeleton className="h-3 w-32" />
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>

          {/* Right sidebar */}
          <div className="space-y-4">
            <Card>
              <CardHeader className="pb-2">
                <Skeleton className="h-5 w-24" />
              </CardHeader>
              <CardContent className="space-y-3">
                {[1, 2, 3, 4, 5].map((i) => (
                  <div key={i} className="flex justify-between">
                    <Skeleton className="h-4 w-20" />
                    <Skeleton className="h-4 w-28" />
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <Skeleton className="h-5 w-36" />
              </CardHeader>
              <CardContent className="space-y-4">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="flex gap-3 items-start">
                    <Skeleton className="h-6 w-6 rounded-full shrink-0" />
                    <div className="space-y-1.5">
                      <Skeleton className="h-4 w-28" />
                      <Skeleton className="h-3 w-40" />
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            <Skeleton className="h-11 w-full rounded-md" />
            <Skeleton className="h-9 w-full rounded-md" />
          </div>
        </div>
      </div>
    )
  }

  if (isError || !order) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <div className="bg-muted/90 p-10 rounded-lg">
          <p className="text-muted-foreground">Order not found.</p>
        </div>
      </div>
    )
  }

  const subtotal = order.items.reduce((s, i) => s + i.lineTotal, 0)
  const tax = order.totalAmount - subtotal
  const rejectedEntry = order.history.findLast((h) => h.action === 'Rejected')
  const pendingMessage = getPendingBannerMessage(order.status);
  const stepLabel = getStepLabel(order.status)
  const lastEditEntry = order.history.findLast(
    (h) => h.action === "ItemsEdited",
  );

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6 lg:w-3/4 mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-muted/90 p-6 sm:p-10 rounded-lg">
        <div>
          <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-1">
            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">
              {order.orderNumber}
            </h1>
            <PurchaseOrderStatusBadge status={order.status} />
          </div>
          <p className="text-muted-foreground text-sm sm:text-base">
            Purchase order · {order.distributorName} · Created{" "}
            {formatDateShort(order.createdAt)}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => router.push('/purchase-orders')}
          className="gap-2 self-start sm:self-auto"
        >
          <ChevronLeft className="h-4 w-4" />
          Back
        </Button>
      </div>

      {/* Pending action banner */}
      {pendingMessage && (
        <div className="flex items-start justify-between gap-4 bg-orange-50 border border-orange-200 rounded-lg px-4 py-3">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-4 w-4 text-orange-600 mt-0.5 shrink-0" />
            <p className="text-sm text-orange-800">{pendingMessage}</p>
          </div>
          {stepLabel && (
            <span className="text-sm font-medium text-orange-600 shrink-0">
              {stepLabel}
            </span>
          )}
        </div>
      )}

      {/* Admin edit banner */}
      {isAdminEditing && (
        <div className="flex items-center gap-3 bg-indigo-50 border border-indigo-200 rounded-lg px-4 py-3">
          <FileEdit className="h-4 w-4 text-indigo-600 shrink-0" />
          <p className="text-sm text-indigo-800 font-medium">
            Admin edit mode — changes are recorded in the audit log.
          </p>
        </div>
      )}

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-6 items-start">
        {/* Left column */}
        <div className="space-y-6">
          {/* Order Items */}
          <Card
            className={
              isAdminEditing ? "ring-2 ring-indigo-400 ring-offset-1" : ""
            }
          >
            <CardHeader className="pb-3">
              <div className="flex items-center gap-2">
                <ShoppingCart className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-base">Order Items</CardTitle>
                {lastEditEntry && (
                  <span className="ml-auto text-[10px] font-medium bg-indigo-100 text-indigo-700 px-1.5 py-0.5 rounded">
                    Edited {formatDateShort(lastEditEntry.performedAt)}
                  </span>
                )}
              </div>
              <p className="text-sm text-muted-foreground">
                {order.items.length}{" "}
                {order.items.length === 1 ? "product" : "products"} ·{" "}
                {formatCurrency(order.totalAmount)} total
              </p>
            </CardHeader>
            <CardContent className="p-0">
              {isAdminEditing ? (
                <AdminItemsEditor
                  order={order}
                  onClose={() => setIsAdminEditing(false)}
                />
              ) : (
                <>
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-130 text-sm">
                      <thead>
                        <tr className="border-b bg-muted/40">
                          <th className="text-left px-6 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">
                            Product
                          </th>
                          <th className="text-left px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">
                            SKU
                          </th>
                          <th className="text-right px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">
                            QTY
                          </th>
                          <th className="text-right px-4 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">
                            Unit Price
                          </th>
                          <th className="text-right px-6 py-3 font-medium text-muted-foreground text-xs uppercase tracking-wide">
                            Line Total
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {order.items.map((item) => (
                          <tr key={item.id} className="hover:bg-muted/20">
                            <td className="px-6 py-3 font-medium">
                              {item.productDescription}
                            </td>
                            <td className="px-4 py-3">
                              <span className="font-mono text-xs bg-muted px-1.5 py-0.5 rounded text-muted-foreground">
                                {item.productCode}
                              </span>
                            </td>
                            <td className="px-4 py-3 text-right">
                              {item.quantity}
                            </td>
                            <td className="px-4 py-3 text-right">
                              {formatCurrency(item.unitPrice)}
                            </td>
                            <td className="px-6 py-3 text-right font-semibold tabular-nums">
                              {formatCurrency(item.lineTotal)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {/* Totals */}
                  <div className="border-t px-6 py-4 space-y-1.5">
                    <div className="flex justify-between text-sm text-muted-foreground">
                      <span>
                        Subtotal ({order.items.length}{" "}
                        {order.items.length === 1 ? "item" : "items"})
                      </span>
                      <span className="tabular-nums">
                        {formatCurrency(subtotal)}
                      </span>
                    </div>
                    <div className="flex justify-between text-sm text-muted-foreground">
                      <span>Discount</span>
                      <span className="tabular-nums">—</span>
                    </div>
                    {tax > 0 ? (
                      <div className="flex justify-between text-sm text-muted-foreground">
                        <span>Tax (8%)</span>
                        <span className="tabular-nums">
                          {formatCurrency(tax)}
                        </span>
                      </div>
                    ) : (
                      <div className="flex justify-between text-sm text-muted-foreground">
                        <span>Tax (0%)</span>
                        <span className="tabular-nums">
                          {formatCurrency(0)}
                        </span>
                      </div>
                    )}
                    <div className="flex justify-between text-base font-bold pt-2 border-t mt-1">
                      <span>Total (LKR)</span>
                      <span className="tabular-nums text-orange-600">
                        {formatCurrency(order.totalAmount)}
                      </span>
                    </div>
                  </div>
                </>
              )}
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
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <CardTitle className="text-base">Order History</CardTitle>
                </div>
                <p className="text-sm text-muted-foreground">
                  Activity log for this order
                </p>
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
              <div className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-base">Order Info</CardTitle>
              </div>
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
                <span className="text-muted-foreground">Created by</span>
                <span className="font-medium">
                  {order.history.find((h) => h.action === "Created")
                    ?.performedByName ?? "—"}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Distributor</span>
                <span className="font-medium text-right">
                  {order.distributorName}
                </span>
              </div>

              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Status</span>
                <PurchaseOrderStatusBadge status={order.status} />
              </div>
              <div className="flex justify-between pt-1 border-t">
                <span className="text-muted-foreground">Total</span>
                <span className="font-bold text-orange-600 tabular-nums">
                  {formatCurrency(order.totalAmount)}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Rejection Details */}
          {order.status ===
            PurchaseOrderStatus.PendingDistributorAcknowledgement &&
            rejectedEntry && (
              <Card className="border-orange-200 bg-orange-50/30">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base text-orange-800">
                    Rejection Details
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  <AuditRow
                    label="Rejected By"
                    value={`${rejectedEntry.performedByName ?? "Unknown"}${order.managerApprovedBy ? " (Manager)" : " (Rep)"}`}
                  />
                  <AuditRow label="Reason" value={order.cancelReason} />
                  <AuditRow
                    label="Rejected At"
                    value={formatDate(rejectedEntry.performedAt)}
                  />
                </CardContent>
              </Card>
            )}

          {/* Approval Progress */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center gap-2">
                <Activity className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-base">Approval Progress</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <ApprovalProgress order={order} />
            </CardContent>
          </Card>

          {/* Actions */}
          <div className="space-y-2">
            <OrderActionsPanel
              order={order}
              isAdminEditing={isAdminEditing}
              onToggleAdminEdit={() => setIsAdminEditing((v) => !v)}
            />
          </div>

          {/* Audit Trail */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm text-muted-foreground">
                Audit Trail
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-xs">
              <AuditRow
                label="Submitted At"
                value={formatDate(order.submittedAt)}
              />
              <AuditRow
                label="Rep Approved At"
                value={formatDate(order.repApprovedAt)}
              />
              <AuditRow
                label="Manager Approved At"
                value={formatDate(order.managerApprovedAt)}
              />
              <AuditRow
                label="Finalized At"
                value={formatDate(order.finalizedAt)}
              />
              <AuditRow
                label="Acknowledged At"
                value={formatDate(order.acknowledgedAt)}
              />
              <AuditRow
                label="Cancelled At"
                value={formatDate(order.cancelledAt)}
              />
              {lastEditEntry && (
                <AuditRow
                  label="Last Edited At"
                  value={`${formatDate(lastEditEntry.performedAt)} by ${lastEditEntry.performedByName ?? "Admin"}`}
                />
              )}
              {order.cancelReason &&
                order.status === PurchaseOrderStatus.Cancelled && (
                  <AuditRow label="Cancel Reason" value={order.cancelReason} />
                )}
            </CardContent>
          </Card>
        </div>
      </div>

      <PurchaseOrderDialogs />
    </div>
  );
}
