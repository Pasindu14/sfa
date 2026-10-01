'use client'

import { cn } from '@/lib/utils'
import type { BinCardTotals } from '../schema/bin-card.schema'
import { formatNumber } from '../lib/format'

/**
 * The bin card in one line: opening + stock in - stock out = closing.
 * The same grouping and colours are used for the column bands in the table below.
 */
export function BinCardSummary({ totals }: { totals: BinCardTotals }) {
  const stockIn =
    totals.invoiceQuantity +
    totals.marketResaleable +
    totals.deletedInv +
    totals.stockAdjustment +
    totals.transferIn
  const stockOut =
    totals.soldQty + totals.freeIssues + totals.companyFreeIssues + totals.transferOut

  return (
    <dl className="flex flex-wrap items-center gap-x-5 gap-y-4 rounded-lg border bg-card px-5 py-4">
      <Figure label="Opening" value={formatNumber(totals.openStock)} />
      <Operator>+</Operator>
      <Figure label="Stock in" value={formatNumber(stockIn)} tone="in" />
      <Operator>&minus;</Operator>
      <Figure label="Stock out" value={formatNumber(stockOut)} tone="out" />
      <Operator>=</Operator>
      <Figure label="Closing" value={formatNumber(totals.endStock)} large />

      <div className="flex flex-col-reverse sm:ml-auto sm:text-right">
        <dt className="text-xs text-muted-foreground">Closing stock value</dt>
        <dd className="text-lg font-semibold tabular-nums">
          Rs {formatNumber(totals.closingStockValue, true)}
        </dd>
      </div>
    </dl>
  )
}

function Figure({
  label,
  value,
  tone,
  large,
}: {
  label: string
  value: string
  tone?: 'in' | 'out'
  large?: boolean
}) {
  return (
    <div className="flex flex-col-reverse">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd
        className={cn(
          'font-semibold tabular-nums leading-none',
          large ? 'text-3xl' : 'text-2xl',
          tone === 'in' && 'text-emerald-700 dark:text-emerald-400',
          tone === 'out' && 'text-rose-700 dark:text-rose-400'
        )}
      >
        {value}
      </dd>
    </div>
  )
}

function Operator({ children }: { children: React.ReactNode }) {
  return (
    <span aria-hidden className="pb-4 text-xl text-muted-foreground/60">
      {children}
    </span>
  )
}
