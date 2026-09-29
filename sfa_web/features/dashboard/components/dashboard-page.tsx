'use client'

import { useMemo, useState } from 'react'
import dynamic from 'next/dynamic'
import Link from 'next/link'
import {
  AlertCircle,
  ArrowUpRight,
  BadgePercent,
  CalendarDays,
  ReceiptText,
  RefreshCw,
  Sparkles,
  Store,
  StoreIcon,
  TrendingUp,
  Undo2,
  UserCheck,
  Users,
} from 'lucide-react'
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
import type {
  DashboardActivity,
  DashboardChartPoint,
  DashboardSales,
  DashboardSalesBlock,
} from '../schema/dashboard.schema'
import { Figure, SectionHeading, StatTile, TargetBar } from './dashboard-cards'
import { count, money, moneyShort, percent } from './format'
import type { TrendMode } from './revenue-trend-chart'

// recharts loads on demand; the skeleton holds the chart slot meanwhile.
const RevenueTrendChart = dynamic(
  () => import('./revenue-trend-chart').then((m) => ({ default: m.RevenueTrendChart })),
  { ssr: false, loading: () => <Skeleton className="h-full w-full rounded-lg" /> },
)

/** Colombo date string → a value formatColombo renders as that same calendar day. */
const day = (d: string) => `${d}T00:00:00+05:30`

/**
 * The admin dashboard. The three API sections (sales, activity, trend) are separate queries that
 * load in parallel. Each block renders the moment its own data arrives, with its own skeleton and
 * error, so a slow section never holds up the others and nothing blocks the page.
 */
export function DashboardPage() {
  return (
    <div className="flex flex-1 flex-col gap-8 p-4 pt-0 md:p-6 md:pt-0">
      <Header />
      <TargetSection />
      <SalesSection />
      <FieldSection />
      <TrendSection />
      <RegionSection />
    </div>
  )
}

// ── Shared section states ───────────────────────────────────────────────────

function SectionError({ onRetry, className }: { onRetry: () => void; className?: string }) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed bg-card p-6 text-center',
        className,
      )}
    >
      <AlertCircle className="h-5 w-5 text-muted-foreground" />
      <p className="text-sm text-muted-foreground">This section couldn&apos;t be loaded.</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        Try again
      </Button>
    </div>
  )
}

/** Dims a section while it shows the previous day's numbers for a newly picked day. */
function Stale({ stale, children }: { stale: boolean; children: React.ReactNode }) {
  return (
    <div className={cn('transition-opacity', stale && 'pointer-events-none opacity-60')} aria-busy={stale}>
      {children}
    </div>
  )
}

// ── Header ──────────────────────────────────────────────────────────────────

function Header() {
  const date = useDashboardStore((s) => s.date)
  const setDate = useDashboardStore((s) => s.setDate)
  const isFetching = useDashboardIsFetching()
  const refresh = useRefreshDashboard()
  const { data: sales } = useDashboardSales()

  const today = toColomboDateStr(new Date())
  const shown = date ?? today
  const isToday = shown === today

  return (
    <div className="flex flex-col gap-4 border-b pb-5 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Sales Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {formatColombo(day(shown), 'EEEE, d MMMM yyyy')}
          <span className="ml-2 text-xs">
            {sales && <>· updated {formatColombo(sales.generatedAtUtc, 'HH:mm')}</>}
            {isToday && ' · refreshes every 2 min'}
          </span>
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative">
          <CalendarDays className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="date"
            aria-label="Dashboard date"
            className="h-9 w-[170px] pl-8"
            value={shown}
            max={today}
            onChange={(e) => {
              const v = e.target.value
              // Empty (cleared) or today → follow "today" so the page keeps auto-refreshing.
              setDate(!v || v >= today ? null : v)
            }}
          />
        </div>
        {!isToday && (
          <Button variant="outline" size="sm" className="h-9" onClick={() => setDate(null)}>
            Today
          </Button>
        )}
        <Button variant="outline" size="sm" className="h-9 gap-2" onClick={refresh} disabled={isFetching}>
          <RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>
    </div>
  )
}

// ── 1. Target & achievement (sales section) ─────────────────────────────────

