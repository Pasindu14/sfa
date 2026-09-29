'use client'

import { useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import { formatColombo } from '@/lib/utils/datetime'
import type { DashboardTrend } from '../schema/dashboard.schema'
import { money } from './format'

export type RibbonMode = 'daily' | 'running'

interface MonthRibbonProps {
  /** Days from the 1st up to the dashboard date. */
  points: DashboardTrend['points']
  monthStart: string
  daysInMonth: number
  /** The dashboard date — highlighted, and the readout's resting day. */
  selectedDate: string
  /** Full-month target; null when none was imported. */
  monthTarget: number | null
  mode: RibbonMode
}

const day = (d: string) => `${d}T00:00:00+05:30`

/** YYYY-MM-DD for day `n` (1-based) of the month starting at `monthStart`. */
function dateOf(monthStart: string, n: number) {
  return `${monthStart.slice(0, 8)}${String(n).padStart(2, '0')}`
}

/**
 * Brand colour throughout. With a target: solid once the day met it, a light tint while short.
 * Without one there is nothing to meet, so every sold day shares one tone. The dashboard date is
 * always the strongest bar of its kind.
 */
function barTone(hasTarget: boolean, met: boolean, selected: boolean) {
  if (!hasTarget) return selected ? 'bg-primary' : 'bg-primary/75'
  if (met) return 'bg-primary'
  return selected ? 'bg-primary/60' : 'bg-primary/30'
}

/**
 * The whole month as a ribbon of day slots: sold days are bars, days still to come are empty
 * outlines, and the target pace is a single orange line. Pointing at (or tabbing to) a day shows
 * its figures in the readout above, so nothing floats over the bars.
 */
export function MonthRibbon({ points, monthStart, daysInMonth, selectedDate, monthTarget, mode }: MonthRibbonProps) {
  const [focused, setFocused] = useState<string | null>(null)

  const perDay = monthTarget !== null ? monthTarget / daysInMonth : null

  const slots = useMemo(() => {
    const byDate = new Map(points.map((p) => [p.date, p]))
    return Array.from({ length: daysInMonth }, (_, i) => {
      const date = dateOf(monthStart, i + 1)
      const p = byDate.get(date)
      const target = perDay === null ? null : mode === 'daily' ? perDay : perDay * (i + 1)
      return {
        n: i + 1,
        date,
        future: !p,
        value: p ? (mode === 'daily' ? p.revenue : p.cumulativeRevenue) : null,
        target,
      }
    })
  }, [points, monthStart, daysInMonth, perDay, mode])

  // Scale to whichever is taller: the best day or the target the bars are measured against.
  const peak = Math.max(
    1,
    ...slots.map((s) => s.value ?? 0),
    perDay === null ? 0 : mode === 'daily' ? perDay : perDay * daysInMonth,
  ) * 1.08

  const shown = slots.find((s) => s.date === (focused ?? selectedDate)) ?? slots[slots.length - 1]
  const pct = (v: number) => `${Math.max(0, (v / peak) * 100)}%`

  return (
    <div>
      {/* Readout — the focused day, or the dashboard date at rest. */}
      <div className="mb-3 flex min-h-[40px] flex-wrap items-baseline gap-x-5 gap-y-1" aria-live="polite">
        <span className="text-sm font-medium">{formatColombo(day(shown.date), 'EEEE d MMMM')}</span>
        {shown.future ? (
          <span className="text-sm text-muted-foreground">Still to come</span>
        ) : (
          <>
            <span className="font-report text-sm tabular-nums">
              <span className="text-muted-foreground">{mode === 'daily' ? 'Sold ' : 'Sold so far '}</span>
              {money(shown.value)}
            </span>
            {shown.target !== null && (
              <span className="font-report text-sm tabular-nums">
                <span className="text-muted-foreground">{mode === 'daily' ? 'Day target ' : 'Target by then '}</span>
                {money(shown.target)}
              </span>
            )}
          </>
        )}
      </div>

      <div className="relative h-[168px] sm:h-[200px]" onMouseLeave={() => setFocused(null)}>
        {/* Target pace */}
        {perDay !== null &&
          (mode === 'daily' ? (
            <div
              aria-hidden
              className="pointer-events-none absolute inset-x-0 z-10 border-t-2 border-dashed border-foreground/55"
              style={{ bottom: pct(perDay) }}
            />
          ) : (
            <svg
              aria-hidden
              className="pointer-events-none absolute inset-0 z-10 h-full w-full overflow-visible"
              viewBox="0 0 100 100"
              preserveAspectRatio="none"
            >
              <line
                x1="0"
                y1="100"
                x2="100"
                y2={100 - ((perDay * daysInMonth) / peak) * 100}
                className="stroke-foreground/55"
                strokeWidth="2"
                strokeDasharray="6 5"
                vectorEffect="non-scaling-stroke"
              />
            </svg>
          ))}

        <div
          className="grid h-full items-end gap-[3px] sm:gap-1"
          style={{ gridTemplateColumns: `repeat(${daysInMonth}, minmax(0, 1fr))` }}
        >
          {slots.map((s, i) => {
            const met = s.value !== null && s.target !== null && s.value >= s.target
            const isSelected = s.date === selectedDate
            return (
              <button
                key={s.date}
                type="button"
                onMouseEnter={() => setFocused(s.date)}
                onFocus={() => setFocused(s.date)}
                onBlur={() => setFocused(null)}
                aria-label={
                  s.future
                    ? `${formatColombo(day(s.date), 'd MMMM')}: still to come`
                    : `${formatColombo(day(s.date), 'd MMMM')}: ${money(s.value)}`
                }
                className="group relative flex h-full items-end rounded-[3px] outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                {s.future ? (
                  <span className="h-full w-full rounded-[3px] border border-dashed border-foreground/20" />
                ) : (
                  <span
                    className={cn(
                      'w-full origin-bottom rounded-t-[3px] transition-colors motion-safe:animate-[dash-grow_700ms_cubic-bezier(0.2,0.7,0.2,1)_both]',
                      barTone(s.target !== null, met, isSelected),
                      'group-hover:brightness-90',
                    )}
                    style={{
                      height: s.value && s.value > 0 ? `max(3px, ${pct(s.value)})` : '2px',
                      animationDelay: `${i * 14}ms`,
                    }}
                  />
                )}
              </button>
            )
          })}
        </div>
      </div>

      {/* Day numbers: the week-ish anchors, the last day, and the dashboard date. */}
      <div
        className="mt-2 grid gap-[3px] sm:gap-1"
        style={{ gridTemplateColumns: `repeat(${daysInMonth}, minmax(0, 1fr))` }}
        aria-hidden
      >
        {slots.map((s) => {
          const isSelected = s.date === selectedDate
          const selectedN = Number(selectedDate.slice(8))
          // Anchors: the 1st, every 5th and the last day — minus any crowding the selected day,
          // whose label always wins.
          const anchor =
            (s.n === 1 || s.n % 5 === 0 || s.n === daysInMonth) && Math.abs(s.n - selectedN) > 2
          return (
            <span
              key={s.date}
              className={cn(
                'text-center font-report text-[11px] tabular-nums',
                isSelected ? 'font-semibold text-foreground' : 'text-muted-foreground',
                !anchor && !isSelected && 'invisible',
              )}
            >
              {s.n}
            </span>
          )
        })}
      </div>
    </div>
  )
}
