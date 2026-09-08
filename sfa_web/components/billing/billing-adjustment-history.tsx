'use client'

import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ChevronDown, ChevronRight, PencilLine } from 'lucide-react'
import { formatColombo } from '@/lib/utils/datetime'

/**
 * Shared between the distributor portal and the staff Rep Bills screen so the two views of the same
 * change trail can't drift. Props are structural on purpose — each feature infers its own zod types.
 */
export interface BillingAdjustmentHistoryLine {
  billingItemId: number
  productCode: string
  productDescription: string
  oldQuantity: number
  newQuantity: number
  oldTotalPrice: number
  newTotalPrice: number
  returnedQuantity: number
  returnValue: number
}

export interface BillingAdjustmentHistoryEntry {
  id: number
  adjustedByName: string
  adjustedAt: string
  note: string | null
  oldTotalAmount: number
  newTotalAmount: number
  lines: BillingAdjustmentHistoryLine[]
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

/** Badge for a line the distributor struck off during review, vs. a return the outlet sent back. */
export function DistributorReturnBadge() {
  return (
    <Badge className="bg-orange-600 hover:bg-orange-700 text-white text-[10px] px-1.5 py-0">
      Dist. Return
    </Badge>
  )
}

/** Renders `7` with the pre-adjustment `10` struck through beside it. */
export function AdjustedQuantity({
  quantity,
  originalQuantity,
}: {
  quantity: number
  originalQuantity: number | null | undefined
}) {
  if (originalQuantity === null || originalQuantity === undefined || originalQuantity === quantity)
    return <span className="tabular-nums">{quantity}</span>

  return (
    <span className="tabular-nums">
      <span className="mr-1.5 text-muted-foreground line-through">{originalQuantity}</span>
      <span className="font-semibold text-orange-700">{quantity}</span>
    </span>
  )
}

export function BillingAdjustmentHistory({
  adjustments,
}: {
  adjustments: BillingAdjustmentHistoryEntry[]
}) {
  const [expanded, setExpanded] = useState(true)

  if (!adjustments || adjustments.length === 0) return null

  return (
    <div className="rounded-lg border border-orange-200 bg-orange-50/50">
      <Button
        type="button"
        variant="ghost"
        className="flex h-auto w-full items-center justify-between px-3 py-2 hover:bg-orange-100/50"
        onClick={() => setExpanded((v) => !v)}
      >
        <span className="flex items-center gap-2 text-sm font-medium text-orange-900">
          <PencilLine className="h-4 w-4" />
          Distributor adjustments
          <Badge variant="secondary" className="text-[10px]">{adjustments.length}</Badge>
        </span>
        {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
      </Button>

      {expanded && (
        <div className="space-y-3 border-t border-orange-200 px-3 py-3">
          {adjustments.map((adjustment) => (
            <div key={adjustment.id} className="space-y-1.5">
              <div className="flex flex-wrap items-baseline justify-between gap-2 text-xs">
                <span className="font-medium text-orange-900">
                  {adjustment.adjustedByName || 'Distributor'}
                </span>
                <span className="text-muted-foreground">
                  {formatColombo(adjustment.adjustedAt, 'd MMM yyyy, h:mm a')}
                </span>
              </div>

              <div className="text-xs text-muted-foreground">
                Total{' '}
                <span className="line-through">{formatCurrency(adjustment.oldTotalAmount)}</span>
                {' → '}
                <span className="font-semibold text-foreground">
                  {formatCurrency(adjustment.newTotalAmount)}
                </span>
              </div>

              <ul className="space-y-1">
                {adjustment.lines.map((line) => (
                  <li
                    key={`${adjustment.id}-${line.billingItemId}`}
                    className="rounded border border-orange-200 bg-white px-2 py-1.5 text-xs"
                  >
                    <div className="font-medium">{line.productDescription}</div>
                    <div className="text-muted-foreground">
                      {line.productCode} · qty{' '}
                      <span className="line-through">{line.oldQuantity}</span>
                      {' → '}
                      <span className="font-semibold text-foreground">{line.newQuantity}</span>
                      {' · '}
                      <span className="text-orange-700">
                        {line.returnedQuantity} returned ({formatCurrency(line.returnValue)})
                      </span>
                    </div>
                  </li>
                ))}
              </ul>

              {adjustment.note && (
                <p className="text-xs italic text-muted-foreground">“{adjustment.note}”</p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
