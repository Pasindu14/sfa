'use client'

import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

export const PAGE_SIZES = [25, 50, 100, 250] as const
/** Sentinel for "no paging" — the row count is capped server-side, so this stays bounded. */
export const ALL_ROWS = -1

export function SalesSummaryPagination({
  page,
  pageSize,
  totalRows,
  onPageChange,
  onPageSizeChange,
}: {
  page: number
  pageSize: number
  totalRows: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
}) {
  const showingAll = pageSize === ALL_ROWS
  const pageCount = showingAll ? 1 : Math.max(1, Math.ceil(totalRows / pageSize))
  const first = showingAll ? 1 : (page - 1) * pageSize + 1
  const last = showingAll ? totalRows : Math.min(page * pageSize, totalRows)

  return (
    <div className="flex flex-col gap-3 border-t px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-xs tabular-nums text-muted-foreground">
        {totalRows === 0
          ? 'No rows'
          : `Showing ${first.toLocaleString()}–${last.toLocaleString()} of ${totalRows.toLocaleString()}`}
      </p>

      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2">
          <label htmlFor="sales-summary-page-size" className="text-xs text-muted-foreground">
            Rows
          </label>
          <Select
            value={String(pageSize)}
            onValueChange={(v) => onPageSizeChange(Number(v))}
          >
            <SelectTrigger id="sales-summary-page-size" className="h-8 w-24">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PAGE_SIZES.map((s) => (
                <SelectItem key={s} value={String(s)}>
                  {s}
                </SelectItem>
              ))}
              <SelectItem value={String(ALL_ROWS)}>All</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {!showingAll && pageCount > 1 && (
          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="sm"
              className="h-8 gap-1 px-2"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
            >
              <ChevronLeft className="h-3.5 w-3.5" />
              Previous
            </Button>
            <span className="px-2 text-xs tabular-nums text-muted-foreground">
              {page} / {pageCount}
            </span>
            <Button
              variant="outline"
              size="sm"
              className="h-8 gap-1 px-2"
              disabled={page >= pageCount}
              onClick={() => onPageChange(page + 1)}
            >
              Next
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}
