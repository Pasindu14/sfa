'use client'

import { useState } from 'react'
import Link from 'next/link'
import { CalendarDays, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { formatColombo, toColomboDateStr } from '@/lib/utils/datetime'
import {
  useDashboardActivity,
  useDashboardIsFetching,
  useDashboardSales,
  useDashboardTrend,
  useRefreshDashboard,
} from '../hooks/dashboard.hooks'
import { useDashboardStore } from '../store/dashboard.store'
import type { DashboardActivity, DashboardSales, DashboardSalesBlock } from '../schema/dashboard.schema'
import { Amount, Meter, Panel, Section, SectionError, Stale } from './dashboard-cards'
import { count, money, percent } from './format'
import { MonthRibbon, type RibbonMode } from './month-ribbon'
import { DistributorShare, RepLeaderboard, TopProducts, VisitOutcomes } from './breakdown-charts'

/** Colombo date string → a value formatColombo renders as that same calendar day. */
const day = (d: string) => `${d}T00:00:00+05:30`

const SOLID = 'bg-primary'
const TINT = 'bg-primary/45'
const TINT_LIGHT = 'bg-primary/15'

/**
 * The admin dashboard. Its four API sections (sales, activity, trend, breakdown) load in parallel
 * as separate queries; each block renders the moment its own data arrives, so nothing waits on
 * anything else.
 *
 * Full width: one column on phones, two on laptops, three on wide screens. The third panel of each
 * row spans both columns at the two-column size so no row is left half empty.
 */
export function DashboardPage() {
  return (
    <div className="flex w-full flex-1 flex-col gap-10 p-4 pt-0 md:p-6 md:pt-2 2xl:px-10">
      <Header />
      <Runway />
      <div className="grid gap-x-6 gap-y-10 lg:grid-cols-2 2xl:grid-cols-3">
        <Ledger />
        <FieldAndOutlets />
        <VisitOutcomes className="lg:col-span-2 2xl:col-span-1" />
      </div>
      <div className="grid gap-x-6 gap-y-10 lg:grid-cols-2 2xl:grid-cols-3">
        <TopProducts />
        <RepLeaderboard />
        <DistributorShare className="lg:col-span-2 2xl:col-span-1" />
      </div>
      <Regions />
    </div>
  )
}

function useShownDate() {
  const date = useDashboardStore((s) => s.date)
  const today = toColomboDateStr(new Date())
  const shown = date ?? today
  return { shown, today, isToday: shown === today }
}

/** "Today" for the current day, otherwise "25 Sep". */
function dayName(date: string, isToday: boolean) {
  return isToday ? 'Today' : formatColombo(day(date), 'd MMM')
}

// ── Header ──────────────────────────────────────────────────────────────────

function Header() {
  const setDate = useDashboardStore((s) => s.setDate)
  const { shown, today, isToday } = useShownDate()
  const isFetching = useDashboardIsFetching()
  const refresh = useRefreshDashboard()
  const { data: sales } = useDashboardSales()

  return (
    <header className="flex flex-col gap-4 pt-2 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-[28px] font-medium leading-tight tracking-tight">Sales dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {formatColombo(day(shown), 'EEEE d MMMM yyyy')}
          {sales && `, updated ${formatColombo(sales.generatedAtUtc, 'HH:mm')}`}
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative">
          <CalendarDays className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="date"
            aria-label="Show the dashboard for"
            className="h-9 w-[168px] pl-8 font-report tabular-nums"
            value={shown}
            max={today}
            onChange={(e) => {
              const v = e.target.value
              // Cleared or today → follow "today", which keeps auto-refreshing.
              setDate(!v || v >= today ? null : v)
            }}
          />
        </div>
        {!isToday && (
          <Button variant="outline" size="sm" className="h-9" onClick={() => setDate(null)}>
            Back to today
          </Button>
        )}
        <Button
          variant="ghost"
          size="sm"
          className="h-9 gap-2 text-muted-foreground"
          onClick={refresh}
          disabled={isFetching}
          title={isToday ? 'Refreshes on its own every 2 minutes' : undefined}
        >
          <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin motion-reduce:animate-none')} />
          Refresh
        </Button>
      </div>
    </header>
  )
}

// ── Runway: the month so far, today, and every day of the month ─────────────

function Runway() {
  const sales = useDashboardSales()
  const trend = useDashboardTrend()
  const { shown, isToday } = useShownDate()
  const [mode, setMode] = useState<RibbonMode>('daily')

  const s = sales.data
  const monthTarget = s?.monthTarget.targetValue ?? null

  if (sales.isError && !s && trend.isError && !trend.data) {
    return <SectionError what="The month's sales" onRetry={() => { sales.refetch(); trend.refetch() }} className="h-[420px]" />
  }

  const monthName = formatColombo(day(s?.monthStart ?? trend.data?.monthStart ?? shown), 'MMMM')

  return (
    <Panel className="p-5 sm:p-7 lg:p-8">
      {/* Headline figures (sales section) */}
      {sales.isPending ? (
        <div className="grid gap-6 lg:grid-cols-[1fr_auto]">
          <div className="space-y-3">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-14 w-72" />
            <Skeleton className="h-4 w-96 max-w-full" />
          </div>
          <Skeleton className="h-24 w-56" />
        </div>
      ) : !s ? (
        <SectionError what="Sales figures" onRetry={() => sales.refetch()} className="border-dashed" />
      ) : (
        <Stale stale={sales.isPlaceholderData}>
          <Headline s={s} isToday={isToday} monthName={monthName} />
        </Stale>
      )}

      {/* The month, day by day (trend section) */}
      <div className="mt-8 border-t pt-5">
        <div className="mb-1 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-muted-foreground">
            Every day of {monthName}
            {monthTarget !== null && (
              <span className="ml-3 inline-flex items-center gap-1.5 whitespace-nowrap">
                <span aria-hidden className="inline-block w-4 border-t-2 border-dashed border-foreground/55" />
                target pace
              </span>
            )}
          </p>
          <SegmentedToggle
            label="Chart shows"
            value={mode}
            onChange={setMode}
            options={[
              ['daily', 'Each day'],
              ['running', 'Running total'],
            ]}
          />
        </div>

        {trend.isPending ? (
          <Skeleton className="mt-[52px] h-[168px] w-full rounded-lg sm:h-[200px]" />
        ) : !trend.data ? (
          <SectionError what="The daily figures" onRetry={() => trend.refetch()} className="mt-3 h-[200px] border-dashed" />
        ) : (
          <Stale stale={trend.isPlaceholderData}>
            <MonthRibbon
              points={trend.data.points}
              monthStart={trend.data.monthStart}
              daysInMonth={trend.data.daysInMonth}
              selectedDate={trend.data.date}
              monthTarget={s && s.monthStart === trend.data.monthStart ? monthTarget : null}
              mode={mode}
            />
          </Stale>
        )}
      </div>

      {/* How far is left (sales section, only with a target) */}
      {s && s.monthTarget.targetValue !== null && (
        <dl className="mt-6 grid grid-cols-1 gap-4 border-t pt-5 sm:grid-cols-3">
          <Fact label="Expected by now" value={money(s.monthTarget.expectedToDate)} />
          <Fact
            label={s.monthTarget.balance !== null && s.monthTarget.balance <= 0 ? 'Beyond the target by' : 'Still to sell'}
            value={money(s.monthTarget.balance !== null ? Math.abs(s.monthTarget.balance) : null)}
          />
          <Fact
            label="Needed each remaining day"
            value={s.monthTarget.requiredDailyRate !== null ? money(s.monthTarget.requiredDailyRate) : 'Month complete'}
          />
        </dl>
      )}
    </Panel>
  )
}

function Headline({ s, isToday, monthName }: { s: DashboardSales; isToday: boolean; monthName: string }) {
  const t = s.monthTarget
  const behind = t.expectedToDate !== null ? t.expectedToDate - s.monthToDate.revenue : null

  return (
    <div className="grid gap-8 lg:grid-cols-[1fr_auto] lg:items-end">
      <div>
        <p className="text-sm text-muted-foreground">
          {monthName} so far, day {s.daysElapsed} of {s.daysInMonth}
        </p>
        <p className="mt-2 font-report text-5xl font-light leading-none tracking-tight tabular-nums sm:text-6xl">
          {money(s.monthToDate.revenue)}
        </p>
        <p className="mt-3 max-w-[64ch] text-sm leading-relaxed">
          {t.targetValue === null ? (
            <span className="text-muted-foreground">
              There&apos;s no target for {monthName} yet, so progress can&apos;t be measured.{' '}
              <Link href="/sales-targets" className="whitespace-nowrap font-medium text-foreground underline underline-offset-4 hover:text-primary">
                Import {monthName} targets
              </Link>
            </span>
          ) : (
            <>
              <span className="font-report font-medium tabular-nums">{percent(t.achievementPercent)}</span>
              <span className="text-muted-foreground"> of the {money(t.targetValue)} monthly target. </span>
              {behind !== null &&
                (behind > 0 ? (
                  <span>
                    <span className="font-report tabular-nums">{money(behind)}</span>
                    <span className="text-muted-foreground"> behind where the month should be by now.</span>
                  </span>
                ) : (
                  <span className="text-primary">
                    <span className="font-report tabular-nums">{money(-behind)}</span> ahead of pace.
                  </span>
                ))}
            </>
          )}
        </p>
      </div>

      <div className="lg:min-w-[240px] lg:border-l lg:pl-8">
        <p className="text-sm text-muted-foreground">{isToday ? 'Today' : `On ${formatColombo(day(s.date), 'd MMMM')}`}</p>
        <p className="mt-2 font-report text-3xl font-light leading-none tracking-tight tabular-nums">
          {money(s.today.revenue)}
        </p>
        <p className="mt-3 text-sm text-muted-foreground">
          {s.today.achievementPercent === null ? (
            'No day target'
          ) : (
            <>
              <span
                className={cn(
                  'font-report font-medium tabular-nums',
                  s.today.achievementPercent >= 100 ? 'text-primary' : 'text-foreground',
                )}
              >
                {percent(s.today.achievementPercent, 0)}
              </span>{' '}
              of the day&apos;s {money(s.today.targetValue)}
            </>
          )}
        </p>
      </div>
    </div>
  )
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-sm text-muted-foreground">{label}</dt>
      <dd className="mt-1 font-report text-lg tabular-nums">{value}</dd>
    </div>
  )
}

