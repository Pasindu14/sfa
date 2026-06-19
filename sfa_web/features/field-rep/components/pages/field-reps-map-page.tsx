'use client'

import { useEffect, useRef } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { Badge } from '@/components/ui/badge'
import { MapPin, Radio } from 'lucide-react'
import { Spinner } from '@/components/ui/spinner'
import { useFieldRepsLive } from '@/features/field-rep/hooks/field-rep.hooks'
import type { RepLocationPingDto } from '@/features/field-rep/schema/field-rep.schema'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka center
const STALE_THRESHOLD_MS = 15 * 60 * 1000     // 15 minutes

function isStale(recordedAt: string): boolean {
  return Date.now() - new Date(recordedAt).getTime() > STALE_THRESHOLD_MS
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
function RepMarkers({ pings }: { pings: RepLocationPingDto[] }) {
  const map = useMap()
  const infoWindowRef = useRef<google.maps.InfoWindow | null>(null)

  useEffect(() => {
    if (!map || pings.length === 0) return

    const infoWindow = new google.maps.InfoWindow()
    infoWindowRef.current = infoWindow

    const markers = pings.map((p) => {
      const stale = isStale(p.recordedAt)
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
  }, [map, pings])

  return null
}

export function FieldRepsMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''
  const { data: pings = [], isLoading, error } = useFieldRepsLive()

  const activeCount = pings.filter((p) => !isStale(p.recordedAt)).length
  const staleCount = pings.length - activeCount

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Field Reps Live Map</h1>
          <p className="text-muted-foreground">
            {isLoading
              ? 'Loading rep locations...'
              : `${activeCount} active · ${staleCount} stale · updates every 30s`}
          </p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          {isLoading ? 'Loading...' : `${pings.length} reps`}
        </Badge>
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

        <APIProvider apiKey={apiKey}>
          <Map
            defaultCenter={CENTER}
            defaultZoom={8}
            gestureHandling="cooperative"
            className="w-full h-full rounded-xl overflow-hidden border"
          >
            <RepMarkers pings={pings} />
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
