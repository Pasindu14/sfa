'use client'

import { useEffect } from 'react'
import { AlertCircle, BarChart3, Info, Loader2 } from 'lucide-react'
import { useSalesSummary } from '../../hooks/sales-summary.hooks'
import { useSalesSummaryFilters } from '../../store'
import { GROUP_BY_OPTIONS } from '../../schema/sales-summary.schema'
import { SalesSummaryCriteria } from '../filters/sales-summary-criteria'
import { SalesSummaryTable } from '../table/sales-summary-table'

export function SalesSummaryPage() {
  const { appliedFilters, reset } = useSalesSummaryFilters()
  const { data, isFetching, isError, error } = useSalesSummary()

  // Start clean on mount so a stale range or grouping never auto-runs an expensive aggregate.
  useEffect(() => {
    reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const groupLabel = data
    ? (GROUP_BY_OPTIONS.find((o) => o.value === data.groupBy)?.label ?? data.groupBy)
    : ''

  return (
    <div className="flex flex-col gap-6 overflow-x-hidden p-6">
      <div className="rounded-lg bg-muted/90 p-10">
        <h1 className="text-3xl font-bold tracking-tight">Sales Summary</h1>
        <p className="text-muted-foreground">
          Targets against gross sales, returns, discounts and net sales for a date range
        </p>
      </div>

      <SalesSummaryCriteria data={data} />

      {!appliedFilters ? (
        <EmptyState />
      ) : isFetching && !data ? (
        <LoadingState />
      ) : isError ? (
        <MessageState
          icon={<AlertCircle className="h-8 w-8 text-destructive/60" />}
          title="Could not load the sales summary"
          subtitle={error instanceof Error ? error.message : 'Please try again.'}
        />
      ) : data ? (
        <div className="flex flex-col gap-3">
          <div>
            <h2 className="text-xl font-semibold tracking-tight">
              By {groupLabel} ({data.groupCount} rows)
            </h2>
            <p className="text-sm text-muted-foreground">
              {data.from} to {data.to}
            </p>
          </div>

          {/* Say WHY the target columns are dashes rather than leaving the reader to guess. */}
          {!data.targetsAvailable && data.targetsUnavailableReason && (
            <div className="flex items-start gap-2 rounded-md border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-700 dark:text-amber-400">
              <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
              <span>{data.targetsUnavailableReason}</span>
            </div>
          )}

          {data.rows.length > 0 ? (
            <SalesSummaryTable data={data} />
          ) : (
            <MessageState
              icon={<BarChart3 className="h-8 w-8 text-muted-foreground/40" />}
              title="No approved sales in this range"
              subtitle="Only bills approved by the distributor are counted. Try a wider date range or fewer filters."
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
      icon={<BarChart3 className="h-8 w-8 text-muted-foreground/40" />}
      title="Pick a date range and a grouping, then press Load report"
      subtitle="Quantities are shown in packs. Only distributor-approved bills are counted."
    />
  )
}

function LoadingState() {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground/50" />
      <p className="text-sm font-medium text-muted-foreground">Building sales summary…</p>
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
      <p className="text-sm font-medium text-muted-foreground">{title}</p>
      {subtitle && <p className="text-xs text-muted-foreground/60">{subtitle}</p>}
    </div>
  )
}
