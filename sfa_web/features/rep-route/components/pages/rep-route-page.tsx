'use client'

import { useCallback, useEffect, useMemo, useState } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { Spinner } from '@/components/ui/spinner'
import { Button } from '@/components/ui/button'
import { DateOnlyPicker } from '@/components/date-only-picker'
import { MapPin, Route as RouteIcon, Search, TriangleAlert } from 'lucide-react'
import { cn } from '@/lib/utils'
import { RepSelect } from '../selects/rep-select'
import { ActivityMarkers } from '../map/activity-markers'
import { RepTimelinePanel } from '../timeline/rep-timeline-panel'
import {
  IDLE_HIGHLIGHT_MINUTES,
  MARKER_COLORS,
  formatLkr,
  formatMinutes,
  type TimelineSelection,
} from '../timeline/timeline-display'
import { useRepTimeline } from '../../hooks/rep-route.hooks'
import type {
  RepDayTimelineDto,
  RepRoutePointDto,
  RepTimelineEventDto,
} from '../../schema/rep-route.schema'
import { formatColombo, toColomboDateStr } from '@/lib/utils/datetime'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka centre — fallback before a route loads

/**
 * Fallback only. The real threshold comes from the server (`summary.gapThresholdMinutes`),
 * so the segments drawn dashed are exactly the ones excluded from the distance total — a
 * local copy would silently drift out of step with the calculation.
 */
const FALLBACK_GAP_MINUTES = 15

const TRAIL_COLOR = '#f97316'

/**
 * Reason codes from the phone, turned into something an admin can act on. Unknown codes
 * fall through verbatim so a newer app version reporting something new still shows it.
 */
const SKIP_REASONS: Record<string, string> = {
  PermissionDenied: 'Location permission is denied on the phone',
  LocationServicesOff: 'Location is switched off on the phone',
  NoFixTimeout: 'No GPS fix — usually indoors or poor sky view',
  AccuracyTooPoor: 'GPS fix too weak to trust (over 100 m accuracy)',
  ZeroCoordinate: 'Phone reported an invalid position',
  CaptureError: 'The phone hit an error while reading location',
}

function formatDistance(meters: number): string {
  if (meters < 1000) return `${Math.round(meters)} m`
  return `${(meters / 1000).toFixed(1)} km`
}

type LatLng = { lat: number; lng: number }

/**
 * Splits the trail into solid runs (consecutive pings close together in time) and gap
 * segments (a jump across missing data). Returned as separate paths so each can be styled
 * differently — one polyline cannot be part solid and part dashed.
 */
function splitTrail(
  points: RepRoutePointDto[],
  gapThresholdMs: number,
): { solid: LatLng[][]; gaps: LatLng[][] } {
  const at = (p: RepRoutePointDto): LatLng => ({ lat: p.latitude, lng: p.longitude })

  const solid: LatLng[][] = []
  const gaps: LatLng[][] = []
  let run: LatLng[] = points.length > 0 ? [at(points[0])] : []

  for (let i = 1; i < points.length; i++) {
    const elapsed =
      new Date(points[i].recordedAt).getTime() - new Date(points[i - 1].recordedAt).getTime()

    if (elapsed > gapThresholdMs) {
      if (run.length > 1) solid.push(run)
      gaps.push([at(points[i - 1]), at(points[i])])
      run = [at(points[i])]
    } else {
      run.push(at(points[i]))
    }
  }
  if (run.length > 1) solid.push(run)

  return { solid, gaps }
}

/**
 * Draws the trail. Must live inside <Map> — that's the only place useMap() resolves.
 *
 * Every overlay is created imperatively and torn down in the cleanup, matching the existing
 * map pages. The per-ping dots matter as much as the line: pings are only every ~5 minutes
 * and are dropped entirely when accuracy is poor, so a long straight segment means "no data
 * here", not "he drove in a straight line". The dots make that gap visible.
 */
