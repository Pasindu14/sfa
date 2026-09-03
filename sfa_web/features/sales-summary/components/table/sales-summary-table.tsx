'use client'

import { useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import type { SalesSummaryResponse } from '../../schema/sales-summary.schema'
import {
  BANDS,
  buildSalesSummaryColumns,
  formatDisplay,
  type Band,
  type SalesSummaryColumn,
} from '../columns/sales-summary-columns'
import { AchievementMeter } from '../summary/achievement-meter'
import { ALL_ROWS, SalesSummaryPagination } from './sales-summary-pagination'

export function SalesSummaryTable({ data }: { data: SalesSummaryResponse }) {
  const columns = useMemo(() => buildSalesSummaryColumns(data.groupBy), [data.groupBy])

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<number>(25)

  // Clamped rather than reset: a shorter result set must never leave the view on a page that no
  // longer exists, which would render as an empty table and read as a failed load. Resetting to
  // page 1 when the grouping changes is handled by the caller remounting on a key, so there is no
  // state-setting effect here.
  const pageCount =
    pageSize === ALL_ROWS ? 1 : Math.max(1, Math.ceil(data.rows.length / pageSize))
  const safePage = Math.min(page, pageCount)

  const rows = useMemo(() => {
    if (pageSize === ALL_ROWS) return data.rows
    const start = (safePage - 1) * pageSize
    return data.rows.slice(start, start + pageSize)
  }, [data.rows, safePage, pageSize])

  // Bands present in this grouping, with the column span each one covers.
  const bands = useMemo(
    () =>
      BANDS.map((b) => ({ band: b, span: columns.filter((c) => c.band === b.id).length })).filter(
        (b) => b.span > 0
      ),
    [columns]
  )

  const bandOf = (id: string) => BANDS.find((b) => b.id === id) as Band
  /** True on the first column of a band — carries the vertical rule that separates bands. */
  const startsBand = (col: SalesSummaryColumn, i: number) =>
    i > 0 && columns[i - 1].band !== col.band

  return (
    <div className="overflow-hidden rounded-lg border bg-card font-report">
      <div className="max-h-[70vh] overflow-auto">
        <table className="w-full min-w-max border-collapse text-[13px]">
          <thead>
            {/* Band tier. Five named groups are legible where fifteen bare columns are not. */}
            <tr>
              {bands.map(({ band, span }, i) => (
                <th
                  key={band.id}
                  colSpan={span}
                  className={cn(
                    'sticky top-0 z-20 whitespace-nowrap border-b px-3 pb-1 pt-2.5 text-left text-[11px] font-semibold text-foreground/70',
                    band.tinted ? 'bg-[#F6F3EF]' : 'bg-card',
                    i > 0 && 'border-l',
                    band.id === 'label' && 'left-0 z-30'
                  )}
                >
                  {band.header}
                </th>
              ))}
            </tr>
            {/* Column tier. */}
            <tr>
              {columns.map((col, i) => (
                <th
                  key={col.key}
                  className={cn(
                    'sticky top-[30px] z-20 whitespace-nowrap border-b px-3 pb-2 text-[11px] font-medium text-muted-foreground',
                    col.align === 'right' ? 'text-right' : 'text-left',
                    bandOf(col.band).tinted ? 'bg-[#F6F3EF]' : 'bg-card',
                    startsBand(col, i) && 'border-l',
                    i === 0 && 'left-0 z-30'
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            {rows.map((row, ri) => (
              <tr
                key={`${row.groupKey ?? 'unassigned'}-${ri}`}
                className="border-b last:border-0 hover:bg-muted/30"
              >
                {columns.map((col, ci) => {
                  const raw = col.get(row)
                  const negative = typeof raw === 'number' && raw < 0
                  const tinted = bandOf(col.band).tinted

                  return (
                    <td
                      key={col.key}
                      className={cn(
                        'whitespace-nowrap px-3 py-2',
                        col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                        tinted && 'bg-[#FAF8F5]',
                        startsBand(col, ci) && 'border-l',
                        ci === 0 && 'sticky left-0 bg-card font-medium',
                        col.key === 'netSaleValue' && 'font-semibold',
                        negative && 'text-red-600'
                      )}
                    >
                      {col.meter ? (
                        <AchievementMeter percent={raw as number | null} />
                      ) : (
                        formatDisplay(raw, col)
                      )}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>

          <tfoot>
            {/* Sticky so the total stays in view while scrolling a long table. */}
            <tr className="sticky bottom-0 z-20 border-t-2 bg-[#F6F3EF] font-semibold">
              {columns.map((col, ci) => {
                const total = col.getTotal ? col.getTotal(data.totals) : null
                return (
                  <td
                    key={col.key}
                    className={cn(
                      'whitespace-nowrap px-3 py-2.5',
                      col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                      startsBand(col, ci) && 'border-l',
                      ci === 0 && 'sticky left-0 z-10 bg-[#F6F3EF]'
                    )}
                  >
                    {/* Named for the whole population, because a paginated view would otherwise
                        read as a page subtotal. */}
                    {ci === 0 ? (
                      <span className="whitespace-nowrap">
                        Total
                        <span className="ml-1.5 font-normal text-muted-foreground">
                          all {data.groupCount.toLocaleString()} rows
                        </span>
                      </span>
                    ) : col.meter ? (
                      <AchievementMeter percent={total as number | null} />
                    ) : col.getTotal ? (
                      formatDisplay(total, col)
                    ) : (
                      ''
                    )}
                  </td>
                )
              })}
            </tr>
          </tfoot>
        </table>
      </div>

      <SalesSummaryPagination
        page={safePage}
        pageSize={pageSize}
        totalRows={data.rows.length}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size)
          setPage(1)
        }}
      />
    </div>
  )
}
