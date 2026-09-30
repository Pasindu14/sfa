'use client'

import { useEffect, useRef } from 'react'
import {
  Ban,
  Hourglass,
  ListOrdered,
  LockOpen,
  MapPinOff,
  Receipt,
  Sunrise,
  Sunset,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { formatColombo } from '@/lib/utils/datetime'
import type {
  RepTimelineEventDto,
  RepTimelineEventKind,
} from '../../schema/rep-route.schema'
import {
  IDLE_HIGHLIGHT_MINUTES,
  MARKER_COLORS,
  eventTitle,
  formatLkr,
  formatMinutes,
  type TimelineSelection,
} from './timeline-display'

const KIND_ICONS: Record<RepTimelineEventKind, LucideIcon> = {
  DayStart: Sunrise,
  Bill: Receipt,
  NoSale: Ban,
  Stop: Hourglass,
  GpsGap: MapPinOff,
  Unlock: LockOpen,
  DayEnd: Sunset,
}

/** GPS gaps have no marker, but still get a muted tint so they read as "missing data". */
const FALLBACK_ICON_COLOR = '#94a3b8'

function subLine(e: RepTimelineEventDto): string[] {
  const parts: string[] = []

  if ((e.kind === 'Stop' || e.kind === 'GpsGap') && e.endAt) {
    parts.push(`${formatColombo(e.at, 'HH:mm')}–${formatColombo(e.endAt, 'HH:mm')}`)
  }
  if (e.kind === 'Stop' && e.detail) parts.push(e.detail)
  if (e.kind === 'GpsGap') parts.push('position shown is the last known fix')

  if (e.billingNumber) parts.push(e.billingNumber)
  if (e.amount != null) parts.push(formatLkr(e.amount))
  if (e.distanceFromOutletMeters != null) {
    parts.push(`${Math.round(e.distanceFromOutletMeters)} m from outlet`)
  }
  if (e.dwellMinutes != null) parts.push(`dwell ${formatMinutes(e.dwellMinutes)}`)

  return parts
}

/**
 * The rep's day as one scrolling list, in the order it happened. Rows with a position are
 * clickable and drive the map; a marker click drives the list back through `selection`.
 */
export function RepTimelinePanel({
  events,
  selection,
  onSelect,
}: {
  events: RepTimelineEventDto[]
  selection: TimelineSelection | null
  onSelect: (index: number) => void
}) {
  const itemRefs = useRef(new Map<number, HTMLLIElement>())

  // Only a marker click scrolls the list — a row the admin just clicked is already in view.
  useEffect(() => {
    if (selection?.source !== 'map') return
    itemRefs.current.get(selection.index)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
  }, [selection])

  if (events.length === 0) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-2 p-6 text-center">
        <ListOrdered className="h-8 w-8 text-muted-foreground" />
        <p className="text-sm font-medium">No activity recorded</p>
        <p className="text-xs text-muted-foreground">
          No bills, no-sale visits, stops or unlock requests on this day.
        </p>
      </div>
    )
  }

  return (
    <ol className="flex flex-col p-3">
      {events.map((e, index) => {
        const Icon = KIND_ICONS[e.kind] ?? ListOrdered
        const color = MARKER_COLORS[e.kind] ?? FALLBACK_ICON_COLOR
        const selected = selection?.index === index
        const since = e.sinceLastActivityMinutes
        const isActivity = e.kind === 'Bill' || e.kind === 'NoSale'
        const details = subLine(e)

        return (
          <li
            key={index}
            ref={(el) => {
              if (el) itemRefs.current.set(index, el)
              else itemRefs.current.delete(index)
            }}
          >
            {isActivity && since != null && (
              <div
                className={cn(
                  'ml-[18px] flex items-center gap-2 border-l border-dashed py-1.5 pl-4 text-[11px]',
                  since >= IDLE_HIGHLIGHT_MINUTES
                    ? 'border-amber-400 font-medium text-amber-700 dark:text-amber-400'
                    : 'text-muted-foreground',
                )}
              >
                {formatMinutes(since)} since last activity
              </div>
            )}

            <button
              type="button"
              onClick={() => onSelect(index)}
              className={cn(
                'flex w-full items-start gap-3 rounded-md border border-transparent p-2 text-left transition-colors hover:bg-muted/60',
                selected && 'border-border bg-muted',
                e.cancelled && 'opacity-60',
              )}
            >
              <span
                className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-white"
                style={{ backgroundColor: color }}
              >
                <Icon className="h-3.5 w-3.5" />
              </span>

              <div className="min-w-0 flex-1">
                <div className="flex items-baseline gap-2">
                  <span className="text-xs font-semibold tabular-nums text-muted-foreground">
                    {formatColombo(e.at, 'HH:mm')}
                  </span>
                  <span
                    className={cn(
                      'truncate text-sm font-medium',
                      e.cancelled && 'line-through',
                    )}
                    title={eventTitle(e)}
                  >
                    {eventTitle(e)}
                  </span>
                </div>

                {details.length > 0 && (
                  <p className="mt-0.5 text-xs text-muted-foreground">{details.join(' · ')}</p>
                )}

                {(e.cancelled || e.outOfRange || e.syncedLate || e.timeSource === 'Server') && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {e.cancelled && <Badge variant="destructive">Cancelled</Badge>}
                    {e.outOfRange && (
                      <Badge
                        variant="outline"
                        className="border-amber-300 text-amber-700 dark:border-amber-800 dark:text-amber-400"
                        title="Billed outside the outlet geofence"
                      >
                        Out of range
                      </Badge>
                    )}
                    {e.syncedLate && (
                      <Badge
                        variant="outline"
                        title="Reached the server more than 15 minutes after it was made"
                      >
                        Synced late
                      </Badge>
                    )}
                    {e.timeSource === 'Server' && (
                      <Badge
                        variant="outline"
                        className="text-muted-foreground"
                        title="The phone did not send its own time, so this is when the server received it"
                      >
                        Server time
                      </Badge>
                    )}
                  </div>
                )}
              </div>
            </button>
          </li>
        )
      })}
    </ol>
  )
}
