'use client'

import { useState } from 'react'
import {
  ChevronDown,
  FileDown,
  FileSpreadsheet,
  Map,
  Package,
  RotateCcw,
  Search,
  Users,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { Badge } from '@/components/ui/badge'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { toColomboDateStr } from '@/lib/utils/datetime'
import { fetchActiveDistributorsForSelect } from '@/features/distributor/actions/distributor.actions'
import { fetchActiveProductsForSelect } from '@/features/product/actions/product.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import type { ProductDto } from '@/features/product/schema/product.schema'
import {
  useAreasFetcher,
  useDivisionsFetcher,
  useRegionsFetcher,
  useRoutesFetcher,
  useSalesSummaryIsFetching,
  useTerritoriesFetcher,
  useUsersByRoleFetcher,
} from '../../hooks/sales-summary.hooks'
import {
  GROUP_BY_OPTIONS,
  PEOPLE_ROLES,
  ROLE_FILTER_KEY,
  type PeopleRole,
  type SalesSummaryGroupBy,
  type SalesSummaryResponse,
  type UserOption,
} from '../../schema/sales-summary.schema'
import { useSalesSummaryFilters } from '../../store'
import { exportSalesSummaryExcel, exportSalesSummaryPdf } from '../../lib/sales-summary-export'
import { IdSelect } from '../selects/id-select'

// Module-level so their identity is stable across renders — IdSelect memoizes on these, and an
// inline arrow here would defeat that and reintroduce the render loop.
const byName = (x: { name: string }) => x.name
const distributorPhone = (d: DistributorDto) => d.phone ?? undefined
const productName = (p: ProductDto) => p.itemDescription
const productCode = (p: ProductDto) => p.code
const userName = (u: UserOption) => u.name
const userSubtitle = (u: UserOption) => u.username

/**
 * Lives below the page hero rather than inside it — the date picker's popover gets clipped by a
 * padded card edge, the same reason Rep Bills and Rep Route History pull their filters out.
 *
 * The three filter sections are collapsed on mount and Radix unmounts closed content, so opening
 * the report issues NO dropdown requests until a section is actually expanded.
 */
export function SalesSummaryCriteria({ data }: { data?: SalesSummaryResponse }) {
  const f = useSalesSummaryFilters()
  const isFetching = useSalesSummaryIsFetching()

  const canLoad = !!f.from && !!f.to && f.from <= f.to
  const applied = f.appliedFilters
  const hasLoaded = applied !== null
  const hasData = !!data && data.rows.length > 0

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
      applied.asmId !== f.asmId ||
      applied.rsmId !== f.rsmId ||
      applied.nsmId !== f.nsmId ||
      applied.productId !== f.productId)

  const geoCount = [
    f.regionId, f.areaId, f.territoryId, f.divisionId, f.routeId, f.distributorId,
  ].filter((v) => v !== null).length
  const peopleCount = [f.salesRepId, f.supervisorId, f.asmId, f.rsmId, f.nsmId]
    .filter((v) => v !== null).length
  const productCount = f.productId !== null ? 1 : 0
  const totalCount = geoCount + peopleCount + productCount

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
            <Select value={f.groupBy} onValueChange={(v) => f.setGroupBy(v as SalesSummaryGroupBy)}>
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

            <Button variant="ghost" onClick={f.reset} className="h-9 gap-1.5 text-muted-foreground">
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
          {totalCount > 0 && (
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground">
                {totalCount} filter{totalCount === 1 ? '' : 's'} active
              </span>
              <Button
                variant="link"
                onClick={f.clearFilterIds}
                className="h-auto p-0 text-xs text-muted-foreground"
              >
                Clear all
              </Button>
            </div>
          )}

          <FilterSection title="Geography" icon={<Map className="h-3.5 w-3.5" />} count={geoCount}>
            <GeographyFilters />
          </FilterSection>

          <FilterSection title="People" icon={<Users className="h-3.5 w-3.5" />} count={peopleCount}>
            <PeopleFilters />
          </FilterSection>

          <FilterSection
            title="Product"
            icon={<Package className="h-3.5 w-3.5" />}
            count={productCount}
          >
            <Field label="Product">
              <IdSelect<ProductDto>
                label="Product"
                value={f.productId}
                onChange={(v) => f.setFilterId('productId', v)}
                fetcher={fetchActiveProductsForSelect}
                getName={productName}
                getSubtitle={productCode}
              />
            </Field>
          </FilterSection>
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

// ── Geography: strict Region → Area → Territory → Division → Route cascade ──

function GeographyFilters() {
  const f = useSalesSummaryFilters()

  // Each fetcher is keyed by its parent id and returns [] when the parent is unset, so a child can
  // never request before its parent is chosen.
  const regions = useRegionsFetcher()
  const areas = useAreasFetcher(f.regionId)
  const territories = useTerritoriesFetcher(f.areaId)
  const divisions = useDivisionsFetcher(f.territoryId)
  const routes = useRoutesFetcher(f.divisionId)

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
      <Field label="Region">
        <IdSelect
          label="Region"
          value={f.regionId}
          onChange={(v) => f.setGeoId('regionId', v)}
          fetcher={regions}
          getName={byName}
          preload
        />
      </Field>

      <CascadeField label="Area" parentLabel="region" enabled={f.regionId !== null}>
        <IdSelect
          label="Area"
          value={f.areaId}
          onChange={(v) => f.setGeoId('areaId', v)}
          fetcher={areas}
          getName={byName}
          preload
        />
      </CascadeField>

      <CascadeField label="Territory" parentLabel="area" enabled={f.areaId !== null}>
        <IdSelect
          label="Territory"
          value={f.territoryId}
          onChange={(v) => f.setGeoId('territoryId', v)}
          fetcher={territories}
          getName={byName}
          preload
        />
      </CascadeField>

      <CascadeField label="Division" parentLabel="territory" enabled={f.territoryId !== null}>
        <IdSelect
          label="Division"
          value={f.divisionId}
          onChange={(v) => f.setGeoId('divisionId', v)}
          fetcher={divisions}
          getName={byName}
          preload
        />
      </CascadeField>

      <CascadeField label="Route" parentLabel="division" enabled={f.divisionId !== null}>
        <IdSelect
          label="Route"
          value={f.routeId}
          onChange={(v) => f.setGeoId('routeId', v)}
          fetcher={routes}
          getName={byName}
          preload
        />
      </CascadeField>

      {/* Independent of the cascade — a distributor is resolved from the rep's territory. */}
      <Field label="Distributor">
        <IdSelect<DistributorDto>
          label="Distributor"
          value={f.distributorId}
          onChange={(v) => f.setFilterId('distributorId', v)}
          fetcher={fetchActiveDistributorsForSelect}
          getName={byName}
          getSubtitle={distributorPhone}
        />
      </Field>
    </div>
  )
}

