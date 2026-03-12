'use client'

import { useEffect } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { MarkerClusterer, SuperClusterAlgorithm } from '@googlemaps/markerclusterer'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MapPin } from 'lucide-react'
import { Spinner } from '@/components/ui/spinner'
import { useOutletsForMap } from '@/features/outlet/hooks/outlet.hooks'
import type { OutletMapPointDto } from '@/features/outlet/schema/outlet.schema'

const CENTER = { lat: 7.8731, lng: 80.7718 } // Sri Lanka center

// Must live inside <Map> to access map context
function ClusteredMarkers({ outlets }: { outlets: OutletMapPointDto[] }) {
  const map = useMap()

  useEffect(() => {
    if (!map || outlets.length === 0) return

    // Legacy Marker: canvas-rendered (GPU), not DOM-based — stays smooth at 5000+ points
    const markers = outlets.map(
      (o) =>
        new google.maps.Marker({
          position: { lat: o.latitude, lng: o.longitude },
          title: o.name,
          optimized: true, // batch all markers into a single canvas layer
        })
    )

    const clusterer = new MarkerClusterer({
      map,
      markers,
      algorithm: new SuperClusterAlgorithm({ radius: 60, maxZoom: 16 }),
    })

    return () => {
      clusterer.clearMarkers()
      markers.forEach((m) => m.setMap(null))
    }
  }, [map, outlets])

  return null
}

export function OutletMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''
  const { data: allOutlets = [], isLoading, error } = useOutletsForMap()

  // Filter out 0,0 placeholders — those have no real coordinates yet
  const mappableOutlets = allOutlets.filter(
    (o) => !(o.latitude === 0 && o.longitude === 0)
  )
  const noCoords = allOutlets.length - mappableOutlets.length

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Outlet Map</h1>
          <p className="text-muted-foreground">
            {isLoading
              ? 'Loading outlets...'
              : noCoords > 0
              ? `${mappableOutlets.length} mapped · ${noCoords} missing coordinates`
              : 'Visualise outlet locations across the region'}
          </p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          {isLoading ? 'Loading...' : `${mappableOutlets.length} outlets`}
        </Badge>
      </div>

      <div className="relative" style={{ height: 'calc(100vh - 260px)' }}>
        {isLoading && (
          <div className="absolute inset-0 z-20 flex items-center justify-center rounded-xl bg-background/60 backdrop-blur-sm">
            <Spinner className="h-8 w-8" />
          </div>
        )}

        {!isLoading && !error && mappableOutlets.length === 0 && (
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center rounded-xl bg-background/80 backdrop-blur-sm gap-2">
            <MapPin className="h-10 w-10 text-muted-foreground" />
            <p className="text-sm font-medium">No outlets with coordinates found</p>
            <p className="text-xs text-muted-foreground">
              {allOutlets.length > 0
                ? `${allOutlets.length} outlets exist but all have missing coordinates (0, 0)`
                : 'No active outlets in the database'}
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