// ── Where the money went: sold → discounts → returns → revenue ──────────────

function Ledger() {
  const { data, isPending, isPlaceholderData, refetch } = useDashboardSales()
  const { isToday } = useShownDate()
  const [span, setSpan] = useState<'day' | 'month'>('day')

  const dayLabel = data ? dayName(data.date, isToday) : 'Today'
  const monthLabel = data ? formatColombo(day(data.monthStart), 'MMMM') : 'Month'

  return (
    <Section
      title="Sales, discounts and returns"
      className="flex flex-col"
      aside={
        <SegmentedToggle
          label="Show figures for"
          value={span}
          onChange={setSpan}
          options={[
            ['day', dayLabel],
            ['month', `${monthLabel} so far`],
          ]}
        />
      }
    >
      {isPending ? (
        <Skeleton className="h-[392px] flex-1 rounded-2xl" />
      ) : !data ? (
        <SectionError what="Sales, discounts and returns" onRetry={() => refetch()} className="h-[392px] flex-1" />
      ) : (
        <Stale stale={isPlaceholderData} className="flex flex-1 flex-col">
          <MoneyFlow b={span === 'day' ? data.today : data.monthToDate} />
        </Stale>
      )}
    </Section>
  )
}

function MoneyFlow({ b }: { b: DashboardSalesBlock }) {
  // Revenue is what's left of everything sold once discounts and returns come off — the Sales
  // Summary's own identity. Built from the parts so the bar always sums to exactly 100%.
  const revenuePart = Math.max(b.revenue, 0)
  const sold = revenuePart + b.totalDiscount + b.totalReturn
  const share = (v: number) => (sold > 0 ? (v / sold) * 100 : 0)

  const parts = [
    {
      key: 'revenue',
      label: 'Revenue',
      value: b.revenue,
      swatch: 'bg-primary',
      detail: null as string | null,
    },
    {
      key: 'discounts',
      label: 'Discounts',
      value: b.totalDiscount,
      swatch: 'bg-primary/40',
      detail: `${money(b.discount)} to outlets, ${money(b.dbDiscount)} in distributor free issues`,
    },
    {
      key: 'returns',
      label: 'Returns',
      value: b.totalReturn,
      swatch: 'bg-foreground/15',
      detail: `${money(b.goodReturn)} back to stock, ${money(b.marketReturn)} damaged or expired`,
    },
  ]

  return (
    <Panel className="flex flex-1 flex-col p-5 sm:p-6">
      <p className="text-sm text-muted-foreground">Sold before discounts and returns</p>
      <Amount value={sold} className="mt-1 text-3xl font-light tracking-tight" />

      {/* One bar, three parts. Revenue first: it's the part that counts. */}
      <div className="mt-5 flex h-3 gap-0.5 overflow-hidden rounded-full bg-muted" role="img"
        aria-label={parts.map((p) => `${p.label} ${percent(share(Math.max(p.value, 0)), 0)}`).join(', ')}
      >
        {sold > 0 &&
          parts.map((p) =>
            share(Math.max(p.value, 0)) > 0 ? (
              <div key={p.key} className={cn('h-full first:rounded-l-full last:rounded-r-full', p.swatch)} style={{ width: `${share(Math.max(p.value, 0))}%` }} />
            ) : null,
          )}
      </div>

      <ul className="mt-5 divide-y">
        {parts.map((p) => (
          <li key={p.key} className="flex items-start gap-3 py-3 first:pt-0">
            <span aria-hidden className={cn('mt-1.5 h-2.5 w-2.5 shrink-0 rounded-[3px]', p.swatch)} />
            <div className="min-w-0 flex-1">
              <div className="flex items-baseline justify-between gap-3">
                <span className={cn('text-sm', p.key === 'revenue' ? 'font-medium' : 'text-foreground')}>{p.label}</span>
                <span className="flex items-baseline gap-2">
                  <span className="font-report text-xs tabular-nums text-muted-foreground">
                    {sold > 0 ? percent(share(Math.max(p.value, 0)), 0) : ''}
                  </span>
                  <Amount value={p.value} className={cn('text-lg', p.key === 'revenue' && 'font-medium')} />
                </span>
              </div>
              {p.detail && <p className="mt-0.5 text-xs leading-relaxed text-muted-foreground">{p.detail}</p>}
            </div>
          </li>
        ))}
      </ul>

      <dl className="mt-auto grid grid-cols-2 gap-4 border-t pt-4">
        <div>
          <dt className="text-sm text-muted-foreground">Bills</dt>
          <dd className="mt-0.5 font-report text-lg tabular-nums">{count(b.billCount)}</dd>
        </div>
        <div>
          <dt className="text-sm text-muted-foreground">Average bill</dt>
          <dd className="mt-0.5">
            {b.billCount > 0 ? <Amount value={b.revenue / b.billCount} className="text-lg" /> : <span className="text-lg">—</span>}
          </dd>
        </div>
      </dl>
      <p className="mt-3 text-xs text-muted-foreground">Approved bills only.</p>
    </Panel>
  )
}