function RouteTrail({
  points,
  gapThresholdMs,
}: {
  points: RepRoutePointDto[]
  gapThresholdMs: number
}) {
  const map = useMap()

  useEffect(() => {
    if (!map || points.length === 0) return

    const path = points.map((p) => ({ lat: p.latitude, lng: p.longitude }))
    const { solid, gaps } = splitTrail(points, gapThresholdMs)

    const solidLines = solid.map(
      (segment) =>
        new google.maps.Polyline({
          path: segment,
          map,
          geodesic: true,
          strokeColor: TRAIL_COLOR,
          strokeOpacity: 0.9,
          strokeWeight: 4,
        }),
    )

    // Google Maps has no dash property — a dashed line is a fully transparent stroke with a
    // repeating dash symbol painted along it.
    const gapLines = gaps.map(
      (segment) =>
        new google.maps.Polyline({
          path: segment,
          map,
          geodesic: true,
          strokeOpacity: 0,
          icons: [
            {
              icon: {
                path: 'M 0,-1 0,1',
                strokeColor: TRAIL_COLOR,
                strokeOpacity: 0.7,
                strokeWeight: 3,
                scale: 3,
              },
              offset: '0',
              repeat: '14px',
            },
          ],
        }),
    )

    const dots = points.map(
      (p) =>
        new google.maps.Marker({
          position: { lat: p.latitude, lng: p.longitude },
          map,
          title: formatColombo(p.recordedAt, 'HH:mm'),
          icon: {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 3.5,
            fillColor: '#f97316',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 1,
          },
          zIndex: 2,
        }),
    )

    const endpoint = (p: RepRoutePointDto, label: string, color: string) =>
      new google.maps.Marker({
        position: { lat: p.latitude, lng: p.longitude },
        map,
        label: { text: label, color: '#ffffff', fontSize: '11px', fontWeight: 'bold' },
        title: `${label === 'A' ? 'First' : 'Last'} ping — ${formatColombo(p.recordedAt, 'HH:mm')}`,
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          scale: 10,
          fillColor: color,
          fillOpacity: 1,
          strokeColor: '#ffffff',
          strokeWeight: 2,
        },
        zIndex: 3,
      })

    const start = endpoint(points[0], 'A', '#16a34a')
    const end = points.length > 1 ? endpoint(points[points.length - 1], 'B', '#dc2626') : null

    // Frame the trail rather than the whole country — a rep who worked one town should
    // fill the screen instead of being a dot on a national view.
    const bounds = new google.maps.LatLngBounds()
    path.forEach((p) => bounds.extend(p))
    map.fitBounds(bounds, 64)

    return () => {
      solidLines.forEach((l) => l.setMap(null))
      gapLines.forEach((l) => l.setMap(null))
      dots.forEach((d) => d.setMap(null))
      start.setMap(null)
      end?.setMap(null)
    }
  }, [map, points, gapThresholdMs])

  return null
}

