'use client'

import { useState, useEffect } from 'react'
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
import { Textarea } from '@/components/ui/textarea'
import { Separator } from '@/components/ui/separator'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CheckCircle2, XCircle, Loader2, Save } from 'lucide-react'
import { useApproveBilling, useRejectBilling, useUpdatePaymentType } from '../../hooks/distributor-billing.hooks'
import type { DistributorBillingListItem } from '../../schema/distributor-billing.schema'
import { formatColombo } from '@/lib/utils/datetime'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
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

  const approveMutation = useApproveBilling(onClose)
  const rejectMutation = useRejectBilling(onClose)
  const paymentTypeMutation = useUpdatePaymentType()

  useEffect(() => {
    if (billing) {
      setSelectedPaymentType(billing.paymentType)
      setSavedPaymentType(billing.paymentType)
    }
  }, [billing?.id])

  const paymentTypeChanged = selectedPaymentType !== savedPaymentType

  function handleClose() {
    setShowRejectForm(false)
    setRejectReason('')
    onClose()
  }

  return (
    <>
      <Dialog open={billing !== null} onOpenChange={(v) => { if (!v) handleClose() }}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Review Billing</DialogTitle>
            {billing && (
              <DialogDescription>
                Billing <span className="font-mono font-semibold">{billing.billingNumber}</span>
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
                  <span className="font-bold tabular-nums">{formatCurrency(billing.totalAmount)}</span>
                </div>
              </div>

              {!showRejectForm ? (
                <div className="flex items-center justify-end gap-2">
                  <Button
                    variant="outline"
                    className="gap-1.5 border-amber-400 text-amber-700 hover:bg-amber-50"
                    onClick={() => setShowRejectForm(true)}
                    disabled={approveMutation.isPending || rejectMutation.isPending}
                  >
                    <XCircle className="h-4 w-4" />
                    Reject
                  </Button>
                  <Button
                    className="gap-1.5 bg-green-600 hover:bg-green-700 text-white"
                    onClick={() => setShowApproveConfirm(true)}
                    disabled={approveMutation.isPending || rejectMutation.isPending}
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
              {billing?.salesRepName} will be marked as approved.
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
