'use client'

import { useEffect } from 'react'
import { FileText, Loader2, AlertCircle } from 'lucide-react'
import { useBinCard } from '../../hooks/bin-card.hooks'
import { useBinCardFilters } from '../../store'
import { BinCardCriteria } from '../bin-card-criteria'
import { BinCardTable } from '../table/bin-card-table'

export function BinCardPage() {
  const { appliedFilters, reset } = useBinCardFilters()
  const { data, isFetching, isError, error } = useBinCard()

  // Start clean on mount so a stale distributor/range never auto-runs.
  useEffect(() => {
    reset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="flex flex-col gap-6 p-6 overflow-x-hidden">
      <div className="rounded-lg bg-muted/90 p-10">
        <h1 className="text-3xl font-bold tracking-tight">Bin Card</h1>
        <p className="text-muted-foreground">
          Per-SKU stock movement &amp; balance report for a distributor
        </p>
      </div>

      <BinCardCriteria data={data} />

      {!appliedFilters ? (
        <EmptyState />
      ) : isFetching && !data ? (
        <LoadingState />
      ) : isError ? (
        <MessageState
          icon={<AlertCircle className="h-8 w-8 text-destructive/60" />}
          title="Could not load the bin card"
          subtitle={error instanceof Error ? error.message : 'Please try again.'}
        />
      ) : data ? (
        <div className="flex flex-col gap-3">
          <div>
            <h2 className="text-xl font-semibold tracking-tight">
              Bin Card Details ({data.recordCount} records)
            </h2>
            <p className="text-sm text-muted-foreground">
              {data.distributorName} &nbsp;|&nbsp; {data.from} to {data.to}
            </p>
          </div>
          {data.rows.length > 0 ? (
            <BinCardTable data={data} />
          ) : (
            <MessageState
              icon={<FileText className="h-8 w-8 text-muted-foreground/40" />}
              title="No stock activity in this range"
              subtitle="Try a wider date range or a different distributor."
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
      icon={<FileText className="h-8 w-8 text-muted-foreground/40" />}
      title="Select a distributor and date range, then click Search"
      subtitle="The bin card shows opening stock, every movement, end stock and variance per SKU."
    />
  )
}

function LoadingState() {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground/50" />
      <p className="text-sm font-medium text-muted-foreground">Building bin card…</p>
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