// ── People: pick a role, then a user of that role ───────────────────────────

function PeopleFilters() {
  const f = useSalesSummaryFilters()
  const users = useUsersByRoleFetcher(f.role)

  const currentUserId = f.role ? f[ROLE_FILTER_KEY[f.role]] : null
  const roleLabel = PEOPLE_ROLES.find((r) => r.role === f.role)?.label ?? 'user'

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <Field label="Role">
        <Select
          value={f.role ?? ''}
          onValueChange={(v) => f.setRole((v || null) as PeopleRole | null)}
        >
          <SelectTrigger className="h-9 w-full">
            <SelectValue placeholder="Any role" />
          </SelectTrigger>
          <SelectContent>
            {PEOPLE_ROLES.map((r) => (
              <SelectItem key={r.role} value={r.role}>
                {r.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <CascadeField label={roleLabel} parentLabel="role" enabled={f.role !== null}>
        <IdSelect<UserOption>
          label={roleLabel}
          value={currentUserId}
          onChange={f.setRoleUserId}
          fetcher={users}
          getName={userName}
          getSubtitle={userSubtitle}
        />
      </CascadeField>
    </div>
  )
}

// ── Layout primitives ───────────────────────────────────────────────────────

function FilterSection({
  title,
  icon,
  count,
  children,
}: {
  title: string
  icon: React.ReactNode
  count: number
  children: React.ReactNode
}) {
  const [open, setOpen] = useState(false)

  return (
    <Collapsible open={open} onOpenChange={setOpen} className="rounded-md border">
      <CollapsibleTrigger className="flex w-full items-center gap-2 px-3 py-2 text-left hover:bg-muted/40">
        {icon}
        <span className="text-xs font-medium">{title}</span>
        {/* Shown on the header so a collapsed section never hides an applied filter. */}
        {count > 0 && (
          <Badge variant="secondary" className="h-5 px-1.5 text-[10px]">
            {count}
          </Badge>
        )}
        <ChevronDown
          className={cn(
            'ml-auto h-3.5 w-3.5 text-muted-foreground transition-transform',
            open && 'rotate-180'
          )}
        />
      </CollapsibleTrigger>
      {/* Radix unmounts this when closed — that is what keeps page load at zero requests. */}
      <CollapsibleContent className="border-t px-3 py-3">{children}</CollapsibleContent>
    </Collapsible>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex w-full min-w-0 flex-col gap-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
    </div>
  )
}

/**
 * A cascade level. Until its parent is chosen it renders an inert stub rather than a disabled
 * AsyncSelect — AsyncSelect fetches on mount regardless of `disabled`, so mounting one here would
 * put back exactly the eager request this redesign removes.
 */
function CascadeField({
  label,
  parentLabel,
  enabled,
  children,
}: {
  label: string
  parentLabel: string
  enabled: boolean
  children: React.ReactNode
}) {
  return (
    <div className="flex w-full min-w-0 flex-col gap-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {enabled ? (
        children
      ) : (
        <div className="flex h-9 items-center rounded-md border border-dashed px-3 text-xs text-muted-foreground/70">
          Select a {parentLabel} first
        </div>
      )}
    </div>
  )
}