function SegmentedToggle<T extends string>({
  label,
  value,
  onChange,
  options,
}: {
  label: string
  value: T
  onChange: (v: T) => void
  options: readonly (readonly [T, string])[]
}) {
  return (
    <div role="group" aria-label={label} className="flex rounded-lg bg-muted p-0.5">
      {options.map(([v, text]) => (
        <button
          key={v}
          type="button"
          aria-pressed={value === v}
          onClick={() => onChange(v)}
          className={cn(
            'whitespace-nowrap rounded-md px-3 py-1 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            value === v ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
          )}
        >
          {text}
        </button>
      ))}
    </div>
  )
}

// ── Field force and outlet reach (activity section) ─────────────────────────

function FieldAndOutlets() {
  const { data, isPending, isPlaceholderData, refetch } = useDashboardActivity()
  const { isToday } = useShownDate()

  return (
    <Section title="Field force and outlets">
      {isPending ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-1">
          <Skeleton className="h-[150px] rounded-2xl" />
          <Skeleton className="h-[226px] rounded-2xl" />
        </div>
      ) : !data ? (
        <SectionError what="Rep and outlet counts" onRetry={() => refetch()} className="h-[392px]" />
      ) : (
        <Stale stale={isPlaceholderData}>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-1">
            <RepsPanel a={data} dayLabel={dayName(data.date, isToday)} />
            <ReachPanel a={data} dayLabel={dayName(data.date, isToday)} />
          </div>
        </Stale>
      )}
    </Section>
  )
}

