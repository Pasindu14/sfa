'use client'

import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { APIProvider, Map, useMap, type MapEvent } from '@vis.gl/react-google-maps'
import { MarkerClusterer, SuperClusterAlgorithm } from '@googlemaps/markerclusterer'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MapPin } from 'lucide-react'
import { Spinner } from '@/components/ui/spinner'
import { useOutletsForMap } from '@/features/outlet/hooks/outlet.hooks'
import type { OutletMapBounds } from '@/features/outlet/actions/outlet.actions'
import type { OutletMapPointDto } from '@/features/outlet/schema/outlet.schema'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka center

const IDLE_DEBOUNCE_MS = 400
const BOUNDS_PADDING = 0.2 // fetch 20% beyond each edge so small pans don't refetch
const BOUNDS_FACTOR = 10_000 // round to 4 decimals (~11m) so query keys stay stable
// After zooming far into a previously fetched box, shrink it so we don't hold a huge payload.
const MAX_FETCHED_TO_VIEW_AREA_RATIO = 25

type Box = OutletMapBounds

const boxArea = (b: Box) => (b.maxLat - b.minLat) * (b.maxLng - b.minLng)
const boxContains = (outer: Box, inner: Box) =>
  inner.minLat >= outer.minLat &&
  inner.maxLat <= outer.maxLat &&
  inner.minLng >= outer.minLng &&
  inner.maxLng <= outer.maxLng
const sameBox = (a: Box | null | undefined, b: Box | null) =>
  a === b ||
  (!!a &&
    !!b &&
    a.minLat === b.minLat &&
    a.minLng === b.minLng &&
    a.maxLat === b.maxLat &&
    a.maxLng === b.maxLng)

/**
 * Current viewport as a plain box, or null when a box can't describe it
 * (bounds unavailable, crossing the antimeridian, or spanning > 180° lng).
 */
function readViewport(map: google.maps.Map): Box | null {
  const bounds = map.getBounds()
  if (!bounds) return null
  const sw = bounds.getSouthWest()
  const ne = bounds.getNorthEast()
  const minLng = sw.lng()
  const maxLng = ne.lng()
  if (minLng > maxLng || maxLng - minLng > 180) return null
  return { minLat: sw.lat(), minLng, maxLat: ne.lat(), maxLng }
}

/** Pads the viewport and rounds outward; null if the padded box would cross the antimeridian. */
function padViewport(view: Box): Box | null {
  const dLat = (view.maxLat - view.minLat) * BOUNDS_PADDING
  const dLng = (view.maxLng - view.minLng) * BOUNDS_PADDING
  const minLng = view.minLng - dLng
  const maxLng = view.maxLng + dLng
  if (minLng < -180 || maxLng > 180) return null
  const down = (v: number) => Math.floor(v * BOUNDS_FACTOR) / BOUNDS_FACTOR
  const up = (v: number) => Math.ceil(v * BOUNDS_FACTOR) / BOUNDS_FACTOR
  return {
    minLat: Math.max(-90, down(view.minLat - dLat)),
    minLng: down(minLng),
    maxLat: Math.min(90, up(view.maxLat + dLat)),
    maxLng: up(maxLng),
  }
}

// Must live inside <Map> to access map context
function ClusteredMarkers({ outlets }: { outlets: OutletMapPointDto[] }) {
  const map = useMap()
  const clustererRef = useRef<MarkerClusterer | null>(null)
  const markersRef = useRef<globalThis.Map<number, google.maps.Marker>>(new globalThis.Map())

  // One clusterer per map instance. Declared before the diff effect so it runs first.
  useEffect(() => {
    if (!map) return
    const markers = markersRef.current
    const instance = new MarkerClusterer({
      map,
      algorithm: new SuperClusterAlgorithm({ radius: 60, maxZoom: 16 }),
    })
    clustererRef.current = instance
    return () => {
      instance.clearMarkers()
      instance.setMap(null)
      markers.forEach((m) => m.setMap(null))
      markers.clear()
      clustererRef.current = null
    }
  }, [map])

  // Diff markers by outlet id so points present before and after a viewport fetch don't blink
  useEffect(() => {
    const clusterer = clustererRef.current
    if (!map || !clusterer) return
    const current = markersRef.current
    const nextIds = new Set(outlets.map((o) => o.id))

    const gone: google.maps.Marker[] = []
    current.forEach((marker, id) => {
      if (!nextIds.has(id)) {
        gone.push(marker)
        current.delete(id)
      }
    })

    const moved: google.maps.Marker[] = []
    const added: google.maps.Marker[] = []
    for (const o of outlets) {
      const existing = current.get(o.id)
      if (existing) {
        const pos = existing.getPosition()
        if (pos?.lat() !== o.latitude || pos?.lng() !== o.longitude) {
          existing.setPosition({ lat: o.latitude, lng: o.longitude })
          moved.push(existing)
        }
        if (existing.getTitle() !== o.name) existing.setTitle(o.name)
        continue
      }
      // Legacy Marker: canvas-rendered (GPU), not DOM-based — stays smooth at 5000+ points
      const marker = new google.maps.Marker({
        position: { lat: o.latitude, lng: o.longitude },
        title: o.name,
        optimized: true, // batch all markers into a single canvas layer
      })
      current.set(o.id, marker)
      added.push(marker)
    }

    if (gone.length === 0 && moved.length === 0 && added.length === 0) return
    if (gone.length > 0 || moved.length > 0) clusterer.removeMarkers([...gone, ...moved], true)
    gone.forEach((m) => m.setMap(null))
    clusterer.addMarkers([...moved, ...added], true)
    clusterer.render()
  }, [map, outlets])

  return null
}