function TargetSection() {
  const { data, isPending, isError, isPlaceholderData, refetch } = useDashboardSales()

  return (
    <section>
      <SectionHeading hint={data ? `Day ${data.daysElapsed} of ${data.daysInMonth}` : undefined}>
        Target &amp; achievement
      </SectionHeading>
      {isPending ? (
        <div className="grid gap-4 lg:grid-cols-3">
          <Skeleton className="h-[250px] rounded-xl lg:col-span-2" />
          <Skeleton className="h-[250px] rounded-xl" />
        </div>
      ) : isError && !data ? (
        <SectionError onRetry={() => refetch()} className="h-[250px]" />
      ) : (
        <Stale stale={isPlaceholderData}>
          <TargetCards d={data} />
        </Stale>
      )}
    </section>
  )
}

function TargetCards({ d }: { d: DashboardSales }) {
  const t = d.monthTarget
  const monthLabel = formatColombo(day(d.monthStart), 'MMMM yyyy')
  // Where the month "should" be by now, as a % of the full target — drawn as a tick on the bar.
  const pace = (d.daysElapsed / d.daysInMonth) * 100
  const ahead = t.achievementPercent !== null && t.achievementPercent >= pace

  return (
    <div className="grid gap-4 lg:grid-cols-3">
      {/* Monthly */}
      <div className="rounded-xl border bg-card p-5 shadow-sm lg:col-span-2">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm font-semibold">Monthly target — {monthLabel}</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Month-to-date revenue against the full month&apos;s target
            </p>
          </div>
          {t.targetValue !== null && (
            <span
              className={cn(
                'rounded-full px-2.5 py-1 text-[11px] font-semibold',
                ahead
                  ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
                  : 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
              )}
            >
              {ahead ? 'On pace' : 'Behind pace'}
            </span>
          )}
        </div>

        {t.targetValue === null ? (
          // No target: lead with what we do know rather than a dash.
          <div className="mt-5 flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <span className="text-4xl font-bold tracking-tight tabular-nums">{money(d.monthToDate.revenue)}</span>
            <span className="text-sm text-muted-foreground">revenue month to date</span>
          </div>
        ) : (
          <>
            <div className="mt-5 flex flex-wrap items-baseline gap-x-3 gap-y-1">
              <span className="text-4xl font-bold tracking-tight tabular-nums">{percent(t.achievementPercent)}</span>
              <span className="text-sm text-muted-foreground">
                {money(d.monthToDate.revenue)} of {money(t.targetValue)}
              </span>
            </div>
            <TargetBar percent={t.achievementPercent} marker={pace} className="mt-3" />
            <p className="mt-1.5 text-[11px] text-muted-foreground">
              The tick marks where the month should be today ({pace.toFixed(0)}%).
            </p>
          </>
        )}

        {t.targetValue === null ? (
          <p className="mt-5 rounded-lg bg-muted/60 px-3 py-2 text-xs text-muted-foreground">
            No sales targets have been imported for {monthLabel}.{' '}
            <Link href="/sales-targets" className="font-medium text-foreground underline underline-offset-2">
              Import targets
            </Link>
          </p>
        ) : (
          <div className="mt-5 grid grid-cols-2 gap-4 border-t pt-4 sm:grid-cols-4">
            <Figure label="Monthly target" value={money(t.targetValue)} />
            <Figure label="Expected by today" value={money(t.expectedToDate)} />
            <Figure
              label={t.balance !== null && t.balance < 0 ? 'Above target by' : 'Balance to target'}
              value={money(t.balance !== null ? Math.abs(t.balance) : null)}
              tone={t.balance !== null && t.balance <= 0 ? 'good' : undefined}
            />
            <Figure
              label="Needed per day"
              value={money(t.requiredDailyRate)}
              tone={
                t.requiredDailyRate !== null && d.today.targetValue !== null && t.requiredDailyRate > d.today.targetValue
                  ? 'warn'
                  : undefined
              }
            />
          </div>
        )}
      </div>

      {/* Daily */}
      <div className="flex flex-col rounded-xl border bg-card p-5 shadow-sm">
        <p className="text-sm font-semibold">Daily achievement</p>
        <p className="mt-1 text-xs text-muted-foreground">
          {formatColombo(day(d.date), 'd MMM')} revenue against the day&apos;s share of the target
        </p>
        {d.today.targetValue === null ? (
          <div className="mt-5 flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <span className="text-4xl font-bold tracking-tight tabular-nums">{money(d.today.revenue)}</span>
            <span className="text-sm text-muted-foreground">no target set</span>
          </div>
        ) : (
          <>
            <div className="mt-5 flex items-baseline gap-3">
              <span className="text-4xl font-bold tracking-tight tabular-nums">{percent(d.today.achievementPercent)}</span>
            </div>
            <TargetBar percent={d.today.achievementPercent} className="mt-3" />
          </>
        )}
        <div className="mt-auto grid grid-cols-2 gap-4 border-t pt-4">
          <Figure label="Revenue" value={money(d.today.revenue)} />
          <Figure label="Daily target" value={money(d.today.targetValue)} />
        </div>
      </div>
    </div>
  )
}

