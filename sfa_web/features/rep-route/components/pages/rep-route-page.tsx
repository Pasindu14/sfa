'use client'

import { useEffect, useMemo, useState } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { Spinner } from '@/components/ui/spinner'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import { MapPin, Route as RouteIcon } from 'lucide-react'
import { RepSelect } from '../selects/rep-select'
import { useRepRoute } from '../../hooks/rep-route.hooks'
import type { RepRoutePointDto } from '../../schema/rep-route.schema'
import { formatColombo, toColomboDateStr } from '@/lib/utils/datetime'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka centre — fallback before a route loads

function formatDistance(meters: number): string {
  if (meters < 1000) return `${Math.round(meters)} m`
  return `${(meters / 1000).toFixed(1)} km`
}

/**
 * Draws the trail. Must live inside <Map> — that's the only place useMap() resolves.
 *
 * Every overlay is created imperatively and torn down in the cleanup, matching the existing
 * map pages. The per-ping dots matter as much as the line: pings are only every ~5 minutes
 * and are dropped entirely when accuracy is poor, so a long straight segment means "no data
 * here", not "he drove in a straight line". The dots make that gap visible.
 */
function RouteTrail({ points }: { points: RepRoutePointDto[] }) {
  const map = useMap()

  useEffect(() => {
    if (!map || points.length === 0) return

    const path = points.map((p) => ({ lat: p.latitude, lng: p.longitude }))

    const polyline = new google.maps.Polyline({
      path,
      map,
      geodesic: true,
      strokeColor: '#f97316',
      strokeOpacity: 0.9,
      strokeWeight: 4,
    })

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
      polyline.setMap(null)
      dots.forEach((d) => d.setMap(null))
      start.setMap(null)
      end?.setMap(null)
    }
  }, [map, points])

  return null
}

export function RepRoutePage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''

  const [repId, setRepId] = useState<number | null>(null)
  const [selectedDate, setSelectedDate] = useState<Date>(() => new Date())

  // Always serialise through Colombo time — the API's business day is Asia/Colombo, so
  // sending the browser's local date would shift the day for anyone outside +05:30.
  const dateStr = useMemo(() => toColomboDateStr(selectedDate), [selectedDate])

  const { data: route, isLoading, isError, error } = useRepRoute(repId, dateStr)

  // Stable identity so RouteTrail's effect doesn't rebuild every overlay on each render.
  const points = useMemo(() => route?.points ?? [], [route])

  const hasSelection = !!repId
  const isEmpty = hasSelection && !isLoading && !isError && points.length === 0

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col gap-4 rounded-lg bg-muted/90 p-10 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Rep Route History</h1>
          <p className="text-muted-foreground">
            {route && points.length > 0
              ? `${route.repName} · ${formatDistance(route.summary.totalDistanceMeters)} travelled · ${route.summary.pointCount} pings`
              : 'Select a sales rep and a date to see where they travelled'}
          </p>
        </div>

        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <RepSelect value={repId} onChange={setRepId} />
          <CalendarDatePicker
            id="rep-route-date"
            date={{ from: selectedDate, to: selectedDate }}
            onDateSelect={({ from }) => from && setSelectedDate(from)}
            numberOfMonths={1}
            variant="outline"
            className="w-fit cursor-pointer"
          />
        </div>
      </div>

      {route && points.length > 0 && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <SummaryTile label="Distance" value={formatDistance(route.summary.totalDistanceMeters)} />
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

        {!hasSelection && (
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-2 rounded-xl bg-background/80 backdrop-blur-sm">
            <RouteIcon className="h-10 w-10 text-muted-foreground" />
            <p className="text-sm font-medium">Pick a sales rep to begin</p>
            <p className="text-xs text-muted-foreground">
              Their route for {formatColombo(selectedDate)} will be drawn here
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
              No location data for {route?.repName ?? 'this rep'} on {formatColombo(selectedDate)}
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
            <RouteTrail points={points} />
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
            Travelled path
          </div>
          <p className="pt-1 text-[11px] leading-snug text-muted-foreground">
            Each dot is one recorded position. A long straight run means no data in between.
          </p>
        </div>
      </div>
    </div>
  )
}

function SummaryTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-background p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-lg font-semibold tabular-nums">{value}</p>
    </div>
  )
}
