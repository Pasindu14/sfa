'use client'

import { useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { BinCardResponse } from '../../schema/bin-card.schema'
import { BIN_CARD_COLUMNS, type BinCardColumn } from '../columns/bin-card-columns'
import { formatNumber } from '../../lib/format'

type GroupId = 'item' | 'price' | 'opening' | 'in' | 'out' | 'reference' | 'closing'

// The column bands follow the stock equation: opening + in - out = closing.
const GROUPS: Record<GroupId, { label: string; band: string }> = {
  item: { label: 'Item', band: 'bg-muted text-foreground' },
  price: { label: 'Unit price', band: 'bg-muted text-foreground' },
  opening: { label: 'Opening', band: 'bg-muted text-foreground' },
  in: {
    label: '+ Stock in',
    band: 'bg-emerald-50 text-emerald-900 dark:bg-emerald-950 dark:text-emerald-200',
  },
  out: {
    label: '− Stock out',
    band: 'bg-rose-50 text-rose-900 dark:bg-rose-950 dark:text-rose-200',
  },
  reference: { label: 'For reference', band: 'bg-muted text-muted-foreground' },
  closing: { label: 'Closing', band: 'bg-muted text-foreground' },
}

// Item and price headers span both header rows; every other band has a label row and a column row.
const TALL_GROUPS = new Set<GroupId>(['item', 'price'])
// Only movement columns can be hidden for having no activity — opening and closing always show.
const MOVEMENT_GROUPS = new Set<GroupId>(['in', 'out', 'reference'])

const META: Record<string, { group: GroupId; label: string; hint?: string }> = {
  itemCode: { group: 'item', label: 'Item' },
  itemDescription: { group: 'item', label: 'Description' },
  itemPrice: { group: 'price', label: 'Unit price', hint: 'Price used to value the stock' },
  openStock: { group: 'opening', label: 'Open stock', hint: 'Stock on hand at the start of the range' },
  invoiceQuantity: { group: 'in', label: 'Invoice qty', hint: 'Received from the company (GRN)' },
  marketResaleable: {
    group: 'in',
    label: 'Market resaleable',
    hint: 'Returned from the market and fit to sell again',
  },
  deletedInv: {
    group: 'in',
    label: 'Deleted invoices',
    hint: 'Net stock restored by deleted or reversed bills. Can be negative',
  },
  stockAdjustment: {
    group: 'in',
    label: 'Stock adjustment',
    hint: 'Net corrections from stock taking. Can be negative',
  },
  transferIn: { group: 'in', label: 'Transfer in', hint: 'Received from a closed distributor' },
  soldQty: { group: 'out', label: 'Sold qty', hint: 'Quantity sold on bills' },
  freeIssues: { group: 'out', label: 'Free issues', hint: 'Free goods funded by the distributor' },
  companyFreeIssues: {
    group: 'out',
    label: 'Company free issues',
    hint: 'Free goods funded by the company',
  },
  transferOut: { group: 'out', label: 'Transfer out', hint: 'Moved to another distributor on closure' },
  repReturnQtyDE: {
    group: 'reference',
    label: 'Damage / expiry returns',
    hint: 'Returned by reps as damaged or expired. Does not change stock',
  },
  endStock: { group: 'closing', label: 'End stock', hint: 'Opening + stock in − stock out' },
  currentStock: { group: 'closing', label: 'Counted stock', hint: 'Latest physical count from stock taking' },
  closingStockValue: { group: 'closing', label: 'Stock value', hint: 'End stock × unit price' },
  stockVariance: {
    group: 'closing',
    label: 'Variance',
    hint: 'Counted stock − end stock. Negative means a shortage',
  },
}

const NEUTRAL_EDGE = 'border-l border-border'

export function BinCardTable({ data }: { data: BinCardResponse }) {
  const [showEmpty, setShowEmpty] = useState(false)

  const { columns, hiddenCount } = useMemo(() => {
    // The description is shown under the item code, so it has no column of its own.
    const all = BIN_CARD_COLUMNS.filter((c) => c.key !== 'itemDescription')
    const isEmpty = (c: BinCardColumn) =>
      MOVEMENT_GROUPS.has(META[c.key].group) && data.rows.every((r) => Number(c.get(r)) === 0)
    return {
      columns: showEmpty ? all : all.filter((c) => !isEmpty(c)),
      hiddenCount: all.filter(isEmpty).length,
    }
  }, [data.rows, showEmpty])

  const bands = useMemo(() => {
    const out: { group: GroupId; span: number; firstKey: string }[] = []
    for (const col of columns) {
      const group = META[col.key].group
      const last = out[out.length - 1]
      if (last && last.group === group) last.span++
      else out.push({ group, span: 1, firstKey: col.key })
    }
    return out
  }, [columns])

  const bandStarts = useMemo(() => new Set(bands.slice(1).map((b) => b.firstKey)), [bands])
  const labelRowColumns = columns.filter((c) => !TALL_GROUPS.has(META[c.key].group))

  return (
    <div className="flex flex-col gap-2">
      {hiddenCount > 0 && (
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
          <span>
            {showEmpty
              ? 'Showing every column.'
              : `${hiddenCount} ${hiddenCount === 1 ? 'column has' : 'columns have'} no activity in this range and ${hiddenCount === 1 ? 'is' : 'are'} hidden.`}
          </span>
          <Button variant="ghost" size="sm" className="h-7" onClick={() => setShowEmpty((v) => !v)}>
            {showEmpty ? 'Hide empty columns' : 'Show all columns'}
          </Button>
        </div>
      )}

      <div className="rounded-lg border bg-card">
        <div className="max-h-[70vh] overflow-auto">
          <table className="w-full min-w-max border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                {bands.map((b, bi) => {
                  const tall = TALL_GROUPS.has(b.group)
                  const col = columns.find((c) => c.key === b.firstKey)!
                  return (
                    <th
                      key={b.firstKey}
                      colSpan={b.span}
                      rowSpan={tall ? 2 : 1}
                      scope={tall ? 'col' : 'colgroup'}
                      className={cn(
                        'sticky top-0 whitespace-nowrap border-b px-3 text-xs font-semibold',
                        GROUPS[b.group].band,
                        tall ? 'align-bottom pb-2' : 'h-8 text-left',
                        tall && col.align === 'right' && 'text-right',
                        b.group === 'item' ? 'left-0 z-30 border-r text-left' : 'z-20',
                        bi > 0 && NEUTRAL_EDGE
                      )}
                    >
                      {tall ? META[col.key].label : GROUPS[b.group].label}
                    </th>
                  )
                })}
              </tr>
              <tr>
                {labelRowColumns.map((col) => {
                  const meta = META[col.key]
                  return (
                    <th
                      key={col.key}
                      scope="col"
                      className={cn(
                        'sticky top-8 z-20 min-w-[4.5rem] border-b px-3 py-1.5 align-bottom text-xs font-medium leading-tight',
                        GROUPS[meta.group].band,
                        col.align === 'right' ? 'text-right' : 'text-left',
                        bandStarts.has(col.key) && NEUTRAL_EDGE
                      )}
                    >
                      <span
                        title={meta.hint}
                        className={cn(
                          meta.hint &&
                            'cursor-help underline decoration-dotted decoration-current/30 underline-offset-4'
                        )}
                      >
                        {meta.label}
                      </span>
                    </th>
                  )
                })}
              </tr>
            </thead>

            <tbody>
              {data.rows.map((row, ri) => (
                <tr key={`${row.itemCode}-${ri}`} className="group">
                  {columns.map((col) => {
                    const raw = col.get(row)
                    const isItem = col.key === 'itemCode'
                    const shortage = col.key === 'stockVariance' && typeof raw === 'number' && raw < 0
                    const surplus = col.key === 'stockVariance' && typeof raw === 'number' && raw > 0
                    return (
                      <td
                        key={col.key}
                        className={cn(
                          'whitespace-nowrap border-b px-3 py-2 group-hover:bg-muted/50',
                          col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                          isItem && 'sticky left-0 z-10 border-r bg-card group-hover:bg-muted',
                          col.key === 'endStock' && 'bg-muted/40 font-semibold',
                          shortage && 'font-semibold text-rose-600 dark:text-rose-400',
                          surplus && 'font-semibold text-amber-600 dark:text-amber-400',
                          raw === 0 && 'text-muted-foreground/45',
                          bandStarts.has(col.key) && NEUTRAL_EDGE
                        )}
                      >
                        {isItem ? (
                          <>
                            <div className="font-semibold">{row.itemCode}</div>
                            <div
                              className="max-w-[17rem] truncate text-xs text-muted-foreground"
                              title={row.itemDescription}
                            >
                              {row.itemDescription}
                            </div>
                          </>
                        ) : (
                          renderCell(raw, col, surplus)
                        )}
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>

            <tfoot>
              <tr>
                {columns.map((col) => {
                  const isItem = col.key === 'itemCode'
                  const total = col.getTotal ? col.getTotal(data.totals) : null
                  return (
                    <td
                      key={col.key}
                      className={cn(
                        'sticky bottom-0 z-10 whitespace-nowrap border-t-2 bg-muted px-3 py-2.5 font-semibold',
                        col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                        isItem && 'left-0 z-20 border-r',
                        bandStarts.has(col.key) && NEUTRAL_EDGE
                      )}
                    >
                      {isItem ? 'Total' : total === null ? '' : formatNumber(Number(total), col.money)}
                    </td>
                  )
                })}
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </div>
  )
}

function renderCell(raw: string | number | null, col: BinCardColumn, showPlus: boolean) {
  if (raw === null || raw === undefined) return '—'
  if (typeof raw === 'string') return raw
  return `${showPlus ? '+' : ''}${formatNumber(raw, col.money)}`
}
