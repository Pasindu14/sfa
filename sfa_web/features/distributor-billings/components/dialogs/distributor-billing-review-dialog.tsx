'use client'

import { useState, useEffect, useMemo } from 'react'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
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
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Separator } from '@/components/ui/separator'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CheckCircle2, XCircle, Loader2, Save, Minus, Plus, Undo2 } from 'lucide-react'
import {
  useApproveBilling,
  useRejectBilling,
  useUpdatePaymentType,
  useAdjustBillingItems,
  useMyBillingDetail,
} from '../../hooks/distributor-billing.hooks'
import type { DistributorBillingListItem, BillingLineItem } from '../../schema/distributor-billing.schema'
import { formatColombo } from '@/lib/utils/datetime'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

/** Mirrors the server's per-line math (BillingService.ApplyLineMath) so the preview matches. */
function lineTotal(quantity: number, unitPrice: number, discountRate: number, isFreeIssue: boolean) {
  const qty = Number.isFinite(quantity) ? quantity : 0
  if (isFreeIssue) return round2(qty * unitPrice)
  const discount = round2((qty * unitPrice * discountRate) / 100)
  return round2(qty * unitPrice - discount)
}

function round2(n: number) {
  return Math.round((n + Number.EPSILON) * 100) / 100
}

/** Only lines the rep entered as Sale or Free Issue can be adjusted. */
function isAdjustable(item: BillingLineItem) {
  return item.source === 'SalesRep'
    && (item.billingItemType === 'Sale' || item.billingItemType === 'FreeIssue')
}

interface AdjustFormValues {
  items: { billingItemId: number; quantity: number }[]
  note: string
}

interface Props {
  billing: DistributorBillingListItem | null
  onClose: () => void
}

