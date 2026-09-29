'use client'

import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { formatColombo } from '@/lib/utils/datetime'
import { useDashboardBreakdown } from '../hooks/dashboard.hooks'
import type { DashboardBreakdown, DashboardRanked } from '../schema/dashboard.schema'
import { Amount, Panel, Section, SectionError, Stale } from './dashboard-cards'
import { count, percent } from './format'

// The four month-to-date panels fed by the breakdown section. Each owns its loading and error
// state, so the page lays them out wherever they fit best.

const monthOf = (d: DashboardBreakdown | undefined) =>
  d ? formatColombo(`${d.monthStart}T00:00:00+05:30`, 'MMMM') : 'this month'

/** Shared shell: heading, skeleton, one-line retry, and the stale dim while a new day loads. */
function BreakdownSection({
  title,
  aside,
  className,
  skeletonRows = 6,
  children,
}: {
  title: string
  aside?: React.ReactNode
  className?: string
  skeletonRows?: number
  children: (d: DashboardBreakdown) => React.ReactNode
}) {
  const { data, isPending, isPlaceholderData, refetch } = useDashboardBreakdown()

  return (
    <Section title={title} aside={aside} className={cn('flex flex-col', className)}>
      {isPending ? (
        <Panel className="flex-1 space-y-4 p-5">
          {Array.from({ length: skeletonRows }, (_, i) => (
            <Skeleton key={i} className="h-7 w-full" />
          ))}
        </Panel>
      ) : !data ? (
        <SectionError what={title} onRetry={() => refetch()} className="flex-1" />
      ) : (
        <Stale stale={isPlaceholderData} className="flex flex-1 flex-col">
          {children(data)}
        </Stale>
      )}
    </Section>
  )
}

// ── Ranked bars ─────────────────────────────────────────────────────────────

/**
 * A ranked list with a bar under each name, scaled to the leader so the gaps between entries read
 * at a glance. The rank number is real information here — the list is an ordering.
 */
function RankedBars({
  rows,
  secondary,
  barTone,
  footer,
  empty,
}: {
  rows: DashboardRanked[]
  secondary: (r: DashboardRanked) => React.ReactNode
  barTone?: (r: DashboardRanked) => string
  footer?: React.ReactNode
  empty: string
}) {
  const top = Math.max(...rows.map((r) => r.revenue), 0)

  if (rows.length === 0 || top <= 0) {
    return <Panel className="flex flex-1 items-center justify-center p-8 text-center text-sm text-muted-foreground">{empty}</Panel>
  }

  return (
    <Panel className="flex flex-1 flex-col p-5">
      <ol className="space-y-3.5">
        {rows.map((r, i) => (
          <li key={r.id ?? r.name} className="grid grid-cols-[1.25rem_minmax(0,1fr)] gap-x-2">
            <span className="pt-px font-report text-xs tabular-nums text-muted-foreground">{i + 1}</span>
            <div className="min-w-0">
              <div className="flex items-baseline justify-between gap-3">
                <span className="truncate text-sm" title={r.name}>
                  {r.name}
                </span>
                <Amount value={r.revenue} className="shrink-0 text-sm" />
              </div>
              <div className="mt-1.5 flex items-center gap-3">
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-muted" aria-hidden>
                  <div
                    className={cn('h-full rounded-full', barTone?.(r) ?? 'bg-primary')}
                    style={{ width: `${Math.max((r.revenue / top) * 100, 0)}%`, minWidth: r.revenue > 0 ? 3 : 0 }}
                  />
                </div>
                <span className="w-[8.5rem] shrink-0 text-right text-xs text-muted-foreground">{secondary(r)}</span>
              </div>
            </div>
          </li>
        ))}
      </ol>
      {footer && <div className="mt-auto pt-4">{footer}</div>}
    </Panel>
  )
}

export function TopProducts({ className }: { className?: string }) {
  return (
    <BreakdownSection title="Top products" aside="By revenue this month" className={className}>
      {(d) => (
        <RankedBars
          rows={d.products}
          empty={`No products sold in ${monthOf(d)} yet.`}
          secondary={(r) => (
            <span className="font-report tabular-nums">
              {count(Math.round(r.quantity))} {Math.round(r.quantity) === 1 ? 'pack' : 'packs'}, {percent(r.sharePercent, 0)}
            </span>
          )}
        />
      )}
    </BreakdownSection>
  )
}

export function RepLeaderboard({ className }: { className?: string }) {
  return (
    <BreakdownSection title="Rep leaderboard" aside="Against each rep's target to date" className={className}>
      {(d) => (
        <RankedBars
          rows={d.reps}
          empty={`No rep has an approved bill in ${monthOf(d)} yet.`}
          // Solid once the rep is on pace; a tint while behind; neutral when there's no target.
          barTone={(r) =>
            r.achievementPercent === null ? 'bg-primary/70' : r.achievementPercent >= 100 ? 'bg-primary' : 'bg-primary/40'
          }
          secondary={(r) =>
            r.achievementPercent === null ? (
              'No target'
            ) : (
              <span className={cn('font-report tabular-nums', r.achievementPercent >= 100 && 'font-medium text-primary')}>
                {percent(r.achievementPercent, 0)} of target
              </span>
            )
          }
        />
      )}
    </BreakdownSection>
  )
}

