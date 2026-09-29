'use client'

import { useEffect } from 'react'
import { AlertCircle, BarChart3, CalendarDays, Layers, Loader2 } from 'lucide-react'
import { useSalesSummary } from '../../hooks/sales-summary.hooks'
import { useSalesSummaryFilters } from '../../store'
import { dimensionsOf, groupByLabel } from '../../schema/sales-summary.schema'
import { SalesSummaryCriteria } from '../filters/sales-summary-criteria'
import { SalesSummaryHeadline } from '../summary/sales-summary-headline'
import { SalesSummaryTable } from '../table/sales-summary-table'

/** "1 Jan – 31 Dec 2026" from two ISO strings, without pulling in a formatter. */
function readableRange(from: string, to: string): string {
  const fmt = (iso: string, withYear: boolean) => {
    const d = new Date(`${iso}T00:00:00`)
    return d.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      ...(withYear ? { year: 'numeric' } : {}),
    })
  }
  const sameYear = from.slice(0, 4) === to.slice(0, 4)
  return `${fmt(from, !sameYear)} – ${fmt(to, true)}`
}

export function SalesSummaryPage() {
  const { appliedFilters, reset } = useSalesSummaryFilters()
  const { data, isFetching, isError, error } = useSalesSummary()

  // Start clean on mount so a stale range or grouping never auto-runs an expensive aggregate.
  useEffect(() => {
    reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const dims = data ? dimensionsOf(data) : []
  const groupLabel = groupByLabel(dims)

  return (
    <div className="flex flex-col gap-6 overflow-x-hidden p-6">
      {/* Same hero as the other report pages (Bin Card, Rep Bills). Title only — the criteria live
          in their own card below, because the date picker's popover is clipped by this padding. */}
      <div className="rounded-lg bg-muted/90 p-10">
        <h1 className="text-3xl font-bold tracking-tight">Sales Summary</h1>
        <p className="text-muted-foreground">
          Targets against gross sales, returns, discounts and net sales — grouped any way you need
        </p>
        {data && (
          <div className="mt-4 flex flex-wrap gap-2 text-xs">
            <HeroChip icon={<CalendarDays className="h-3.5 w-3.5" />}>
              {readableRange(data.from, data.to)}
            </HeroChip>
            <HeroChip icon={<Layers className="h-3.5 w-3.5" />}>{groupLabel}</HeroChip>
          </div>
        )}
      </div>

      <SalesSummaryCriteria data={data} />

      {!appliedFilters ? (
        <EmptyState />
      ) : isFetching && !data ? (
        <LoadingState />
      ) : isError ? (
        <MessageState
          icon={<AlertCircle className="h-7 w-7 text-destructive/60" />}
          title="Could not load the sales summary"
          subtitle={error instanceof Error ? error.message : 'Try again.'}
        />
      ) : data ? (
        <div className="flex flex-col gap-4">
          {/* The Target tile also says why target columns are dashes, when they are. */}
          <SalesSummaryHeadline data={data} />

          {data.rows.length > 0 ? (
            // Remounting on the grouping/range resets paging: page 4 of the old grouping is
            // meaningless against a new one.
            <SalesSummaryTable
              key={`${dims.join('-')}-${data.from}-${data.to}`}
              data={data}
            />
          ) : (
            <MessageState
              icon={<BarChart3 className="h-7 w-7 text-muted-foreground/40" />}
              title="No approved sales in this range"
              subtitle="Only bills approved by the distributor are counted. Widen the date range or remove a filter."
            />
          )}
        </div>
      ) : null}
    </div>
  )
}

function EmptyState() {
  return (
    <MessageState
      icon={<BarChart3 className="h-7 w-7 text-muted-foreground/40" />}
      title="Choose a date range and a grouping, then load the report"
      subtitle="Quantities are in packs. Only distributor-approved bills are counted."
    />
  )
}

function LoadingState() {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
      <Loader2 className="h-7 w-7 animate-spin text-muted-foreground/50 motion-reduce:animate-none" />
      <p className="text-sm text-muted-foreground">Building the report…</p>
    </div>
  )
}

function MessageState({
  icon,
  title,
  subtitle,
}: {
  icon: React.ReactNode
  title: string
  subtitle?: string
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
      {icon}
      <p className="text-sm font-medium">{title}</p>
      {subtitle && <p className="max-w-md text-xs text-muted-foreground">{subtitle}</p>}
    </div>
  )
}

function HeroChip({ icon, children }: { icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-foreground/10 bg-background/70 px-3 py-1 font-medium text-foreground/80">
      {icon}
      {children}
    </span>
  )
}