export function DistributorBillingReviewDialog({ billing, onClose }: Props) {
  const [showApproveConfirm, setShowApproveConfirm] = useState(false)
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [rejectReason, setRejectReason] = useState('')
  const [selectedPaymentType, setSelectedPaymentType] = useState<'Cash' | 'Credit'>(billing?.paymentType ?? 'Cash')
  const [savedPaymentType, setSavedPaymentType] = useState<'Cash' | 'Credit'>(billing?.paymentType ?? 'Cash')

  const { data: detail, isLoading: detailLoading } = useMyBillingDetail(billing?.id ?? null)

  const approveMutation = useApproveBilling(onClose)
  const rejectMutation = useRejectBilling(onClose)
  const paymentTypeMutation = useUpdatePaymentType()
  const adjustMutation = useAdjustBillingItems()

  const form = useForm<AdjustFormValues>({ defaultValues: { items: [], note: '' } })
  const { fields } = useFieldArray({ control: form.control, name: 'items' })
  const watchedItems = useWatch({ control: form.control, name: 'items' })

  // Adjustable lines only — Return lines and previously generated Distributor Return lines are
  // shown read-only below the editable table.
  const adjustableItems = useMemo(
    () => (detail?.items ?? []).filter(isAdjustable),
    [detail],
  )
  const readOnlyItems = useMemo(
    () => (detail?.items ?? []).filter((i) => !isAdjustable(i)),
    [detail],
  )

  // Hydrate the editable rows once the detail lands, and again after a successful adjustment
  // (the mutation invalidates the detail query, so `detail` arrives with the new quantities).
  useEffect(() => {
    if (!detail) return
    form.reset({
      items: detail.items.filter(isAdjustable).map((i) => ({ billingItemId: i.id, quantity: i.quantity })),
      note: '',
    })
    // `form` is stable across renders (react-hook-form), so this re-runs only when the detail changes.
  }, [detail, form])

  useEffect(() => {
    if (billing) {
      setSelectedPaymentType(billing.paymentType)
      setSavedPaymentType(billing.paymentType)
    }
  }, [billing?.id])

  const paymentTypeChanged = selectedPaymentType !== savedPaymentType

  const changedLines = useMemo(() => {
    if (!watchedItems) return []
    return watchedItems.filter((row, idx) => {
      const original = adjustableItems[idx]
      return original !== undefined
        && Number.isFinite(row?.quantity)
        && row.quantity !== original.quantity
    })
  }, [watchedItems, adjustableItems])

  const hasInvalidQuantity = useMemo(() => {
    if (!watchedItems) return false
    return watchedItems.some((row, idx) => {
      const original = adjustableItems[idx]
      if (!original) return false
      const q = row?.quantity
      return !Number.isFinite(q) || q < 0 || q > original.quantity
    })
  }, [watchedItems, adjustableItems])

  // Preview of the payable total after the pending edits — mirrors the server rollup: only Sale
  // lines feed the sub-total, and Distributor Returns are never subtracted.
  const previewTotal = useMemo(() => {
    if (!detail) return 0
    let subTotal = 0
    let marketReturns = 0

    detail.items.forEach((item) => {
      const editedIdx = adjustableItems.findIndex((a) => a.id === item.id)
      const quantity = editedIdx >= 0 && Number.isFinite(watchedItems?.[editedIdx]?.quantity)
        ? watchedItems[editedIdx].quantity
        : item.quantity

      if (item.billingItemType === 'Sale') {
        subTotal += lineTotal(quantity, item.unitPrice, item.discountRate, false)
      } else if (item.billingItemType === 'Return' && item.returnType === 'MarketResell') {
        marketReturns += item.totalPrice
      }
    })

    const billDiscount = round2((subTotal * detail.billDiscountRate) / 100)
    return round2(subTotal - billDiscount - marketReturns)
  }, [detail, adjustableItems, watchedItems])

  const totalReturning = useMemo(
    () => changedLines.reduce((sum, row) => {
      const original = adjustableItems.find((a) => a.id === row.billingItemId)
      return original ? sum + (original.quantity - row.quantity) : sum
    }, 0),
    [changedLines, adjustableItems],
  )

  const busy = approveMutation.isPending || rejectMutation.isPending || adjustMutation.isPending

  function handleClose() {
    setShowRejectForm(false)
    setRejectReason('')
    form.reset({ items: [], note: '' })
    onClose()
  }

  function handleSaveAdjustments() {
    if (!billing) return
    adjustMutation.mutate({
      id: billing.id,
      input: {
        items: changedLines.map((row) => ({ billingItemId: row.billingItemId, quantity: row.quantity })),
        note: form.getValues('note') || undefined,
      },
    })
  }

  return (
    <>
      <Dialog open={billing !== null} onOpenChange={(v) => { if (!v) handleClose() }}>
        {/* The base DialogContent sets `sm:max-w-md`; an unprefixed `max-w-*` does not override a
            breakpoint-prefixed one, so the width has to be set at the same breakpoint with `!`. */}
        <DialogContent className="w-[95vw] sm:w-[90vw]! sm:max-w-4xl!">
          <DialogHeader>
            <DialogTitle>Review Billing</DialogTitle>
            {billing && (
              <DialogDescription>
                Billing <span className="font-mono font-semibold">{billing.billingNumber}</span>
                {' — reduce a quantity if the outlet took less than billed, then approve.'}
              </DialogDescription>
            )}
          </DialogHeader>

          {billing && (
            <div className="space-y-4">
              <div className="rounded-lg border bg-muted/30 p-4 space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Outlet</span>
                  <span className="font-medium">{billing.outletName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Sales Rep</span>
                  <span className="font-medium">{billing.salesRepName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Date</span>
                  <span className="font-medium">
                    {formatColombo(billing.billingDate, 'd MMM yyyy')}
                  </span>
                </div>
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="text-muted-foreground shrink-0">Payment</span>
                  <div className="flex items-center gap-2 min-w-0">
                    <Select
                      value={selectedPaymentType}
                      onValueChange={(v) => setSelectedPaymentType(v as 'Cash' | 'Credit')}
                      disabled={paymentTypeMutation.isPending}
                    >
                      <SelectTrigger className="h-7 w-28 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Cash">Cash</SelectItem>
                        <SelectItem value="Credit">Credit</SelectItem>
                      </SelectContent>
                    </Select>
                    {paymentTypeChanged && (
                      <Button
                        size="sm"
                        className="h-7 gap-1.5 text-xs"
                        disabled={paymentTypeMutation.isPending}
                        onClick={() => paymentTypeMutation.mutate(
                          { id: billing.id, paymentType: selectedPaymentType },
                          { onSuccess: () => setSavedPaymentType(selectedPaymentType) },
                        )}
                      >
                        {paymentTypeMutation.isPending
                          ? <Loader2 className="h-3 w-3 animate-spin" />
                          : <Save className="h-3 w-3" />
                        }
                        Save
                      </Button>
                    )}
                  </div>
                </div>
                <Separator />
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Total Amount</span>
                  <span className="tabular-nums">
                    {changedLines.length > 0 ? (
                      <>
                        <span className="text-muted-foreground line-through mr-2">
                          {formatCurrency(detail?.totalAmount ?? billing.totalAmount)}
                        </span>
                        <span className="font-bold text-amber-700">{formatCurrency(previewTotal)}</span>
                      </>
                    ) : (
                      <span className="font-bold">{formatCurrency(detail?.totalAmount ?? billing.totalAmount)}</span>
                    )}
                  </span>
                </div>
              </div>

              {/* ── Editable quantities ─────────────────────────────────── */}
              {detailLoading ? (
                <div className="flex items-center justify-center gap-2 py-8 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Loading items…
                </div>
              ) : (
                <div className="space-y-3">
                  {/* overflow-x-auto keeps a wide row scrolling inside the table instead of
                      widening the dialog and clipping the labels on the left. */}
                  <ScrollArea className="max-h-[380px] overflow-x-auto rounded-lg border">
                    <table className="w-full min-w-[560px] text-sm">
                      <thead className="sticky top-0 bg-muted/60 backdrop-blur">
                        <tr className="text-left">
                          <th className="px-3 py-2 font-medium">Product</th>
                          <th className="px-3 py-2 font-medium">Type</th>
                          <th className="px-3 py-2 font-medium text-center w-[170px]">Qty</th>
                          <th className="px-3 py-2 font-medium text-right">Unit Price</th>
                          <th className="px-3 py-2 font-medium text-right">Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {fields.map((field, index) => {
                          const item = adjustableItems[index]
                          if (!item) return null
                          const current = watchedItems?.[index]?.quantity
                          const qty = Number.isFinite(current) ? current : item.quantity
                          const returning = item.quantity - qty
                          const isFreeIssue = item.billingItemType === 'FreeIssue'

                          return (
                            <tr
                              key={field.id}
                              className={`border-t ${isFreeIssue ? 'bg-amber-50/40' : ''}`}
                            >
                              <td className="px-3 py-2">
                                <div className="font-medium">{item.productDescription}</div>
                                <div className="text-xs text-muted-foreground">
                                  {item.productCode}
                                  {item.discountRate > 0 && ` · ${item.discountRate}% disc`}
                                </div>
                                {returning > 0 && (
                                  <div className="mt-0.5 text-xs font-medium text-amber-700">
                                    Returning {returning} to stock
                                  </div>
                                )}
                              </td>
                              <td className="px-3 py-2">
                                <Badge variant={isFreeIssue ? 'secondary' : 'outline'} className="text-xs">
                                  {isFreeIssue ? 'Free' : 'Sale'}
                                </Badge>
                              </td>
                              <td className="px-3 py-2">
                                <div className="flex items-center justify-center gap-1">
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    className="h-7 w-7 shrink-0"
                                    disabled={busy || qty <= 0}
                                    onClick={() => {
                                      const cur = form.getValues(`items.${index}.quantity`)
                                      if (cur > 0) form.setValue(`items.${index}.quantity`, cur - 1, { shouldDirty: true })
                                    }}
                                  >
                                    <Minus className="h-3 w-3" />
                                  </Button>
                                  <Input
                                    type="number"
                                    min={0}
                                    // Decrease-only: the rep's billed quantity is the ceiling.
                                    max={item.quantity}
                                    step="any"
                                    className="h-7 w-16 text-center"
                                    disabled={busy}
                                    {...form.register(`items.${index}.quantity`, { valueAsNumber: true })}
                                  />
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    className="h-7 w-7 shrink-0"
                                    disabled={busy || qty >= item.quantity}
                                    onClick={() => {
                                      const cur = form.getValues(`items.${index}.quantity`)
                                      if (cur < item.quantity) form.setValue(`items.${index}.quantity`, cur + 1, { shouldDirty: true })
                                    }}
                                  >
                                    <Plus className="h-3 w-3" />
                                  </Button>
                                  {returning > 0 && (
                                    <Button
                                      type="button"
                                      variant="ghost"
                                      size="icon"
                                      className="h-7 w-7 shrink-0"
                                      title="Reset to billed quantity"
                                      disabled={busy}
                                      onClick={() => form.setValue(`items.${index}.quantity`, item.quantity, { shouldDirty: true })}
                                    >
                                      <Undo2 className="h-3 w-3" />
                                    </Button>
                                  )}
                                </div>
                                <div className="mt-0.5 text-center text-xs text-muted-foreground">
                                  billed {item.quantity}
                                  {item.originalQuantity !== null && item.originalQuantity !== item.quantity
                                    && ` · originally ${item.originalQuantity}`}
                                </div>
                              </td>
                              <td className="px-3 py-2 text-right tabular-nums">
                                {formatCurrency(item.unitPrice)}
                              </td>
                              <td className="px-3 py-2 text-right tabular-nums">
                                {formatCurrency(lineTotal(qty, item.unitPrice, item.discountRate, isFreeIssue))}
                              </td>
                            </tr>
                          )
                        })}

                        {/* Return lines and already-recorded distributor returns — read-only */}
                        {readOnlyItems.map((item) => (
                          <tr key={item.id} className="border-t bg-red-50/40 text-muted-foreground">
                            <td className="px-3 py-2">
                              <div className="font-medium">{item.productDescription}</div>
                              <div className="text-xs">{item.productCode}</div>
                            </td>
                            <td className="px-3 py-2">
                              <Badge variant="destructive" className="text-xs">
                                {item.returnType === 'DistributorReturn' ? 'Dist. Return' : 'Return'}
                              </Badge>
                            </td>
                            <td className="px-3 py-2 text-center tabular-nums">{item.quantity}</td>
                            <td className="px-3 py-2 text-right tabular-nums">{formatCurrency(item.unitPrice)}</td>
                            <td className="px-3 py-2 text-right tabular-nums">{formatCurrency(item.totalPrice)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </ScrollArea>

                  {changedLines.length > 0 && (
                    <div className="space-y-2 rounded-lg border border-amber-300 bg-amber-50/60 p-3">
                      <p className="text-sm font-medium text-amber-900">
                        {changedLines.length} line{changedLines.length > 1 ? 's' : ''} reduced —{' '}
                        {totalReturning} unit{totalReturning === 1 ? '' : 's'} will be returned to your stock
                        and recorded as a Distributor Return.
                      </p>
                      <Textarea
                        placeholder="Note for this adjustment (optional)"
                        rows={2}
                        className="resize-none bg-white text-sm"
                        disabled={busy}
                        {...form.register('note')}
                      />
                      <div className="flex justify-end gap-2">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={busy}
                          onClick={() => form.reset({
                            items: adjustableItems.map((i) => ({ billingItemId: i.id, quantity: i.quantity })),
                            note: '',
                          })}
                        >
                          Discard changes
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          className="gap-1.5"
                          disabled={busy || hasInvalidQuantity}
                          onClick={handleSaveAdjustments}
                        >
                          {adjustMutation.isPending
                            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            : <Save className="h-3.5 w-3.5" />}
                          Save changes
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {!showRejectForm ? (
                <div className="flex items-center justify-end gap-2">
                  <Button
                    variant="outline"
                    className="gap-1.5 border-amber-400 text-amber-700 hover:bg-amber-50"
                    onClick={() => setShowRejectForm(true)}
                    disabled={busy}
                  >
                    <XCircle className="h-4 w-4" />
                    Reject
                  </Button>
                  <Button
                    className="gap-1.5 bg-green-600 hover:bg-green-700 text-white"
                    onClick={() => setShowApproveConfirm(true)}
                    // Approving would discard unsaved edits, so require them to be saved first.
                    disabled={busy || changedLines.length > 0}
                    title={changedLines.length > 0 ? 'Save or discard your quantity changes first' : undefined}
                  >
                    <CheckCircle2 className="h-4 w-4" />
                    Approve
                  </Button>
                </div>
              ) : (
                <div className="space-y-3">
                  <Textarea
                    placeholder="Reason for rejection (optional)"
                    value={rejectReason}
                    onChange={(e) => setRejectReason(e.target.value)}
                    rows={3}
                    className="text-sm resize-none"
                    autoFocus
                  />
                  <div className="flex items-center justify-end gap-2">
                    <Button
                      variant="ghost"
                      onClick={() => { setShowRejectForm(false); setRejectReason('') }}
                      disabled={rejectMutation.isPending}
                    >
                      Cancel
                    </Button>
                    <Button
                      variant="destructive"
                      className="gap-1.5"
                      disabled={rejectMutation.isPending}
                      onClick={() => rejectMutation.mutate(
                        { id: billing.id, reason: rejectReason || undefined },
                        { onSuccess: () => { setShowRejectForm(false); setRejectReason('') } }
                      )}
                    >
                      {rejectMutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                      Confirm Reject
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <AlertDialog open={showApproveConfirm} onOpenChange={setShowApproveConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Approve this billing?</AlertDialogTitle>
            <AlertDialogDescription>
              Billing <span className="font-mono font-semibold">{billing?.billingNumber}</span> from{' '}
              {billing?.salesRepName} will be marked as approved
              {detail ? ` at ${formatCurrency(detail.totalAmount)}` : ''}.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={approveMutation.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-green-600 hover:bg-green-700"
              disabled={approveMutation.isPending}
              onClick={() => billing && approveMutation.mutate(billing.id)}
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