export function DistributorShare({ className }: { className?: string }) {
  return (
    <BreakdownSection title="Sales by distributor" aside="Share of this month's revenue" className={className}>
      {(d) => (
        <RankedBars
          rows={d.distributors}
          empty={`No distributor has sales in ${monthOf(d)} yet.`}
          secondary={(r) => <span className="font-report tabular-nums">{percent(r.sharePercent, 1)} of sales</span>}
          footer={
            d.otherDistributors && (
              <div className="flex items-baseline justify-between gap-3 border-t pt-3 text-sm text-muted-foreground">
                <span>
                  {count(d.otherDistributors.count)} other {d.otherDistributors.count === 1 ? 'distributor' : 'distributors'}
                </span>
                <span className="flex items-baseline gap-2">
                  <span className="font-report text-xs tabular-nums">{percent(d.otherDistributors.sharePercent, 1)}</span>
                  <Amount value={d.otherDistributors.revenue} />
                </span>
              </div>
            )
          }
        />
      )}
    </BreakdownSection>
  )
}

// ── Visits donut ────────────────────────────────────────────────────────────

const REASON_LABEL: Record<string, string> = {
  OutletClosed: 'Shop was closed',
  OwnerAbsent: 'Owner not there',
  CreditIssue: 'Credit or payment issue',
  NoOrder: 'Had stock, no order',
  OutOfStock: 'Out of stock',
}

/** No-sale reasons step down in strength from the sale colour, largest reason darkest. */
const REASON_OPACITY = [0.55, 0.4, 0.28, 0.18, 0.1]

export function VisitOutcomes({ className }: { className?: string }) {
  return (
    <BreakdownSection title="How visits ended" aside="This month" className={className} skeletonRows={5}>
      {(d) => <VisitDonut d={d} />}
    </BreakdownSection>
  )
}

function VisitDonut({ d }: { d: DashboardBreakdown }) {
  const v = d.visits
  const total = v.saleVisits + v.noSaleVisits

  if (total === 0) {
    return (
      <Panel className="flex flex-1 items-center justify-center p-8 text-center text-sm text-muted-foreground">
        No visits recorded in {monthOf(d)} yet.
      </Panel>
    )
  }

  const segments = [
    { key: 'sale', label: 'Sale made', value: v.saleVisits, cls: 'stroke-primary', opacity: 1, swatch: 'bg-primary' },
    ...v.reasons.map((r, i) => ({
      key: r.reason,
      label: REASON_LABEL[r.reason] ?? r.reason,
      value: r.count,
      cls: 'stroke-foreground',
      opacity: REASON_OPACITY[i] ?? 0.08,
      swatch: 'bg-foreground',
    })),
  ].filter((s) => s.value > 0)

  // Circumference 100 on a radius of 15.915 makes each dash length a straight percentage.
  const R = 15.915
  let offset = 0

  return (
    <Panel className="flex flex-1 flex-col gap-6 p-5 sm:flex-row sm:items-center">
      <div className="relative mx-auto h-40 w-40 shrink-0">
        <svg viewBox="0 0 36 36" className="h-full w-full -rotate-90" role="img"
          aria-label={segments.map((s) => `${s.label} ${s.value}`).join(', ')}
        >
          <circle cx="18" cy="18" r={R} fill="none" className="stroke-muted" strokeWidth="3.2" />
          {segments.map((s) => {
            const len = (s.value / total) * 100
            // A hairline gap between slices, unless a slice is the whole ring.
            const gap = segments.length > 1 ? 0.6 : 0
            const el = (
              <circle
                key={s.key}
                cx="18"
                cy="18"
                r={R}
                fill="none"
                className={s.cls}
                strokeOpacity={s.opacity}
                strokeWidth="3.2"
                strokeDasharray={`${Math.max(len - gap, 0)} ${100 - Math.max(len - gap, 0)}`}
                strokeDashoffset={-offset}
              />
            )
            offset += len
            return el
          })}
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center text-center">
          <span className="font-report text-3xl font-light tabular-nums">{percent(v.salePercent, 0)}</span>
          <span className="mt-0.5 max-w-[6.5rem] text-[11px] leading-tight text-muted-foreground">of visits ended in a sale</span>
        </div>
      </div>

      <ul className="min-w-0 flex-1 space-y-2.5 text-sm">
        {segments.map((s) => (
          <li key={s.key} className="flex items-center gap-2.5">
            <span aria-hidden className={cn('h-2.5 w-2.5 shrink-0 rounded-[3px]', s.swatch)} style={{ opacity: s.opacity }} />
            <span className={cn('min-w-0 flex-1 truncate', s.key === 'sale' ? 'font-medium' : 'text-muted-foreground')}>
              {s.label}
            </span>
            <span className="font-report tabular-nums">{count(s.value)}</span>
            <span className="w-9 text-right font-report text-xs tabular-nums text-muted-foreground">
              {percent((s.value / total) * 100, 0)}
            </span>
          </li>
        ))}
        <li className="border-t pt-2.5 text-xs text-muted-foreground">
          {count(total)} visits in total, {count(v.noSaleVisits)} without a sale
        </li>
      </ul>
    </Panel>
  )
}
