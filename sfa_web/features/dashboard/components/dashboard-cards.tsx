'use client'

import { cn } from '@/lib/utils'

export function SectionHeading({ children, hint }: { children: React.ReactNode; hint?: string }) {
  return (
    <div className="mb-3 flex items-center gap-3">
      <p className="whitespace-nowrap text-[10px] font-bold uppercase tracking-[0.25em] text-muted-foreground">
        {children}
      </p>
      <div className="h-px flex-1 bg-border" />
      {hint && <p className="whitespace-nowrap text-[11px] text-muted-foreground">{hint}</p>}
    </div>
  )
}

/**
 * A progress bar against a target. Green only once the target is met — over a part-finished
 * month almost everything is short, and a wall of red would alarm without informing.
 * `marker` draws where the value "should" be by now (e.g. the pro-rated target for today).
 */
export function TargetBar({
  percent,
  marker,
  className,
}: {
  percent: number | null
  marker?: number | null
  className?: string
}) {
  if (percent === null) return <div className={cn('h-2.5 rounded-full bg-muted', className)} aria-label="No target" />

  const met = percent >= 100
  const onPace = marker !== null && marker !== undefined && percent >= marker
  return (
    <div
      className={cn('relative h-2.5 overflow-hidden rounded-full bg-muted', className)}
      role="meter"
      aria-valuenow={Math.round(percent)}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div
        className={cn(
          'h-full rounded-full transition-[width] duration-500 motion-reduce:transition-none',
          met ? 'bg-emerald-600' : onPace ? 'bg-emerald-500/80' : 'bg-amber-500',
        )}
        style={{ width: `${Math.max(0, Math.min(percent, 100))}%` }}
      />
      {marker !== null && marker !== undefined && marker > 0 && marker < 100 && (
        <span
          aria-hidden
          className="absolute inset-y-0 w-0.5 bg-foreground/60"
          style={{ left: `${marker}%` }}
        />
      )}
    </div>
  )
}

interface StatTileProps {
  icon: React.ElementType
  label: string
  value: React.ReactNode
  sub?: React.ReactNode
  accent: string
  children?: React.ReactNode
}

/** KPI tile — left-border accent, same visual language as the distributor dashboard. */
export function StatTile({ icon: Icon, label, value, sub, accent, children }: StatTileProps) {
  return (
    <div
      className={cn(
        'relative flex min-h-[118px] flex-col justify-between gap-3 overflow-hidden rounded-xl border border-l-[3px] bg-card px-5 py-4 shadow-sm',
        accent,
      )}
    >
      <Icon className="pointer-events-none absolute bottom-2 right-3 h-14 w-14 select-none opacity-[0.05]" />
      <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">{label}</p>
      <div>
        <p className="text-[1.6rem] font-bold leading-none tracking-tight tabular-nums">{value}</p>
        {sub && <p className="mt-1.5 text-[11px] leading-tight text-muted-foreground">{sub}</p>}
      </div>
      {children}
    </div>
  )
}

/** A label/value pair used inside the larger cards. */
export function Figure({
  label,
  value,
  tone,
}: {
  label: string
  value: React.ReactNode
  tone?: 'good' | 'warn'
}) {
  return (
    <div className="min-w-0">
      <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-muted-foreground">{label}</p>
      <p
        className={cn(
          'mt-1 truncate text-sm font-semibold tabular-nums',
          tone === 'good' && 'text-emerald-600 dark:text-emerald-400',
          tone === 'warn' && 'text-amber-600 dark:text-amber-400',
        )}
      >
        {value}
      </p>
    </div>
  )
}