// ── 2. Revenue, discounts, returns (sales section) ──────────────────────────

function SalesSection() {
  const { data, isPending, isError, isPlaceholderData, refetch } = useDashboardSales()

  return (
    <section>
      <SectionHeading hint="Approved bills only">Revenue, discounts &amp; returns</SectionHeading>
      {isPending ? (
        <TileSkeletons />
      ) : isError && !data ? (
        <SectionError onRetry={() => refetch()} className="h-[140px]" />
      ) : (
        <Stale stale={isPlaceholderData}>
          <SalesTiles t={data.today} m={data.monthToDate} />
        </Stale>
      )}
    </section>
  )
}

function SalesTiles({ t, m }: { t: DashboardSalesBlock; m: DashboardSalesBlock }) {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <StatTile
        icon={TrendingUp}
        label="Revenue"
        accent="border-l-emerald-500"
        value={moneyShort(t.revenue)}
        sub={<>Today · <b className="text-foreground">{moneyShort(m.revenue)}</b> month to date</>}
      />
      <StatTile
        icon={BadgePercent}
        label="Total discount"
        accent="border-l-violet-500"
        value={moneyShort(t.totalDiscount)}
        sub={<>Today · <b className="text-foreground">{moneyShort(m.totalDiscount)}</b> month to date</>}
      >
        <Split
          items={[
            { label: 'Outlet discount', today: t.discount, mtd: m.discount },
            { label: 'Distributor free issue', today: t.dbDiscount, mtd: m.dbDiscount },
          ]}
        />
      </StatTile>
      <StatTile
        icon={Undo2}
        label="Total returns"
        accent="border-l-rose-500"
        value={moneyShort(t.totalReturn)}
        sub={<>Today · <b className="text-foreground">{moneyShort(m.totalReturn)}</b> month to date</>}
      >
        <Split
          items={[
            { label: 'Good (resaleable)', today: t.goodReturn, mtd: m.goodReturn },
            { label: 'Damage / expiry', today: t.marketReturn, mtd: m.marketReturn },
          ]}
        />
      </StatTile>
      <StatTile
        icon={ReceiptText}
        label="Bills"
        accent="border-l-sky-500"
        value={count(t.billCount)}
        sub={<>Today · <b className="text-foreground">{count(m.billCount)}</b> month to date</>}
      >
        <p className="text-[11px] text-muted-foreground">
          Avg bill today <b className="text-foreground">{avgBill(t)}</b> · month{' '}
          <b className="text-foreground">{avgBill(m)}</b>
        </p>
      </StatTile>
    </div>
  )
}

function avgBill(b: DashboardSalesBlock) {
  return b.billCount > 0 ? moneyShort(b.revenue / b.billCount) : '—'
}

function Split({ items }: { items: { label: string; today: number; mtd: number }[] }) {
  return (
    <div className="space-y-1 border-t pt-2 text-[11px]">
      {items.map((i) => (
        <div key={i.label} className="flex justify-between gap-2 text-muted-foreground">
          <span className="truncate">{i.label}</span>
          <span className="tabular-nums">
            <b className="text-foreground">{moneyShort(i.today)}</b> / {moneyShort(i.mtd)}
          </span>
        </div>
      ))}
    </div>
  )
}

function TileSkeletons() {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {[1, 2, 3, 4].map((i) => (
        <Skeleton key={i} className="h-[140px] rounded-xl" />
      ))}
    </div>
  )
}

// ── 3. Field force & outlets (activity section) ─────────────────────────────

function FieldSection() {
  const { data, isPending, isError, isPlaceholderData, refetch } = useDashboardActivity()

  return (
    <section>
      <SectionHeading>Field force &amp; outlets</SectionHeading>
      {isPending ? (
        <div className="grid gap-3 lg:grid-cols-3">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:col-span-2">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-[118px] rounded-xl" />
            ))}
          </div>
          <Skeleton className="h-[248px] rounded-xl" />
        </div>
      ) : isError && !data ? (
        <SectionError onRetry={() => refetch()} className="h-[248px]" />
      ) : (
        <Stale stale={isPlaceholderData}>
          <FieldTiles a={data} />
        </Stale>
      )}
    </section>
  )
}

