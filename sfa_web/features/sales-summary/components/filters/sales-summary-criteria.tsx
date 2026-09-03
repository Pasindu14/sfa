'use client'

import { FileDown, FileSpreadsheet, RotateCcw, Search, SlidersHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toColomboDateStr } from '@/lib/utils/datetime'
import { fetchActiveDistributorsForSelect } from '@/features/distributor/actions/distributor.actions'
import { fetchActiveProductsForSelect } from '@/features/product/actions/product.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import type { ProductDto } from '@/features/product/schema/product.schema'
import {
  fetchAreasForSelect,
  fetchDivisionsForSelect,
  fetchRegionsForSelect,
  fetchRoutesForSelect,
  fetchTerritoriesForSelect,
} from '../../actions/sales-summary.actions'
import { useSalesSummaryIsFetching, useUsersByRoleFetcher } from '../../hooks/sales-summary.hooks'
import {
  GROUP_BY_OPTIONS,
  type SalesSummaryGroupBy,
  type SalesSummaryResponse,
  type UserOption,
} from '../../schema/sales-summary.schema'
import { useSalesSummaryFilters } from '../../store'
import { exportSalesSummaryExcel, exportSalesSummaryPdf } from '../../lib/sales-summary-export'
import { IdSelect } from '../selects/id-select'

/**
 * Lives below the page hero rather than inside it — the date picker's popover gets clipped by a
 * padded card edge, the same reason Rep Bills and Rep Route History pull their filters out.
 */
