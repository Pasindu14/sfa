'use client'

import {
  Bar,
  CartesianGrid,
  Cell,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { formatColombo } from '@/lib/utils/datetime'
import type { DashboardChartPoint } from '../schema/dashboard.schema'
import { money } from './format'

export type TrendMode = 'daily' | 'cumulative'

const MET = '#059669'      // emerald-600 — the day beat its target
const SHORT = '#60a5fa'    // blue-400 — sold, but under the day's target
const TARGET = '#f59e0b'   // amber-500

/**
 * Daily revenue against the month's target spread evenly per day (daily mode), or the running
 * month-to-date total against the running target (cumulative mode). Lives in its own module so
 * the page loads recharts on demand.
 */
export function RevenueTrendChart({ data, mode }: { data: DashboardChartPoint[]; mode: TrendMode }) {
  const rows = data.map((p) => ({
    ...p,
    day: formatColombo(`${p.date}T00:00:00+05:30`, 'd'),
    value: mode === 'daily' ? p.revenue : p.cumulativeRevenue,
    goal: mode === 'daily' ? p.target : p.cumulativeTarget,
  }))

  return (
    <ResponsiveContainer width="100%" height="100%">
      <ComposedChart data={rows} barCategoryGap="22%">
        <CartesianGrid vertical={false} strokeDasharray="3 3" stroke="hsl(var(--border))" strokeOpacity={0.6} />
        <XAxis dataKey="day" tick={{ fontSize: 10, fill: 'hsl(var(--muted-foreground))' }} axisLine={false} tickLine={false} />
        <YAxis
          tickFormatter={axisTick}
          tick={{ fontSize: 10, fill: 'hsl(var(--muted-foreground))' }}
          axisLine={false}
          tickLine={false}
          width={48}
        />
        <Tooltip
          cursor={{ fill: 'hsl(var(--muted))', opacity: 0.5 }}
          content={({ active, payload }) => {
            if (!active || !payload?.length) return null
            const p = payload[0].payload as (typeof rows)[number]
            const pct = p.goal ? (p.value / p.goal) * 100 : null
            return (
              <div className="rounded-lg border bg-popover px-3 py-2 text-xs shadow-lg">
                <p className="mb-1.5 font-semibold text-foreground">
                  {formatColombo(`${p.date}T00:00:00+05:30`, 'EEE, d MMM')}
                </p>
                <Row color={p.goal && p.value >= p.goal ? MET : SHORT} label={mode === 'daily' ? 'Revenue' : 'Revenue to date'} value={money(p.value)} />
                <Row color={TARGET} label={mode === 'daily' ? 'Daily target' : 'Target to date'} value={money(p.goal)} />
                {pct !== null && (
                  <p className="mt-1.5 text-right font-medium text-foreground">{pct.toFixed(1)}% of target</p>
                )}
              </div>
            )
          }}
        />
        <Bar dataKey="value" name="Revenue" radius={[3, 3, 0, 0]}>
          {rows.map((r) => (
            <Cell key={r.date} fill={r.goal && r.value >= r.goal ? MET : SHORT} />
          ))}
        </Bar>
        <Line
          dataKey="goal"
          name="Target"
          type="monotone"
          stroke={TARGET}
          strokeWidth={2}
          strokeDasharray="5 4"
          dot={false}
          connectNulls
        />
      </ComposedChart>
    </ResponsiveContainer>
  )
}

/** Bare compact figure for the axis ("1.2M", "850K", "0") — the currency is implied by the chart. */
function axisTick(v: number): string {
  const abs = Math.abs(v)
  if (abs >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`
  if (abs >= 1_000) return `${(v / 1_000).toFixed(0)}K`
  return String(Math.round(v))
}

function Row({ color, label, value }: { color: string; label: string; value: string }) {
  return (
    <div className="mt-1 flex items-center justify-between gap-6">
      <span className="flex items-center gap-1.5 text-muted-foreground">
        <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: color }} />
        {label}
      </span>
      <span className="font-mono font-medium text-foreground">{value}</span>
    </div>
  )
}
