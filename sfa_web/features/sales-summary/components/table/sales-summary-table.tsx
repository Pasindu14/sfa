'use client'

import { Fragment, useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import {
  dimensionsOf,
  groupCell,
  type SalesSummaryResponse,
  type SalesSummaryRow,
  type SalesSummaryTotals,
} from '../../schema/sales-summary.schema'
import {
  BANDS,
  buildSalesSummaryColumns,
  formatDisplay,
  type Band,
  type SalesSummaryColumn,
} from '../columns/sales-summary-columns'
import { AchievementMeter } from '../summary/achievement-meter'
import { ALL_ROWS, SalesSummaryPagination } from './sales-summary-pagination'

/** True when two rows share every grouping value up to and including dimension `upTo`. */
function sameLeading(a: SalesSummaryRow, b: SalesSummaryRow, upTo: number): boolean {
  for (let i = 0; i <= upTo; i++) {
    if (groupCell(a, i).key !== groupCell(b, i).key) return false
  }
  return true
}

const blockKey = (row: SalesSummaryRow) => String(groupCell(row, 0).key ?? 'unassigned')

interface Block {
  name: string
  count: number
  totals: SalesSummaryTotals
}

/** Subtotals per first-dimension value, summed the same way the API sums the grand total. */
function buildBlocks(rows: SalesSummaryRow[]): Map<string, Block> {
  const map = new Map<string, { name: string; rows: SalesSummaryRow[] }>()
  for (const r of rows) {
    const k = blockKey(r)
    const b = map.get(k) ?? { name: groupCell(r, 0).name, rows: [] }
    b.rows.push(r)
    map.set(k, b)
  }

  const sum = (rs: SalesSummaryRow[], f: (r: SalesSummaryRow) => number) =>
    Math.round(rs.reduce((a, r) => a + f(r), 0) * 100) / 100
  const sumNullable = (rs: SalesSummaryRow[], f: (r: SalesSummaryRow) => number | null) =>
    rs.every((r) => f(r) === null) ? null : sum(rs, (r) => f(r) ?? 0)

  const out = new Map<string, Block>()
  for (const [k, { name, rows: rs }] of map) {
    const targetValue = sumNullable(rs, (r) => r.targetValue)
    const netSaleValue = sum(rs, (r) => r.netSaleValue)
    out.set(k, {
      name,
      count: rs.length,
      totals: {
        targetValue,
        targetQty: sumNullable(rs, (r) => r.targetQty),
        grossSaleValue: sum(rs, (r) => r.grossSaleValue),
        saleQty: sum(rs, (r) => r.saleQty),
        goodReturn: sum(rs, (r) => r.goodReturn),
        goodReturnQty: sum(rs, (r) => r.goodReturnQty),
        marketReturn: sum(rs, (r) => r.marketReturn),
        marketReturnQty: sum(rs, (r) => r.marketReturnQty),
        dbDiscount: sum(rs, (r) => r.dbDiscount),
        discount: sum(rs, (r) => r.discount),
        netSaleValue,
        netSaleQty: sum(rs, (r) => r.netSaleQty),
        // Same rule as the API: no percentage of a missing or zero target.
        achievementPercent:
          targetValue === null || targetValue === 0
            ? null
            : Math.round((netSaleValue / targetValue) * 10000) / 100,
      },
    })
  }
  return out
}

export function SalesSummaryTable({ data }: { data: SalesSummaryResponse }) {
  const dims = useMemo(() => dimensionsOf(data), [data])
  const columns = useMemo(() => buildSalesSummaryColumns(dims), [dims])

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<number>(25)

  // Clamped rather than reset: a shorter result set must never leave the view on a page that no
  // longer exists, which would render as an empty table and read as a failed load. Resetting to
  // page 1 when the grouping changes is handled by the caller remounting on a key, so there is no
  // state-setting effect here.
  const pageCount = pageSize === ALL_ROWS ? 1 : Math.max(1, Math.ceil(data.rows.length / pageSize))
  const safePage = Math.min(page, pageCount)

  const rows = useMemo(() => {
    if (pageSize === ALL_ROWS) return data.rows
    const start = (safePage - 1) * pageSize
    return data.rows.slice(start, start + pageSize)
  }, [data.rows, safePage, pageSize])

  // Label columns come first; the block header spans them.
  const labelSpan = columns.filter((c) => c.band === 'label').length

  // With 2+ dimensions, rows arrive grouped by the first one (the API orders them that way).
  // Each first-dimension value becomes a block with a subtotal over ALL its rows.
  const blocks = useMemo(() => (dims.length > 1 ? buildBlocks(data.rows) : null), [dims, data.rows])

  // Bands present in this grouping, with the column span each one covers.
  const bands = useMemo(
    () =>
      BANDS.map((b) => ({
        band: b,
        span: columns.filter((c) => c.band === b.id).length,
      })).filter((b) => b.span > 0),
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
                    'sticky top-0 z-20 h-9 whitespace-nowrap border-b border-t-[3px] px-3 text-left align-middle text-xs font-bold uppercase tracking-wider text-foreground',
                    band.tinted
                      ? 'border-t-foreground/60 bg-[#EDE6DE]'
                      : 'border-t-foreground/25 bg-[#F5F2EE]',
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
                    'sticky top-9 z-20 whitespace-nowrap border-b-2 border-b-foreground/15 px-3 py-2.5 text-xs font-semibold uppercase tracking-wide text-foreground/75',
                    col.align === 'right' ? 'text-right' : 'text-left',
                    bandOf(col.band).tinted ? 'bg-[#F3EEE8]' : 'bg-[#FAF8F5]',
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
            {rows.map((row, ri) => {
              const bk = blockKey(row)
              const block = blocks?.get(bk)
              // A one-row block would only repeat its row, so it gets no header and renders plainly.
              const inBlock = !!block && block.count > 1
              const opensBlock = inBlock && (ri === 0 || blockKey(rows[ri - 1]) !== bk)

              return (
                <Fragment
                  key={`${dims.map((_, i) => groupCell(row, i).key ?? 'u').join('-')}-${ri}`}
                >
                  {opensBlock && block && (
                    // Block subtotal: the first dimension's total across ALL its rows (not just this
                    // page), so a rep's number is right even when their rows span two pages.
                    <tr className="border-b border-t-2 border-t-foreground/10 bg-[#F3EEE8] font-semibold">
                      <td
                        colSpan={labelSpan}
                        className="sticky left-0 z-10 whitespace-nowrap bg-[#F3EEE8] px-3 py-2.5"
                      >
                        <span className="inline-flex items-center gap-2">
                          <span className="h-4 w-1 rounded-full bg-foreground/40" aria-hidden />
                          {block.name}
                          <span className="rounded-full bg-background/80 px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                            {block.count} {block.count === 1 ? 'row' : 'rows'}
                            {ri === 0 &&
                            safePage > 1 &&
                            blockKey(data.rows[(safePage - 1) * pageSize - 1]) === bk
                              ? ' · continued'
                              : ''}
                          </span>
                        </span>
                      </td>
                      {columns.slice(labelSpan).map((col, i) => {
                        const ci = i + labelSpan
                        const v = col.getTotal ? col.getTotal(block.totals) : null
                        return (
                          <td
                            key={col.key}
                            className={cn(
                              'whitespace-nowrap px-3 py-2.5',
                              col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                              startsBand(col, ci) && 'border-l',
                              typeof v === 'number' && v < 0 && 'text-red-600'
                            )}
                          >
                            {col.meter ? (
                              <AchievementMeter percent={v as number | null} />
                            ) : col.getTotal ? (
                              formatDisplay(v, col)
                            ) : (
                              ''
                            )}
                          </td>
                        )
                      })}
                    </tr>
                  )}
                  <tr className="border-b last:border-0 hover:bg-muted/30">
                    {columns.map((col, ci) => {
                      const raw = col.get(row)
                      const negative = typeof raw === 'number' && raw < 0
                      const tinted = bandOf(col.band).tinted
                      // Rows arrive grouped by their leading dimensions; a label that just repeats the
                      // row above (or the block header) is dimmed so each block reads as a block. Still
                      // printed, not blanked, so a row makes sense on its own when copied.
                      const repeated =
                        col.groupIndex !== undefined &&
                        ((col.groupIndex === 0 && inBlock) ||
                          (ri > 0 && sameLeading(rows[ri - 1], row, col.groupIndex)))

                      return (
                        <td
                          key={col.key}
                          className={cn(
                            'whitespace-nowrap px-3 py-2',
                            col.align === 'right' ? 'text-right tabular-nums' : 'text-left',
                            tinted && 'bg-[#FAF8F5]',
                            startsBand(col, ci) && 'border-l',
                            ci === 0 && 'sticky left-0 bg-card font-medium',
                            // Indent detail rows under their block header.
                            ci === 0 && inBlock && 'pl-6',
                            col.key === 'netSaleValue' && 'font-semibold',
                            negative && 'text-red-600',
                            repeated && 'font-normal text-muted-foreground/60'
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
                </Fragment>
              )
            })}
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