/** Past this many reps a dot per rep stops being readable, so it becomes a bar. */
const MAX_DOTS = 120

function RepsPanel({ a, dayLabel }: { a: DashboardActivity; dayLabel: string }) {
  const r = a.reps
  return (
    <Panel className="p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="text-sm font-medium">Reps working</p>
        <p className="text-sm text-muted-foreground">{dayLabel}</p>
      </div>
      <p className="mt-3 font-report tabular-nums">
        <span className="text-4xl font-light tracking-tight">{count(r.activeToday)}</span>
        <span className="ml-2 text-lg text-muted-foreground">of {count(r.totalReps)}</span>
      </p>
      <p className="mt-1 text-sm text-muted-foreground">made a bill or logged a no-sale visit</p>

      {r.totalReps > 0 && r.totalReps <= MAX_DOTS ? (
        <div className="mt-4 flex flex-wrap gap-1.5" role="img" aria-label={`${r.activeToday} of ${r.totalReps} reps working`}>
          {Array.from({ length: r.totalReps }, (_, i) => (
            <span
              key={i}
              className={cn(
                'h-2.5 w-2.5 rounded-full',
                i < r.activeToday ? SOLID : 'border border-primary/35',
              )}
            />
          ))}
        </div>
      ) : (
        <Meter percent={r.activePercent} className="mt-4" />
      )}
    </Panel>
  )
}