export function RepRoutePage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''

  // Pending = what's in the controls. Applied = what's actually been requested. Keeping
  // them separate means changing a filter doesn't fire a query; only the button does.
  const [repId, setRepId] = useState<number | null>(null)
  // Held as a Colombo `YYYY-MM-DD` string, which is exactly what the API's business-date
  // param expects — no Date→string conversion to get wrong on the way out.
  const [pendingDate, setPendingDate] = useState<string>(() => toColomboDateStr(new Date()))
  const [applied, setApplied] = useState<{ repId: number; date: string } | null>(null)

  // One call for the whole day — the route rides along inside the timeline. Fetching them
  // separately would queue, since Next.js runs server actions one at a time.
  const {
    data: timeline,
    isLoading,
    isError,
    error,
    refetch,
  } = useRepTimeline(applied?.repId ?? null, applied?.date ?? null)
  const route = timeline?.route

  // Stable identity so RouteTrail's effect doesn't rebuild every overlay on each render.
  const points = useMemo(() => route?.points ?? [], [route])
  const events = useMemo(() => timeline?.events ?? [], [timeline])

  // Tagged with the events array it indexes into, so a reload (new array) drops a stale
  // selection on its own instead of highlighting whatever now sits at that index.
  const [selectionState, setSelectionState] = useState<
    (TimelineSelection & { events: RepTimelineEventDto[] }) | null
  >(null)
  const selection = selectionState?.events === events ? selectionState : null

  const selectFromList = useCallback(
    (index: number) => setSelectionState({ index, source: 'list', events }),
    [events],
  )
  const selectFromMap = useCallback(
    (index: number) => setSelectionState({ index, source: 'map', events }),
    [events],
  )

  const gapThresholdMs =
    (route?.summary.gapThresholdMinutes ?? FALLBACK_GAP_MINUTES) * 60_000

  const hasData = points.length > 0 || events.length > 0
  const isEmpty = !!applied && !isLoading && !isError && !hasData

  // Only surface the failure if it is more recent than the last position we got — otherwise
  // the phone already recovered and the warning would be noise.
  const status = route?.trackingStatus ?? null
  const statusIsCurrent =
    !!status &&
    (!route?.summary.lastPingAt ||
      new Date(status.reportedAt) > new Date(route.summary.lastPingAt))

  // The controls have moved on from what's drawn — say so, rather than letting the map
  // silently disagree with the filters above it.
  const isDirty =
    !!applied && (applied.repId !== repId || applied.date !== pendingDate)

  const showRoute = () => {
    if (!repId) return
    const next = { repId, date: pendingDate }
    const unchanged = applied?.repId === next.repId && applied?.date === next.date
    setApplied(next)
    // Same rep + date means the query key doesn't change, so nothing would refetch on its
    // own. Force it — pings queued offline back-fill into past days, so re-pressing the
    // button on yesterday can legitimately return more data than it did an hour ago.
    if (unchanged) refetch()
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="rounded-lg bg-muted/90 p-10">
        <h1 className="text-3xl font-bold tracking-tight">Rep Day</h1>
        <p className="text-muted-foreground">
          {timeline && route && hasData
            ? `${timeline.repName} · ${timeline.assignment?.routeName ?? 'no route assigned'} · ${formatDistance(route.summary.measuredDistanceMeters)} recorded · ${route.summary.pointCount} pings` +
              (route.summary.gapCount > 0
                ? ` · ${route.summary.gapCount} gap${route.summary.gapCount === 1 ? '' : 's'} not measured`
                : '')
            : 'Select a sales rep and a date to see where they travelled and what they did along the way'}
        </p>
      </div>

      {/* Filters live in their own row rather than inside the hero card — the date
          picker's popover trigger was being clipped by the card's padded edge. */}
      <div className="flex flex-col gap-4 rounded-lg border bg-background p-4 sm:flex-row sm:flex-wrap sm:items-end">
        {/* Width lives here, not on the select — AsyncSelect applies its `width` prop as an
            inline style on the trigger, which no Tailwind class can override. */}
        <div className="flex w-full flex-col gap-1.5 sm:w-96">
          <label className="text-xs font-medium text-muted-foreground">Sales rep</label>
          <RepSelect value={repId} onChange={setRepId} />
        </div>

        <div className="flex w-full flex-col gap-1.5 sm:w-72">
          <label className="text-xs font-medium text-muted-foreground">Date</label>
          <DateOnlyPicker
            id="rep-route-date"
            value={pendingDate}
            onChange={setPendingDate}
            className="h-10 w-full cursor-pointer"
          />
        </div>

        <Button
          onClick={showRoute}
          disabled={!repId || isLoading}
          className="h-10 gap-2 sm:w-40"
        >
          {isLoading ? <Spinner className="h-4 w-4" /> : <Search className="h-4 w-4" />}
          Show route
        </Button>

        {isDirty && (
          <p className="self-center text-xs text-muted-foreground">
            Filters changed — press <span className="font-medium">Show route</span> to reload
          </p>
        )}
      </div>

      {timeline && hasData && <DayKpis timeline={timeline} />}

      {statusIsCurrent && status && (
        <div className="flex items-start gap-3 rounded-lg border border-amber-300 bg-amber-50 p-3 dark:border-amber-900 dark:bg-amber-950/40">
          <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
          <div className="text-sm">
            <p className="font-medium">
              The phone stopped recording positions —{' '}
              {SKIP_REASONS[status.reason] ?? status.reason}
            </p>
            <p className="text-xs text-muted-foreground">
              Reported at {formatColombo(status.reportedAt, 'd MMM, HH:mm')}
              {status.accuracyMeters != null &&
                ` · fix was ±${Math.round(status.accuracyMeters)} m`}
              . The tracking service is still running — it just has nothing usable to send.
            </p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="relative lg:col-span-2" style={{ height: 'calc(100vh - 320px)' }}>
          {isLoading && (
            <div className="absolute inset-0 z-20 flex items-center justify-center rounded-xl bg-background/60 backdrop-blur-sm">
              <Spinner className="h-8 w-8" />
            </div>
          )}

          {!applied && (
            <div className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-2 rounded-xl bg-background/80 backdrop-blur-sm">
              <RouteIcon className="h-10 w-10 text-muted-foreground" />
              <p className="text-sm font-medium">Choose a sales rep and a date</p>
              <p className="text-xs text-muted-foreground">
                Then press <span className="font-medium">Show route</span> to load the day
              </p>
            </div>
          )}

          {isError && (
            <div className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-2 rounded-xl bg-background/80 backdrop-blur-sm">
              <MapPin className="h-10 w-10 text-destructive" />
              <p className="text-sm font-medium">Could not load this route</p>
              <p className="text-xs text-muted-foreground">
                {error instanceof Error ? error.message : 'Unknown error'}
              </p>
            </div>
          )}

          {isEmpty && (
            <div className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-2 rounded-xl bg-background/80 backdrop-blur-sm">
              <MapPin className="h-10 w-10 text-muted-foreground" />
              <p className="text-sm font-medium">
                No location data or activity for {timeline?.repName ?? 'this rep'} on{' '}
                {formatColombo(`${applied?.date}T00:00:00`)}
              </p>
              <p className="max-w-md text-center text-xs text-muted-foreground">
                {status
                  ? `${SKIP_REASONS[status.reason] ?? status.reason} — last reported ${formatColombo(status.reportedAt, 'd MMM, HH:mm')}.`
                  : 'No reason was reported either, which usually means the tracking service was not running at all.'}
              </p>
            </div>
          )}

          <APIProvider apiKey={apiKey}>
            <Map
              defaultCenter={CENTER}
              defaultZoom={8}
              gestureHandling="cooperative"
              className="h-full w-full overflow-hidden rounded-xl border"
            >
              <RouteTrail points={points} gapThresholdMs={gapThresholdMs} />
              <ActivityMarkers
                events={events}
                selection={selection}
                onSelect={selectFromMap}
                fitToEvents={points.length === 0}
              />
            </Map>
          </APIProvider>

          <div className="absolute top-4 right-4 z-10 max-h-[calc(100%-2rem)] w-52 space-y-2 overflow-y-auto rounded-lg border bg-background p-3 text-xs shadow-md">
            <p className="text-sm font-semibold">Legend</p>
            <div className="flex items-center gap-2 text-muted-foreground">
              <span className="inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-green-600 text-[9px] font-bold text-white">
                A
              </span>
              First ping of the day
            </div>
            <div className="flex items-center gap-2 text-muted-foreground">
              <span className="inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-red-600 text-[9px] font-bold text-white">
                B
              </span>
              Last ping of the day
            </div>
            <div className="flex items-center gap-2 text-muted-foreground">
              <span className="h-1 w-5 shrink-0 rounded bg-orange-500" />
              Recorded path
            </div>
            <div className="flex items-center gap-2 text-muted-foreground">
              <span
                className="h-1 w-5 shrink-0 rounded"
                style={{
                  backgroundImage:
                    'repeating-linear-gradient(to right, #f97316 0 3px, transparent 3px 6px)',
                }}
              />
              Gap — no data
            </div>
            {LEGEND_MARKERS.map(({ kind, label }) => (
              <div key={kind} className="flex items-center gap-2 text-muted-foreground">
                <span
                  className="h-3 w-3 shrink-0 rounded-full border-2 border-white shadow-sm"
                  style={{ backgroundColor: MARKER_COLORS[kind] }}
                />
                {label}
              </div>
            ))}
            <p className="pt-1 text-[11px] leading-snug text-muted-foreground">
              Each small dot is one recorded position. A dashed run means more than{' '}
              {route?.summary.gapThresholdMinutes ?? FALLBACK_GAP_MINUTES} minutes passed with
              no ping — the path there is unknown, so it is left out of the distance.
            </p>
          </div>
        </div>

        <div className="flex h-[480px] flex-col overflow-hidden rounded-xl border bg-background lg:h-[calc(100vh-320px)]">
          <div className="flex items-baseline justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold">Activity</p>
            {events.length > 0 && (
              <p className="text-xs text-muted-foreground">Click an entry to find it on the map</p>
            )}
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto">
            {timeline ? (
              <RepTimelinePanel events={events} selection={selection} onSelect={selectFromList} />
            ) : (
              <div className="flex h-full items-center justify-center p-6 text-center text-xs text-muted-foreground">
                {isLoading ? <Spinner className="h-6 w-6" /> : 'The day’s bills, visits and stops appear here'}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

const LEGEND_MARKERS = [
  { kind: 'Bill', label: 'Bill' },
  { kind: 'NoSale', label: 'No-sale visit' },
  { kind: 'Stop', label: 'Stop with nothing recorded' },
  { kind: 'Unlock', label: 'Route unlock request' },
] as const

/**
 * The day at a glance. Distance stays from the route summary; everything else is the
 * timeline's own KPIs, so tiles and list are computed by the same server pass.
 */
function DayKpis({ timeline }: { timeline: RepDayTimelineDto }) {
  const s = timeline.summary
  const route = timeline.route.summary
  const flagCount =
    s.gpsGapCount + s.unrecordedStopCount + s.outOfRangeBillCount + s.lateSyncCount
  const plural = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 xl:grid-cols-7">
      <SummaryTile
        label="Working time"
        value={
          s.dayStartAt
            ? `${formatColombo(s.dayStartAt, 'HH:mm')}–${formatColombo(s.dayEndAt, 'HH:mm')}`
            : '—'
        }
        hint={
          `${formatMinutes(s.workingMinutes)}` +
          (s.firstActivityAt ? ` · first sale/visit ${formatColombo(s.firstActivityAt, 'HH:mm')}` : '')
        }
      />
      <SummaryTile
        label="Bills"
        value={String(s.billCount)}
        hint={
          formatLkr(s.billRevenue) +
          (s.cancelledBillCount > 0 ? ` · ${s.cancelledBillCount} cancelled` : '')
        }
      />
      <SummaryTile
        label="Coverage"
        value={
          s.plannedOutlets != null
            ? `${s.outletsCovered}/${s.plannedOutlets}` +
              (s.coveragePercent != null ? ` (${Math.round(s.coveragePercent)}%)` : '')
            : String(s.outletsCovered)
        }
        hint={timeline.assignment?.routeName ?? 'no route assigned — outlets visited'}
      />
      <SummaryTile label="No-sale visits" value={String(s.noSaleCount)} />
      <SummaryTile
        label="Longest idle"
        value={formatMinutes(s.longestIdleMinutes)}
        hint="between consecutive bills / visits"
        warn={(s.longestIdleMinutes ?? 0) >= IDLE_HIGHLIGHT_MINUTES}
      />
      <SummaryTile
        label="Flags"
        value={String(flagCount)}
        hint={
          flagCount === 0
            ? 'nothing unusual'
            : [
                s.gpsGapCount > 0 &&
                  `${plural(s.gpsGapCount, 'GPS gap')} (${formatMinutes(s.gpsGapMinutes)})`,
                s.unrecordedStopCount > 0 && plural(s.unrecordedStopCount, 'unrecorded stop'),
                s.outOfRangeBillCount > 0 && `${s.outOfRangeBillCount} out of range`,
                s.lateSyncCount > 0 && `${s.lateSyncCount} late sync`,
              ]
                .filter(Boolean)
                .join(' · ')
        }
        warn={flagCount > 0}
      />
      <SummaryTile
        label="Distance recorded"
        value={formatDistance(route.measuredDistanceMeters)}
        hint={
          route.gapCount > 0
            ? `excludes ${plural(route.gapCount, 'gap')} · ${route.pointCount} pings`
            : `straight-line, not road distance · ${route.pointCount} pings`
        }
      />
    </div>
  )
}

function SummaryTile({
  label,
  value,
  hint,
  warn = false,
}: {
  label: string
  value: string
  hint?: string
  warn?: boolean
}) {
  return (
    <div
      className={cn(
        'rounded-lg border bg-background p-3',
        warn && 'border-amber-300 bg-amber-50/60 dark:border-amber-900 dark:bg-amber-950/30',
      )}
    >
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-lg font-semibold tabular-nums">{value}</p>
      {hint && <p className="text-[11px] leading-tight text-muted-foreground">{hint}</p>}
    </div>
  )
}
