'use client'

import { useState } from 'react'
import { Download, Loader2, RotateCcw, Search } from 'lucide-react'
import { AsyncSelect } from '@/components/async-select'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toColomboDateStr } from '@/lib/utils/datetime'
import { fetchAllDistributorsForSelect } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import { useActiveProductsFetcher } from '@/features/product/hooks/product.hooks'
import type { ProductLookupDto } from '@/features/product/schema/product.schema'
import {
  useExportStockActivity,
  useStockActivityIsFetching,
  useStockActivityUsersFetcher,
} from '../../hooks/stock-activity.hooks'
import {
  MAX_RANGE_DAYS,
  STOCK_TRANSACTION_TYPES,
  type StockActivityUser,
  type StockDirection,
  type StockTransactionType,
} from '../../schema/stock-activity.schema'
import { useStockActivityFilters } from '../../store'

const ALL = 'all'

/** Inclusive day count of a YYYY-MM-DD range; 0 when from is after to. */
function rangeDays(from: string, to: string): number {
  const ms = Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)
  return ms < 0 ? 0 : Math.round(ms / 86_400_000) + 1
}

function FilterField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
    </div>
  )
}

/**
 * Lives below the page hero rather than inside it — the date picker's popover gets clipped by a
 * padded card edge, the same reason Rep Bills pulls its filters out.
 */