/**
 * Outlet reach as nested bars: every outlet on record, the active ones among them, and the active
 * ones billed in the last 45 days. Each bar is a subset of the one above, drawn to the same scale.
 */
function ReachPanel({ a, dayLabel }: { a: DashboardActivity; dayLabel: string }) {
  const o = a.outlets
  const total = Math.max(o.totalCustomers, 1)
  const since = formatColombo(day(o.billedWindowFrom), 'd MMM')

  const rows = [
    { label: 'On record', value: o.totalCustomers, share: null, bar: TINT_LIGHT },
    { label: 'Active', value: o.activeOutlets, share: `${percent(o.activePercent)} of all`, bar: TINT },
    {
      label: `Billed since ${since}`,
      value: o.billedLast45Days,
      share: `${percent(o.billedLast45DaysPercent)} of active`,
      bar: SOLID,
    },
  ]

  return (
    <Panel className="p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="text-sm font-medium">Outlet reach</p>
        <Link href="/outlets" className="text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline">
          Manage outlets
        </Link>
      </div>

      <div className="mt-4 space-y-3.5">
        {rows.map((row) => (
          <div key={row.label}>
            <div className="flex items-baseline justify-between gap-3 text-sm">
              <span className="text-muted-foreground">{row.label}</span>
              <span className="font-report tabular-nums">
                <span className="text-base">{count(row.value)}</span>
                {row.share && <span className="ml-2 text-xs text-muted-foreground">{row.share}</span>}
              </span>
            </div>
            <div className="mt-1.5 h-2 overflow-hidden rounded-full bg-muted" aria-hidden>
              <div
                className={cn('h-full rounded-full', row.bar)}
                style={{ width: `${(row.value / total) * 100}%`, minWidth: row.value > 0 ? 3 : 0 }}
              />
            </div>
          </div>
        ))}
      </div>

      <dl className="mt-5 grid grid-cols-2 gap-4 border-t pt-4 text-sm">
        <div>
          <dt className="text-muted-foreground">Deactivated</dt>
          <dd className="mt-0.5 font-report tabular-nums">
            {count(o.inactiveOutlets)} <span className="text-xs text-muted-foreground">{percent(o.inactivePercent)}</span>
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground">{dayLabel === 'Today' ? 'New outlets today' : `New outlets on ${dayLabel}`}</dt>
          <dd className="mt-0.5 font-report tabular-nums">{count(o.newToday)}</dd>
        </div>
      </dl>
    </Panel>
  )
}