function FieldTiles({ a }: { a: DashboardActivity }) {
  const r = a.reps
  const o = a.outlets
  return (
    <div className="grid gap-3 lg:grid-cols-3">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:col-span-2">
        <StatTile
          icon={UserCheck}
          label="Active reps today"
          accent="border-l-blue-500"
          value={
            <>
              {count(r.activeToday)}
              <span className="text-base font-medium text-muted-foreground"> / {count(r.totalReps)}</span>
            </>
          }
          sub={`${percent(r.activePercent)} of reps billed or logged a visit`}
        >
          <TargetBar percent={r.activePercent} />
        </StatTile>
        <StatTile
          icon={Store}
          label="Active shops"
          accent="border-l-emerald-500"
          value={count(o.activeOutlets)}
          sub="Outlets currently active"
        />
        <StatTile
          icon={ReceiptText}
          label="Billed outlets · last 45 days"
          accent="border-l-amber-500"
          value={count(o.billedLast45Days)}
          sub={`${percent(o.billedLast45DaysPercent)} of active shops · since ${formatColombo(day(o.billedWindowFrom), 'd MMM')}`}
        >
          <TargetBar percent={o.billedLast45DaysPercent} />
        </StatTile>
        <StatTile
          icon={Sparkles}
          label="New outlets today"
          accent="border-l-fuchsia-500"
          value={count(o.newToday)}
          sub={`Registered on ${formatColombo(day(a.date), 'd MMM')}`}
        />
      </div>

      <CustomerCard o={o} />
    </div>
  )
}

function CustomerCard({ o }: { o: DashboardActivity['outlets'] }) {
  return (
    <div className="flex flex-col rounded-xl border bg-card p-5 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">Total customers</p>
        <Users className="h-4 w-4 text-muted-foreground" />
      </div>
      <p className="mt-3 text-4xl font-bold tracking-tight tabular-nums">{count(o.totalCustomers)}</p>

      {/* Stacked split bar — the two shares always sum to the whole. */}
      <div className="mt-5 flex h-3 overflow-hidden rounded-full bg-muted" aria-hidden>
        <div className="bg-emerald-500" style={{ width: `${o.activePercent ?? 0}%` }} />
        <div className="bg-zinc-400 dark:bg-zinc-600" style={{ width: `${o.inactivePercent ?? 0}%` }} />
      </div>

      <div className="mt-4 space-y-2.5 text-sm">
        <LegendRow color="bg-emerald-500" label="Active" n={o.activeOutlets} pct={o.activePercent} />
        <LegendRow color="bg-zinc-400 dark:bg-zinc-600" label="Deactivated" n={o.inactiveOutlets} pct={o.inactivePercent} />
      </div>

      <Link
        href="/outlets"
        className="mt-auto flex items-center gap-1 pt-4 text-xs font-medium text-muted-foreground hover:text-foreground"
      >
        <StoreIcon className="h-3.5 w-3.5" /> Manage outlets <ArrowUpRight className="h-3 w-3" />
      </Link>
    </div>
  )
}

function LegendRow({ color, label, n, pct }: { color: string; label: string; n: number; pct: number | null }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="flex items-center gap-2 text-muted-foreground">
        <span className={cn('h-2.5 w-2.5 rounded-sm', color)} />
        {label}
      </span>
      <span className="tabular-nums">
        <b>{count(n)}</b> <span className="text-muted-foreground">· {percent(pct)}</span>
      </span>
    </div>
  )
}

// ── 4. Trend (trend section + target from the sales section) ────────────────

