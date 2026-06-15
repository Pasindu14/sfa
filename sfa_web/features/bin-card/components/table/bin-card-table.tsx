'use client'

import { cn } from '@/lib/utils'
import type { BinCardResponse } from '../../schema/bin-card.schema'
import { BIN_CARD_COLUMNS, formatCell } from '../columns/bin-card-columns'

export function BinCardTable({ data }: { data: BinCardResponse }) {
  return (
    <div className="rounded-lg border">
      <div className="max-h-[70vh] overflow-auto">
        <table className="w-full min-w-max border-collapse text-sm">
          <thead>
            <tr>
              {BIN_CARD_COLUMNS.map((col, i) => (
                <th
                  key={col.key}
                  className={cn(
                    'sticky top-0 z-10 whitespace-nowrap border-b bg-muted px-3 py-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground',
                    col.align === 'right' ? 'text-right' : 'text-left',
                    i === 0 && 'left-0 z-20'
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.rows.map((row, ri) => (
              <tr key={`${row.itemCode}-${ri}`} className="border-b last:border-0 hover:bg-muted/40">
                {BIN_CARD_COLUMNS.map((col, ci) => {
                  const raw = col.get(row)
                  const negative =
                    col.key === 'stockVariance' && typeof raw === 'number' && raw < 0
                  return (
                    <td
                      key={col.key}
                      className={cn(
                        'whitespace-nowrap px-3 py-1.5',
                        col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                        ci === 0 && 'sticky left-0 bg-card font-medium',
                        col.key === 'endStock' && 'font-semibold',
                        negative && 'font-semibold text-red-500'
                      )}
                    >
                      {formatCell(raw, col)}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t-2 bg-muted/60 font-semibold">
              {BIN_CARD_COLUMNS.map((col, ci) => {
                const total = col.getTotal ? col.getTotal(data.totals) : null
                return (
                  <td
                    key={col.key}
                    className={cn(
                      'whitespace-nowrap px-3 py-2',
                      col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                      ci === 0 && 'sticky left-0 bg-muted'
                    )}
                  >
                    {ci === 0 ? 'TOTAL' : total === null ? '' : formatCell(total, col)}
                  </td>
                )
              })}
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  )
}
