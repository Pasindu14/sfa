'use client'

import { useEffect } from 'react'
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps'
import { MarkerClusterer, SuperClusterAlgorithm } from '@googlemaps/markerclusterer'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MapPin } from 'lucide-react'
import { OUTLET_LOCATIONS } from '@/features/outlet/data/outlet-locations'

const CENTER = { lat: 6.03, lng: 80.31 }

// Must live inside <Map> to access map context
function ClusteredMarkers() {
  const map = useMap()

  useEffect(() => {
    if (!map) return

    // Legacy Marker: canvas-rendered (GPU), not DOM-based — stays smooth at 600+ points
    const markers = OUTLET_LOCATIONS.map(
      ({ lat, lng }) =>
        new google.maps.Marker({
          position: { lat, lng },
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
  }, [map])

  return null
}

export function OutletMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Outlet Map</h1>
          <p className="text-muted-foreground">Visualise outlet locations across the region</p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          {OUTLET_LOCATIONS.length} outlets
        </Badge>
      </div>

      <div className="relative" style={{ height: 'calc(100vh - 260px)' }}>
        <APIProvider apiKey={apiKey}>
          <Map
            defaultCenter={CENTER}
            defaultZoom={12}
            gestureHandling="cooperative"
            className="w-full h-full rounded-xl overflow-hidden border"
          >
            <ClusteredMarkers />
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