function TrendSection() {
  const [mode, setMode] = useState<TrendMode>('daily')
  const { data: trend, isPending, isError, isPlaceholderData, refetch } = useDashboardTrend()
  // The target line comes from the sales section, which is loading in parallel. The bars render
  // as soon as the trend lands, and the target line joins them when (and if) the sales data arrives.
  const { data: sales } = useDashboardSales()
  const monthTarget = sales && trend && sales.monthStart === trend.monthStart ? sales.monthTarget.targetValue : null

  const points = useMemo<DashboardChartPoint[]>(() => {
    if (!trend) return []
    const perDay = monthTarget !== null ? monthTarget / trend.daysInMonth : null
    return trend.points.map((p, i) => ({
      ...p,
      target: perDay !== null ? Math.round(perDay * 100) / 100 : null,
      cumulativeTarget: perDay !== null ? Math.round(perDay * (i + 1) * 100) / 100 : null,
    }))
  }, [trend, monthTarget])

  const hasTarget = monthTarget !== null

  return (
    <section>
      <SectionHeading>This month, day by day</SectionHeading>
      <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b px-5 py-3">
          <div className="flex items-center gap-4 text-[11px] text-muted-foreground">
            {hasTarget ? (
              <>
                <LegendDot color="#059669" label="Hit target" />
                <LegendDot color="#60a5fa" label="Below target" />
                <span className="flex items-center gap-1.5">
                  <span className="h-0.5 w-4 border-t-2 border-dashed border-amber-500" />
                  {mode === 'daily' ? 'Daily target' : 'Target to date'}
                </span>
              </>
            ) : (
              <LegendDot color="#60a5fa" label="Revenue" />
            )}
          </div>
          <div className="flex rounded-md border p-0.5">
            {(['daily', 'cumulative'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setMode(m)}
                className={cn(
                  'rounded px-3 py-1 text-xs font-medium transition-colors',
                  mode === m ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {m === 'daily' ? 'Daily' : 'Cumulative'}
              </button>
            ))}
          </div>
        </div>
        <div className="h-[280px] px-2 py-4">
          {isPending ? (
            <Skeleton className="h-full w-full rounded-lg" />
          ) : isError && !trend ? (
            <SectionError onRetry={() => refetch()} className="h-full border-0" />
          ) : (
            <Stale stale={isPlaceholderData}>
              <div className="h-[248px]">
                <RevenueTrendChart data={points} mode={mode} />
              </div>
            </Stale>
          )}
        </div>
      </div>
    </section>
  )
}

function LegendDot({ color, label }: { color: string; label: string }) {
  return (
    <span className="flex items-center gap-1.5">
      <span className="h-2.5 w-2.5 rounded-sm" style={{ backgroundColor: color }} />
      {label}
    </span>
  )
}

// ── 5. Regions (sales section) ──────────────────────────────────────────────

function RegionSection() {
  const { data, isPending, isError, isPlaceholderData } = useDashboardSales()

  // The target section above already shows the retry for a failed sales request.
  if (isError && !data) return null

  return (
    <section>
      <SectionHeading hint="Month to date vs full-month target">By region</SectionHeading>
      {isPending ? (
        <Skeleton className="h-[200px] rounded-xl" />
      ) : data.regions.length === 0 ? (
        <p className="rounded-xl border border-dashed p-6 text-center text-sm text-muted-foreground">
          No sales or targets recorded this month yet.
        </p>
      ) : (
        <Stale stale={isPlaceholderData}>
          <RegionTable d={data} />
        </Stale>
      )}
    </section>
  )
}

function RegionTable({ d }: { d: DashboardSales }) {
  const pace = (d.daysElapsed / d.daysInMonth) * 100
  return (
    <>
      <div className="overflow-x-auto rounded-xl border bg-card shadow-sm">
        <table className="w-full min-w-[560px] text-sm">
          <thead>
            <tr className="border-b bg-muted/40 text-left text-[11px] uppercase tracking-wider text-muted-foreground">
              <th className="px-5 py-2.5 font-semibold">Region</th>
              <th className="px-3 py-2.5 text-right font-semibold">Month target</th>
              <th className="px-3 py-2.5 text-right font-semibold">Revenue</th>
              <th className="w-[36%] px-5 py-2.5 font-semibold">Achievement</th>
            </tr>
          </thead>
          <tbody>
            {d.regions.map((r) => (
              <tr key={r.regionId ?? 'none'} className="border-b last:border-0">
                <td className="px-5 py-3 font-medium">{r.regionName}</td>
                <td className="px-3 py-3 text-right tabular-nums text-muted-foreground">{money(r.monthTarget)}</td>
                <td className="px-3 py-3 text-right font-medium tabular-nums">{money(r.revenue)}</td>
                <td className="px-5 py-3">
                  <div className="flex items-center gap-3">
                    <TargetBar
                      percent={r.achievementPercent}
                      marker={r.achievementPercent !== null ? pace : null}
                      className="flex-1"
                    />
                    <span className="w-14 shrink-0 text-right text-xs tabular-nums">{percent(r.achievementPercent)}</span>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="mt-2 text-right text-[11px]">
        <Link href="/sales-summary" className="inline-flex items-center gap-1 text-muted-foreground hover:text-foreground">
          Full breakdown in Sales Summary <ArrowUpRight className="h-3 w-3" />
        </Link>
      </p>
    </>
  )
}
