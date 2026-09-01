'use client'

import { useEffect, useMemo, useState } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { Spinner } from '@/components/ui/spinner'
import { Button } from '@/components/ui/button'
import { DateOnlyPicker } from '@/components/date-only-picker'
import { MapPin, Route as RouteIcon, Search } from 'lucide-react'
import { RepSelect } from '../selects/rep-select'
import { useRepRoute } from '../../hooks/rep-route.hooks'
import type { RepRoutePointDto } from '../../schema/rep-route.schema'
import { formatColombo, toColomboDateStr } from '@/lib/utils/datetime'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka centre — fallback before a route loads

/**
 * Fallback only. The real threshold comes from the server (`summary.gapThresholdMinutes`),
 * so the segments drawn dashed are exactly the ones excluded from the distance total — a
 * local copy would silently drift out of step with the calculation.
 */
const FALLBACK_GAP_MINUTES = 15

const TRAIL_COLOR = '#f97316'

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

  const {
    data: route,
    isLoading,
    isError,
    error,
    refetch,
  } = useRepRoute(applied?.repId ?? null, applied?.date ?? null)

  // Stable identity so RouteTrail's effect doesn't rebuild every overlay on each render.
  const points = useMemo(() => route?.points ?? [], [route])

  const gapThresholdMs =
    (route?.summary.gapThresholdMinutes ?? FALLBACK_GAP_MINUTES) * 60_000

  const isEmpty = !!applied && !isLoading && !isError && points.length === 0

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
        <h1 className="text-3xl font-bold tracking-tight">Rep Route History</h1>
        <p className="text-muted-foreground">
          {route && points.length > 0
            ? `${route.repName} · ${formatDistance(route.summary.measuredDistanceMeters)} recorded · ${route.summary.pointCount} pings` +
              (route.summary.gapCount > 0
                ? ` · ${route.summary.gapCount} gap${route.summary.gapCount === 1 ? '' : 's'} not measured`
                : '')
            : 'Select a sales rep and a date to see where they travelled'}
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

      {route && points.length > 0 && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <SummaryTile
            label="Distance recorded"
            value={formatDistance(route.summary.measuredDistanceMeters)}
            hint={
              route.summary.gapCount > 0
                ? `excludes ${route.summary.gapCount} gap${route.summary.gapCount === 1 ? '' : 's'}`
                : 'straight-line, not road distance'
            }
          />
          <SummaryTile label="Pings" value={String(route.summary.pointCount)} />
          <SummaryTile label="First ping" value={formatColombo(route.summary.firstPingAt, 'HH:mm')} />
          <SummaryTile label="Last ping" value={formatColombo(route.summary.lastPingAt, 'HH:mm')} />
        </div>
      )}

      <div className="relative" style={{ height: 'calc(100vh - 320px)' }}>
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
              No location data for {route?.repName ?? 'this rep'} on{' '}
              {formatColombo(`${applied?.date}T00:00:00`)}
            </p>
            <p className="max-w-md text-center text-xs text-muted-foreground">
              The app records a position every 5 minutes, but skips it when location is off,
              the GPS fix is too weak, or the phone stopped the tracking service.
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
          </Map>
        </APIProvider>

        <div className="absolute top-4 right-4 z-10 w-52 space-y-2 rounded-lg border bg-background p-3 text-xs shadow-md">
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
          <p className="pt-1 text-[11px] leading-snug text-muted-foreground">
            Each dot is one recorded position. A dashed run means more than{' '}
            {route?.summary.gapThresholdMinutes ?? FALLBACK_GAP_MINUTES} minutes passed with
            no ping — the path there is unknown, so it is left out of the distance.
          </p>
        </div>
      </div>
    </div>
  )
}

function SummaryTile({
  label,
  value,
  hint,
}: {
  label: string
  value: string
  hint?: string
}) {
  return (
    <div className="rounded-lg border bg-background p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-lg font-semibold tabular-nums">{value}</p>
      {hint && <p className="text-[11px] leading-tight text-muted-foreground">{hint}</p>}
    </div>
  )
}