export function OutletMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''

  // undefined = viewport not known yet, null = full list, Box = viewport fetch.
  // The page never auto-fit to all outlets (fixed Sri Lanka center/zoom), so the
  // first fetch already uses the initial viewport.
  const [fetchBounds, setFetchBounds] = useState<Box | null | undefined>(undefined)
  const fetchBoundsRef = useRef<Box | null | undefined>(undefined)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const applyViewport = useCallback((map: google.maps.Map) => {
    const view = readViewport(map)
    const prev = fetchBoundsRef.current
    let next: Box | null
    if (!view) {
      next = null
    } else if (prev === null) {
      return // full list already loaded — it covers every viewport
    } else if (
      prev &&
      boxContains(prev, view) &&
      boxArea(prev) <= boxArea(view) * MAX_FETCHED_TO_VIEW_AREA_RATIO
    ) {
      return // still inside the padded box we already fetched
    } else {
      next = padViewport(view)
    }
    if (prev !== undefined && sameBox(prev, next)) return
    fetchBoundsRef.current = next
    setFetchBounds(next)
  }, [])

  const handleIdle = useCallback(
    (event: MapEvent) => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
      // First idle fetches immediately; later pans/zooms are debounced
      if (fetchBoundsRef.current === undefined) {
        applyViewport(event.map)
        return
      }
      debounceRef.current = setTimeout(() => applyViewport(event.map), IDLE_DEBOUNCE_MS)
    },
    [applyViewport]
  )

  useEffect(
    () => () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
    },
    []
  )

  const { data, isPending, isFetching, error } = useOutletsForMap(fetchBounds)
  const isViewportMode = fetchBounds !== null
  const isLoading = isPending && !error
  const loadedOutlets = useMemo(() => data ?? [], [data])

  // Filter out 0,0 placeholders — those have no real coordinates yet
  const mappableOutlets = useMemo(
    () => loadedOutlets.filter((o) => !(o.latitude === 0 && o.longitude === 0)),
    [loadedOutlets]
  )
  const noCoords = loadedOutlets.length - mappableOutlets.length

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Outlet Map</h1>
          <p className="text-muted-foreground">
            {isLoading
              ? 'Loading outlets...'
              : isViewportMode
              ? 'Showing outlets around the current map view'
              : noCoords > 0
              ? `${mappableOutlets.length} mapped · ${noCoords} missing coordinates`
              : 'Visualise outlet locations across the region'}
          </p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          {isLoading
            ? 'Loading...'
            : isViewportMode
            ? `${mappableOutlets.length} outlets in this area`
            : `${mappableOutlets.length} outlets`}
        </Badge>
      </div>

      <div className="relative" style={{ height: 'calc(100vh - 260px)' }}>
        {isLoading && (
          <div className="absolute inset-0 z-20 flex items-center justify-center rounded-xl bg-background/60 backdrop-blur-sm">
            <Spinner className="h-8 w-8" />
          </div>
        )}

        {!isLoading && isFetching && (
          <div className="pointer-events-none absolute left-4 top-4 z-20 flex items-center gap-2 rounded-md bg-background/90 px-3 py-1.5 text-xs shadow">
            <Spinner className="h-3.5 w-3.5" />
            Updating...
          </div>
        )}

        {/* Full-list mode: same blocking empty state as before */}
        {!isLoading && !isFetching && !error && !isViewportMode && mappableOutlets.length === 0 && (
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center rounded-xl bg-background/80 backdrop-blur-sm gap-2">
            <MapPin className="h-10 w-10 text-muted-foreground" />
            <p className="text-sm font-medium">No outlets with coordinates found</p>
            <p className="text-xs text-muted-foreground">
              {loadedOutlets.length > 0
                ? `${loadedOutlets.length} outlets exist but all have missing coordinates (0, 0)`
                : 'No active outlets in the database'}
            </p>
          </div>
        )}

        {/* Viewport mode: non-blocking, so the user can still pan/zoom out of an empty area */}
        {!isLoading && !isFetching && !error && isViewportMode && mappableOutlets.length === 0 && (
          <div className="pointer-events-none absolute bottom-6 left-1/2 z-20 flex -translate-x-1/2 items-center gap-2 rounded-md bg-background/90 px-3 py-2 text-xs shadow">
            <MapPin className="h-4 w-4 text-muted-foreground" />
            No outlets with coordinates in this area. Zoom out or pan to see more.
          </div>
        )}

        <APIProvider apiKey={apiKey}>
          <Map
            defaultCenter={CENTER}
            defaultZoom={8}
            gestureHandling="cooperative"
            className="w-full h-full rounded-xl overflow-hidden border"
            onIdle={handleIdle}
          >
            <ClusteredMarkers outlets={mappableOutlets} />
          </Map>
        </APIProvider>

        <Card className="absolute top-4 right-4 w-52 shadow-lg z-10">
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm flex items-center gap-2">
              <MapPin className="h-4 w-4 text-primary" />
              Legend
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4 space-y-1">
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-blue-500 text-white text-[10px] font-bold">N</span>
              Cluster (N outlets)
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <MapPin className="h-4 w-4 text-red-500" />
              Individual outlet
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
