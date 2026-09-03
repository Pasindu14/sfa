'use client'

import { useMemo } from 'react'
import { cn } from '@/lib/utils'
import type { SalesSummaryResponse } from '../../schema/sales-summary.schema'
import { buildSalesSummaryColumns, formatCell } from '../columns/sales-summary-columns'

export function SalesSummaryTable({ data }: { data: SalesSummaryResponse }) {
  const columns = useMemo(() => buildSalesSummaryColumns(data.groupBy), [data.groupBy])

  return (
    <div className="rounded-lg border">
      {/* The table scrolls inside this box so the page body never scrolls sideways. */}
      <div className="max-h-[70vh] overflow-auto">
        <table className="w-full min-w-max border-collapse text-sm">
          <thead>
            <tr>
              {columns.map((col, i) => (
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
              <tr
                key={`${row.groupKey ?? 'unassigned'}-${ri}`}
                className="border-b last:border-0 hover:bg-muted/40"
              >
                {columns.map((col, ci) => {
                  const raw = col.get(row)
                  const negative = typeof raw === 'number' && raw < 0
                  const underTarget =
                    col.key === 'achievementPercent' && typeof raw === 'number' && raw < 100
                  return (
                    <td
                      key={col.key}
                      className={cn(
                        'whitespace-nowrap px-3 py-1.5',
                        col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                        ci === 0 && 'sticky left-0 bg-card font-medium',
                        col.key === 'netSaleValue' && 'font-semibold',
                        underTarget && 'text-amber-600 dark:text-amber-500',
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
              {columns.map((col, ci) => {
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
                    {ci === 0 ? 'TOTAL' : col.getTotal ? formatCell(total, col) : ''}
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