// ── Regions (sales section) ─────────────────────────────────────────────────

function Regions() {
  const { data, isPending, isError, isPlaceholderData } = useDashboardSales()

  // The runway above already offers the retry for a failed sales request.
  if (isError && !data) return null

  return (
    <Section
      title="Regions this month"
      aside={
        <Link href="/sales-summary" className="underline-offset-4 hover:text-foreground hover:underline">
          Full breakdown in Sales Summary
        </Link>
      }
    >
      {isPending ? (
        <Skeleton className="h-[160px] rounded-2xl" />
      ) : data.regions.length === 0 ? (
        <Panel className="p-6 text-center text-sm text-muted-foreground">
          No sales or targets recorded this month yet.
        </Panel>
      ) : (
        <Stale stale={isPlaceholderData}>
          <Panel className="overflow-x-auto">
            <table className="w-full min-w-[560px] text-sm">
              <thead>
                <tr className="border-b text-left text-muted-foreground">
                  <th className="px-5 py-3 font-normal">Region</th>
                  <th className="px-3 py-3 text-right font-normal">Month target</th>
                  <th className="px-3 py-3 text-right font-normal">Sold so far</th>
                  <th className="w-[34%] px-5 py-3 font-normal">Progress</th>
                </tr>
              </thead>
              <tbody>
                {data.regions.map((r) => (
                  <tr key={r.regionId ?? 'none'} className="border-b last:border-0">
                    <td className="px-5 py-3.5 font-medium">{r.regionName}</td>
                    <td className="px-3 py-3.5 text-right font-report tabular-nums text-muted-foreground">
                      {r.monthTarget === null ? 'No target' : money(r.monthTarget)}
                    </td>
                    <td className="px-3 py-3.5 text-right font-report tabular-nums">{money(r.revenue)}</td>
                    <td className="px-5 py-3.5">
                      {r.achievementPercent === null ? (
                        <span className="text-muted-foreground">Not measurable</span>
                      ) : (
                        <div className="flex items-center gap-3">
                          <Meter percent={r.achievementPercent} className="flex-1" />
                          <span className="w-12 shrink-0 text-right font-report text-xs tabular-nums">
                            {percent(r.achievementPercent, 0)}
                          </span>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Panel>
        </Stale>
      )}
    </Section>
  )
}