export function StockActivityFilterBar() {
  const f = useStockActivityFilters()
  const isFetching = useStockActivityIsFetching()
  const fetchProducts = useActiveProductsFetcher()
  const fetchUsers = useStockActivityUsersFetcher()
  const { exportExcel, isExporting } = useExportStockActivity()
  // Remounts the pickers on Reset so their displayed selection clears too.
  const [resetKey, setResetKey] = useState(0)

  const days = rangeDays(f.from, f.to)
  const rangeError =
    !f.from || !f.to
      ? 'Select a date range'
      : days === 0
        ? 'The start date must be on or before the end date'
        : days > MAX_RANGE_DAYS
          ? `The date range can span at most ${MAX_RANGE_DAYS} days (currently ${days})`
          : null

  const applied = f.appliedFilters
  const hasLoaded = applied !== null
  const isDirty =
    hasLoaded &&
    (applied.from !== f.from ||
      applied.to !== f.to ||
      applied.distributorId !== f.distributorId ||
      applied.productId !== f.productId ||
      applied.userId !== f.userId ||
      applied.transactionType !== f.transactionType ||
      applied.direction !== f.direction)

  const handleReset = () => {
    f.reset()
    setResetKey((k) => k + 1)
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-background p-4">
        <FilterField label="Date range">
          <CalendarDatePicker
            id="stock-activity-date-range"
            date={{
              from: f.from ? new Date(`${f.from}T00:00:00`) : undefined,
              to: f.to ? new Date(`${f.to}T00:00:00`) : undefined,
            }}
            onDateSelect={({ from, to }) => f.setDateRange(toColomboDateStr(from), toColomboDateStr(to))}
            numberOfMonths={2}
            variant="outline"
            className="h-9 w-fit cursor-pointer"
          />
        </FilterField>

        <FilterField label="Distributor">
          <AsyncSelect<DistributorDto>
            key={`distributor-${resetKey}`}
            label="Distributor"
            placeholder="All distributors"
            fetcher={fetchAllDistributorsForSelect}
            value={f.distributorId?.toString() ?? ''}
            onChange={(v) => f.setDistributorId(v ? Number(v) : null)}
            getOptionValue={(d) => d.id.toString()}
            getDisplayValue={(d) => (
              <span className="flex items-center gap-2 text-sm">
                {d.name}
                {!d.isActive && <Badge variant="outline" className="text-[10px]">Closed</Badge>}
              </span>
            )}
            renderOption={(d) => (
              <span className="flex items-center gap-2 py-0.5 text-sm font-medium">
                {d.name}
                {!d.isActive && <Badge variant="outline" className="text-[10px]">Closed</Badge>}
              </span>
            )}
            noResultsMessage="No distributors found"
            width="240px"
            triggerClassName="h-9"
            clearable
          />
        </FilterField>

        <FilterField label="Product">
          <AsyncSelect<ProductLookupDto>
            key={`product-${resetKey}`}
            label="Product"
            placeholder="All products"
            fetcher={fetchProducts}
            value={f.productId?.toString() ?? ''}
            onChange={(v) => f.setProductId(v ? Number(v) : null)}
            getOptionValue={(p) => p.id.toString()}
            getDisplayValue={(p) => (
              <span className="text-sm">
                <span className="font-mono text-xs">{p.code}</span> {p.itemDescription}
              </span>
            )}
            renderOption={(p) => (
              <div className="flex flex-col gap-0.5 py-0.5">
                <span className="text-sm font-medium">{p.itemDescription}</span>
                <span className="font-mono text-xs text-muted-foreground">{p.code}</span>
              </div>
            )}
            noResultsMessage="No products found"
            width="260px"
            triggerClassName="h-9"
            clearable
          />
        </FilterField>

        <FilterField label="User">
          <AsyncSelect<StockActivityUser>
            key={`user-${resetKey}`}
            label="User"
            placeholder="All users"
            fetcher={fetchUsers}
            value={f.userId?.toString() ?? ''}
            onChange={(v) => f.setUserId(v ? Number(v) : null)}
            getOptionValue={(u) => u.id.toString()}
            getDisplayValue={(u) => <span className="text-sm">{u.name}</span>}
            renderOption={(u) => <span className="py-0.5 text-sm">{u.name}</span>}
            noResultsMessage="No users found"
            width="200px"
            triggerClassName="h-9"
            clearable
          />
        </FilterField>

        <FilterField label="Transaction type">
          <Select
            value={f.transactionType ?? ALL}
            onValueChange={(v) => f.setTransactionType(v === ALL ? null : (v as StockTransactionType))}
          >
            <SelectTrigger className="h-9 w-52 text-sm">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All types</SelectItem>
              {STOCK_TRANSACTION_TYPES.map((t) => (
                <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FilterField>

        <FilterField label="Direction">
          <Select
            value={f.direction ?? ALL}
            onValueChange={(v) => f.setDirection(v === ALL ? null : (v as StockDirection))}
          >
            <SelectTrigger className="h-9 w-28 text-sm">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>In &amp; Out</SelectItem>
              <SelectItem value="In">In</SelectItem>
              <SelectItem value="Out">Out</SelectItem>
            </SelectContent>
          </Select>
        </FilterField>

        <div className="flex items-center gap-2">
          <Button onClick={f.applyFilters} disabled={!!rangeError || isFetching} className="h-9 gap-2">
            {isFetching ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Search className="h-3.5 w-3.5" />}
            {isFetching ? 'Loading...' : hasLoaded ? 'Reload' : 'Load Activity'}
          </Button>
          {hasLoaded && (
            <>
              <Button
                variant="outline"
                onClick={() => exportExcel(applied)}
                disabled={isExporting}
                className="h-9 gap-2"
              >
                {isExporting ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
                {isExporting ? 'Exporting...' : 'Export Excel'}
              </Button>
              <Button variant="ghost" onClick={handleReset} className="h-9 gap-1.5 text-muted-foreground">
                <RotateCcw className="h-3.5 w-3.5" />
                Reset
              </Button>
            </>
          )}
        </div>
      </div>

      {rangeError && <p className="text-xs text-destructive">{rangeError}</p>}
      {!rangeError && isDirty && (
        <p className="text-xs text-muted-foreground">
          Filters changed — press <span className="font-medium">Reload</span> to apply them. Export uses
          the loaded filters.
        </p>
      )}
    </div>
  )
}
