'use client'

import { Separator } from '@/components/ui/separator'

/**
 * The one money breakdown every bill screen shows, so the screens can't drift:
 *
 *   Sales (gross) − Discount − Returns = Total        (Free issues is informational only)
 *
 * Gross is the sale lines before ANY discount; Discount is line discounts + bill-level discount;
 * Returns is the outlet returns deducted from the total. Props are structural on purpose — each
 * feature infers its own zod types.
 */
export interface BillBreakdownAmounts {
  gross: number
  discount: number
  returns: number
  freeIssue: number
  total: number
}

/** Amounts a list endpoint row carries. The breakdown fields are optional so an older API still renders. */
export interface BillListAmounts {
  totalAmount: number
  grossAmount?: number
  totalDiscount?: number
  returnValue?: number
  freeIssueValue?: number
}

/** Amounts a detail endpoint payload carries (`BillingDto`). */
export interface BillDetailAmounts {
  subTotalAmount: number
  billDiscountAmount: number
  returnValue: number
  freeIssueValue: number
  totalAmount: number
  /** Optional: fall back to summing the Sale lines' discounts when the API doesn't send it. */
  itemWiseTotalDiscount?: number
  totalDiscount?: number
  items: { billingItemType: string; discountAmount: number }[]
}

/** List row -> breakdown, or null when the API predates the breakdown fields (show Total only). */
export function listBreakdown(row: BillListAmounts): BillBreakdownAmounts | null {
  if (row.grossAmount === undefined) return null
  return {
    gross: row.grossAmount,
    discount: row.totalDiscount ?? 0,
    returns: row.returnValue ?? 0,
    freeIssue: row.freeIssueValue ?? 0,
    total: row.totalAmount,
  }
}

/** Detail payload -> breakdown. `subTotalAmount` is NET of line discounts, so gross adds them back. */
export function detailBreakdown(bill: BillDetailAmounts): BillBreakdownAmounts {
  const itemDiscount =
    bill.itemWiseTotalDiscount ??
    bill.items
      .filter((i) => i.billingItemType === 'Sale')
      .reduce((sum, i) => sum + i.discountAmount, 0)
  return {
    gross: bill.subTotalAmount + itemDiscount,
    discount: bill.totalDiscount ?? itemDiscount + bill.billDiscountAmount,
    returns: bill.returnValue,
    freeIssue: bill.freeIssueValue,
    total: bill.totalAmount,
  }
}

const plainNumber = new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2 })

/**
 * "Items 40.00 · Bill 0.79 (2%)" — what the single Discount row is made of, so the item-vs-bill
 * split isn't lost. Undefined when only one component exists (the row already says it all).
 */
export function discountNote(itemDiscount: number, billDiscount: number, billRate: number) {
  if (!(itemDiscount > 0 && billDiscount > 0)) return undefined
  return `Items ${plainNumber.format(itemDiscount)} · Bill ${plainNumber.format(billDiscount)} (${billRate}%)`
}

/**
 * Detail-view footer rows. Sales (gross) and Total always show; Discount, Returns and Free issues
 * only when above zero. `children` render between Free issues and Total for screen-specific
 * informational rows.
 */
export function BillBreakdown({
  amounts,
  formatCurrency,
  discountNote,
  children,
  totalSlot,
}: {
  amounts: BillBreakdownAmounts
  formatCurrency: (amount: number) => string
  /** Muted hint under the Discount label, e.g. "Items 40.00 · Bill 0.79 (2%)". */
  discountNote?: string
  children?: React.ReactNode
  /** Replaces the Total value (the review dialog shows old -> new while edits are pending). */
  totalSlot?: React.ReactNode
}) {
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>Sales (gross)</span>
        <span className="tabular-nums">{formatCurrency(amounts.gross)}</span>
      </div>
      {amounts.discount > 0 && (
        <div className="flex justify-between gap-3 text-xs text-muted-foreground">
          <span>
            Discount
            {discountNote && <span className="ml-1.5 text-[10px]">({discountNote})</span>}
          </span>
          <span className="tabular-nums text-red-500">− {formatCurrency(amounts.discount)}</span>
        </div>
      )}
      {amounts.returns > 0 && (
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>Returns</span>
          <span className="tabular-nums text-red-500">− {formatCurrency(amounts.returns)}</span>
        </div>
      )}
      {amounts.freeIssue > 0 && (
        // Informational: free issues are never part of the arithmetic above.
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>Free issues (info)</span>
          <span className="tabular-nums text-amber-600">{formatCurrency(amounts.freeIssue)}</span>
        </div>
      )}
      {children}
      <Separator className="my-1" />
      <div className="flex justify-between text-sm font-bold">
        <span>Total</span>
        {totalSlot ?? <span className="tabular-nums">{formatCurrency(amounts.total)}</span>}
      </div>
    </div>
  )
}

/**
 * List-view amount cell: Total stays the headline; a small muted line underneath spells out the
 * breakdown ("Sales 6,128.00 · Disc −40.79 · Returns −2,352.31"), omitting zero parts. Falls back
 * to the bare total when the API doesn't send the breakdown or the bill has nothing to break down.
 */
export function BillAmountCell({
  amounts,
  total,
  formatCurrency,
}: {
  amounts: BillBreakdownAmounts | null
  total: number
  formatCurrency: (amount: number) => string
}) {
  const parts: string[] = []
  if (amounts && (amounts.discount > 0 || amounts.returns > 0 || amounts.freeIssue > 0)) {
    parts.push(`Sales ${plainNumber.format(amounts.gross)}`)
    if (amounts.discount > 0) parts.push(`Disc −${plainNumber.format(amounts.discount)}`)
    if (amounts.returns > 0) parts.push(`Returns −${plainNumber.format(amounts.returns)}`)
    if (amounts.freeIssue > 0) parts.push(`Free ${plainNumber.format(amounts.freeIssue)}`)
  }

  return (
    <div className="text-right">
      <span className="block text-sm font-semibold tabular-nums">{formatCurrency(total)}</span>
      {parts.length > 0 && (
        // Each part is nowrap but the row wraps, so a narrow column folds the line instead of clipping it.
        <div className="mt-0.5 flex flex-wrap justify-end gap-x-1.5 text-[11px] leading-tight text-muted-foreground tabular-nums">
          {parts.map((p) => (
            <span key={p} className="whitespace-nowrap">
              {p}
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
