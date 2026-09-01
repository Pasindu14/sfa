'use client'

import { useEffect, useMemo, useRef, useState } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { MapPin, Radio } from 'lucide-react'
import { Spinner } from '@/components/ui/spinner'
import { useFieldRepsLive } from '@/features/field-rep/hooks/field-rep.hooks'
import type { RepLocationPingDto } from '@/features/field-rep/schema/field-rep.schema'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka center
const STALE_THRESHOLD_MS = 15 * 60 * 1000     // 15 minutes
const TICK_MS = 30_000                        // matches the query poll interval

type RepFilter = 'all' | 'active' | 'stale'

const FILTERS: { key: RepFilter; label: string; dot?: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'active', label: 'Active', dot: 'bg-green-500' },
  { key: 'stale', label: 'Stale', dot: 'bg-gray-400' },
]

// `now` is passed in so every marker, count and filter in one render agrees on
// a single instant rather than each calling Date.now() separately.
function isStale(recordedAt: string, now: number = Date.now()): boolean {
  return now - new Date(recordedAt).getTime() > STALE_THRESHOLD_MS
}

function formatLastSeen(recordedAt: string): string {
  const diffMs = Date.now() - new Date(recordedAt).getTime()
  const mins = Math.floor(diffMs / 60_000)
  if (mins < 1) return 'Just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  return `${hrs}h ${mins % 60}m ago`
}

// Must live inside <Map> to access map context via useMap()
function RepMarkers({ pings, now }: { pings: RepLocationPingDto[]; now: number }) {
  const map = useMap()
  const infoWindowRef = useRef<google.maps.InfoWindow | null>(null)

  useEffect(() => {
    if (!map || pings.length === 0) return

    const infoWindow = new google.maps.InfoWindow()
    infoWindowRef.current = infoWindow

    const markers = pings.map((p) => {
      const stale = isStale(p.recordedAt, now)
      const marker = new google.maps.Marker({
        position: { lat: p.latitude, lng: p.longitude },
        map,
        title: p.repName,
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          scale: 9,
          fillColor: stale ? '#9ca3af' : '#22c55e',
          fillOpacity: 1,
          strokeColor: stale ? '#6b7280' : '#16a34a',
          strokeWeight: 2,
        },
      })

      marker.addListener('click', () => {
        infoWindow.setContent(`
          <div style="font-family:sans-serif;min-width:160px;padding:4px 0">
            <div style="font-weight:600;font-size:14px;margin-bottom:4px">${p.repName}</div>
            <div style="font-size:12px;color:#6b7280">Last seen: <b>${formatLastSeen(p.recordedAt)}</b></div>
            <div style="font-size:12px;color:#6b7280">Accuracy: ±${Math.round(p.accuracy)}m</div>
            ${stale ? '<div style="font-size:11px;color:#ef4444;margin-top:4px">Signal lost (stale)</div>' : ''}
          </div>
        `)
        infoWindow.open({ anchor: marker, map })
      })

      return marker
    })

    return () => {
      infoWindow.close()
      markers.forEach((m) => m.setMap(null))
    }
  }, [map, pings, now])

  return null
}

export function FieldRepsMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''
  const { data: pings = [], isLoading, error } = useFieldRepsLive()
  const [filter, setFilter] = useState<RepFilter>('all')

  // Staleness is derived from elapsed time, not from the payload. A poll that
  // returns identical data keeps the same array identity and re-renders
  // nothing, so without this tick a rep would stay "active" indefinitely.
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), TICK_MS)
    return () => clearInterval(id)
  }, [])

  const { active, stale } = useMemo(() => {
    const active: RepLocationPingDto[] = []
    const stale: RepLocationPingDto[] = []
    for (const p of pings) {
      ;(isStale(p.recordedAt, now) ? stale : active).push(p)
    }
    return { active, stale }
  }, [pings, now])

  // Memoised: RepMarkers rebuilds every marker whenever this array's identity
  // changes, so returning a fresh array on each render would thrash the map.
  const visiblePings = useMemo(() => {
    if (filter === 'active') return active
    if (filter === 'stale') return stale
    return pings
  }, [filter, active, stale, pings])

  const counts: Record<RepFilter, number> = {
    all: pings.length,
    active: active.length,
    stale: stale.length,
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Field Reps Live Map</h1>
          <p className="text-muted-foreground">
            {isLoading
              ? 'Loading rep locations...'
              : `${counts.active} active · ${counts.stale} stale · updates every 30s`}
          </p>
        </div>

        <div className="flex items-center gap-3">
          <div
            role="group"
            aria-label="Filter reps by signal status"
            className="inline-flex items-center gap-0.5 rounded-lg border bg-background p-0.5"
          >
            {FILTERS.map((f) => (
              <Button
                key={f.key}
                size="sm"
                variant={filter === f.key ? 'default' : 'ghost'}
                aria-pressed={filter === f.key}
                className="h-7 gap-1.5 px-2.5 text-xs"
                onClick={() => setFilter(f.key)}
              >
                {f.dot && (
                  <span className={`h-2 w-2 shrink-0 rounded-full ${f.dot}`} />
                )}
                {f.label}
                <span className="tabular-nums opacity-70">{counts[f.key]}</span>
              </Button>
            ))}
          </div>

          <Badge variant="secondary" className="text-sm px-3 py-1">
            {isLoading ? 'Loading...' : `${pings.length} reps`}
          </Badge>
        </div>
      </div>

      <div className="relative" style={{ height: 'calc(100vh - 260px)' }}>
        {isLoading && (
          <div className="absolute inset-0 z-20 flex items-center justify-center rounded-xl bg-background/60 backdrop-blur-sm">
            <Spinner className="h-8 w-8" />
          </div>
        )}

        {!isLoading && !error && pings.length === 0 && (
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center rounded-xl bg-background/80 backdrop-blur-sm gap-2">
            <Radio className="h-10 w-10 text-muted-foreground" />
            <p className="text-sm font-medium">No location data yet</p>
            <p className="text-xs text-muted-foreground">
              Reps will appear here once the mobile app sends its first ping
            </p>
          </div>
        )}

        {/* Filter matched nothing — a light banner, not a full overlay, so the
            map stays readable while the user switches filters. */}
        {!isLoading && !error && pings.length > 0 && visiblePings.length === 0 && (
          <div className="absolute left-1/2 top-4 z-10 -translate-x-1/2 rounded-full border bg-background/95 px-4 py-1.5 text-xs shadow-md">
            No {filter} reps right now
          </div>
        )}

        <APIProvider apiKey={apiKey}>
          <Map
            defaultCenter={CENTER}
            defaultZoom={8}
            gestureHandling="cooperative"
            className="w-full h-full rounded-xl overflow-hidden border"
          >
            <RepMarkers pings={visiblePings} now={now} />
          </Map>
        </APIProvider>

        {/* Legend */}
        <div className="absolute top-4 right-4 z-10 rounded-lg border bg-background shadow-md p-3 w-48 space-y-2 text-xs">
          <p className="font-semibold text-sm">Legend</p>
          <div className="flex items-center gap-2 text-muted-foreground">
            <span className="h-3 w-3 rounded-full bg-green-500 shrink-0" />
            Active (pinged &lt;15 min)
          </div>
          <div className="flex items-center gap-2 text-muted-foreground">
            <span className="h-3 w-3 rounded-full bg-gray-400 shrink-0" />
            Stale (pinged &gt;15 min)
          </div>
          <div className="flex items-center gap-2 text-muted-foreground">
            <MapPin className="h-3 w-3 shrink-0" />
            Click marker for details
          </div>
        </div>
      </div>
    </div>
  )
}
