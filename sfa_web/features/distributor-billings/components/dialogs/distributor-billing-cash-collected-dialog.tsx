'use client'

import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { Loader2, Save } from 'lucide-react'
import { useUpdateCashCollected } from '../../hooks/distributor-billing.hooks'
import type { DistributorBillingListItem } from '../../schema/distributor-billing.schema'

interface Props {
  billing: DistributorBillingListItem | null
  onClose: () => void
}

export function DistributorBillingCashCollectedDialog({ billing, onClose }: Props) {
  const [localValue, setLocalValue] = useState(billing?.isCashCollected ?? false)
  const cashCollectedMutation = useUpdateCashCollected()

  useEffect(() => {
    if (billing) setLocalValue(billing.isCashCollected)
  }, [billing?.id])

  const hasChanged = billing !== null && localValue !== billing.isCashCollected

  return (
    <Dialog open={billing !== null} onOpenChange={(v) => { if (!v) onClose() }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Cash Collection</DialogTitle>
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
                <span className="text-muted-foreground">Payment Type</span>
                <span className="font-medium">{billing.paymentType}</span>
              </div>
            </div>

            <div className="flex items-center justify-between rounded-lg border p-4">
              <div className="space-y-0.5">
                <p className="text-sm font-medium">Cash Collected</p>
                <p className="text-xs text-muted-foreground">
                  Mark whether cash has been collected from the sales rep
                </p>
              </div>
              <Switch
                checked={localValue}
                disabled={cashCollectedMutation.isPending}
                onCheckedChange={setLocalValue}
              />
            </div>

            <div className="flex justify-end">
              <Button
                disabled={!hasChanged || cashCollectedMutation.isPending}
                onClick={() =>
                  cashCollectedMutation.mutate(
                    { id: billing.id, isCashCollected: localValue },
                    { onSuccess: () => onClose() },
                  )
                }
                className="gap-1.5"
              >
                {cashCollectedMutation.isPending
                  ? <Loader2 className="h-4 w-4 animate-spin" />
                  : <Save className="h-4 w-4" />
                }
                Save
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