export function SalesSummaryCriteria({ data }: { data?: SalesSummaryResponse }) {
  const f = useSalesSummaryFilters()
  const isFetching = useSalesSummaryIsFetching()

  const fetchSupervisors = useUsersByRoleFetcher('Supervisor')
  const fetchSalesReps = useUsersByRoleFetcher('SalesRep')

  const canLoad = !!f.from && !!f.to && f.from <= f.to
  const applied = f.appliedFilters
  const hasLoaded = applied !== null
  const hasData = !!data && data.rows.length > 0

  // The controls have moved on from what the table is showing. Say so, rather than letting the
  // rows silently disagree with the filters above them.
  const isDirty =
    applied !== null &&
    (applied.from !== f.from ||
      applied.to !== f.to ||
      applied.groupBy !== f.groupBy ||
      applied.regionId !== f.regionId ||
      applied.areaId !== f.areaId ||
      applied.territoryId !== f.territoryId ||
      applied.divisionId !== f.divisionId ||
      applied.routeId !== f.routeId ||
      applied.distributorId !== f.distributorId ||
      applied.salesRepId !== f.salesRepId ||
      applied.supervisorId !== f.supervisorId ||
      applied.productId !== f.productId)

  const activeFilterCount = [
    f.regionId, f.areaId, f.territoryId, f.divisionId, f.routeId,
    f.distributorId, f.salesRepId, f.supervisorId, f.productId,
  ].filter((v) => v !== null).length

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-4 rounded-lg border bg-background p-4">
        {/* Primary row — the two decisions that define the report */}
        <div className="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-end">
          <div className="flex w-full flex-col gap-1.5 sm:w-auto">
            <label className="text-xs font-medium text-muted-foreground">Date range</label>
            <CalendarDatePicker
              id="sales-summary-date-range"
              date={{
                from: f.from ? new Date(`${f.from}T00:00:00`) : undefined,
                to: f.to ? new Date(`${f.to}T00:00:00`) : undefined,
              }}
              onDateSelect={({ from, to }) =>
                f.setDateRange(toColomboDateStr(from), toColomboDateStr(to))
              }
              numberOfMonths={2}
              variant="outline"
              className="h-9 w-full cursor-pointer sm:w-fit"
            />
          </div>

          <div className="flex w-full flex-col gap-1.5 sm:w-56">
            <label className="text-xs font-medium text-muted-foreground">Group by</label>
            <Select
              value={f.groupBy}
              onValueChange={(v) => f.setGroupBy(v as SalesSummaryGroupBy)}
            >
              <SelectTrigger className="h-9 w-full">
                <SelectValue placeholder="Group by" />
              </SelectTrigger>
              <SelectContent>
                {GROUP_BY_OPTIONS.map((o) => (
                  <SelectItem key={o.value} value={o.value}>
                    {o.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center gap-2">
            <Button
              onClick={f.applyFilters}
              disabled={!canLoad || isFetching}
              className="h-9 gap-2 sm:w-36"
            >
              {isFetching ? <Spinner className="h-4 w-4" /> : <Search className="h-4 w-4" />}
              {isFetching ? 'Loading…' : hasLoaded ? 'Reload' : 'Load report'}
            </Button>

            <Button
              variant="ghost"
              onClick={f.reset}
              className="h-9 gap-1.5 text-muted-foreground"
            >
              <RotateCcw className="h-3.5 w-3.5" />
              Reset
            </Button>

            <Button
              variant="outline"
              disabled={!hasData}
              onClick={() => data && exportSalesSummaryPdf(data)}
              className="h-9 gap-1.5"
            >
              <FileDown className="h-3.5 w-3.5" />
              PDF
            </Button>

            <Button
              variant="outline"
              disabled={!hasData}
              onClick={() => data && void exportSalesSummaryExcel(data)}
              className="h-9 gap-1.5"
            >
              <FileSpreadsheet className="h-3.5 w-3.5" />
              Excel
            </Button>
          </div>
        </div>

        {/* Optional narrowing filters — every one is AND-ed server-side */}
        <div className="flex flex-col gap-2 border-t pt-3">
          <div className="flex items-center gap-2">
            <SlidersHorizontal className="h-3.5 w-3.5 text-muted-foreground" />
            <span className="text-xs font-medium text-muted-foreground">
              Narrow the report {activeFilterCount > 0 && `(${activeFilterCount} active)`}
            </span>
            {activeFilterCount > 0 && (
              <Button
                variant="link"
                onClick={f.clearFilterIds}
                className="h-auto p-0 text-xs text-muted-foreground"
              >
                Clear filters
              </Button>
            )}
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
            <Filter label="Region">
              <IdSelect
                label="Region"
                value={f.regionId}
                onChange={(v) => f.setFilterId('regionId', v)}
                fetcher={fetchRegionsForSelect}
                getName={(r) => r.name}
                preload
              />
            </Filter>

            <Filter label="Area">
              <IdSelect
                label="Area"
                value={f.areaId}
                onChange={(v) => f.setFilterId('areaId', v)}
                fetcher={fetchAreasForSelect}
                getName={(a) => a.name}
                preload
              />
            </Filter>

            <Filter label="Territory">
              <IdSelect
                label="Territory"
                value={f.territoryId}
                onChange={(v) => f.setFilterId('territoryId', v)}
                fetcher={fetchTerritoriesForSelect}
                getName={(t) => t.name}
                preload
              />
            </Filter>

            <Filter label="Division">
              <IdSelect
                label="Division"
                value={f.divisionId}
                onChange={(v) => f.setFilterId('divisionId', v)}
                fetcher={fetchDivisionsForSelect}
                getName={(d) => d.name}
                preload
              />
            </Filter>

            <Filter
              label="Route"
              hint={f.routeId !== null ? 'Targets are hidden while a route is selected' : undefined}
            >
              <IdSelect
                label="Route"
                value={f.routeId}
                onChange={(v) => f.setFilterId('routeId', v)}
                fetcher={fetchRoutesForSelect}
                getName={(r) => r.name}
                preload
              />
            </Filter>

            <Filter label="Distributor">
              <IdSelect<DistributorDto>
                label="Distributor"
                value={f.distributorId}
                onChange={(v) => f.setFilterId('distributorId', v)}
                fetcher={fetchActiveDistributorsForSelect}
                getName={(d) => d.name}
                getSubtitle={(d) => d.phone ?? undefined}
              />
            </Filter>

            <Filter label="Supervisor">
              <IdSelect<UserOption>
                label="Supervisor"
                value={f.supervisorId}
                onChange={(v) => f.setFilterId('supervisorId', v)}
                fetcher={fetchSupervisors}
                getName={(u) => u.name}
                getSubtitle={(u) => u.username}
              />
            </Filter>

            <Filter label="Sales rep">
              <IdSelect<UserOption>
                label="Sales rep"
                value={f.salesRepId}
                onChange={(v) => f.setFilterId('salesRepId', v)}
                fetcher={fetchSalesReps}
                getName={(u) => u.name}
                getSubtitle={(u) => u.username}
              />
            </Filter>

            <Filter label="Product">
              <IdSelect<ProductDto>
                label="Product"
                value={f.productId}
                onChange={(v) => f.setFilterId('productId', v)}
                fetcher={fetchActiveProductsForSelect}
                getName={(p) => p.itemDescription}
                getSubtitle={(p) => p.code}
              />
            </Filter>
          </div>
        </div>
      </div>

      {isDirty && (
        <p className="text-xs text-muted-foreground">
          Filters changed — press <span className="font-medium">Reload</span> to apply them
        </p>
      )}
    </div>
  )
}

function Filter({
  label,
  hint,
  children,
}: {
  label: string
  hint?: string
  children: React.ReactNode
}) {
  return (
    <div className="flex w-full min-w-0 flex-col gap-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
      {hint && <span className="text-[11px] text-amber-600 dark:text-amber-500">{hint}</span>}
    </div>
  )
}
