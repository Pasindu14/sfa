'use client'

import { APIProvider, Map } from '@vis.gl/react-google-maps'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MapPin } from 'lucide-react'

interface OutletMarker {
  id: string
  name: string
  lat: number
  lng: number
}

const outlets: OutletMarker[] = []

const SRI_LANKA_CENTER = { lat: 7.8731, lng: 80.7718 }

export function OutletMapPage() {
  const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? ''

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Outlet Map</h1>
          <p className="text-muted-foreground">Visualise outlet locations across the country</p>
        </div>
        <Badge variant="secondary" className="text-sm px-3 py-1">
          {outlets.length} outlet{outlets.length !== 1 ? 's' : ''}
        </Badge>
      </div>

      <div className="relative" style={{ height: 'calc(100vh - 260px)' }}>
        <APIProvider apiKey={apiKey}>
          <Map
            mapId="outlet-map"
            defaultCenter={SRI_LANKA_CENTER}
            defaultZoom={8}
            gestureHandling="cooperative"
            disableDefaultUI={false}
            className="w-full h-full rounded-xl overflow-hidden border"
          />
        </APIProvider>

        {/* Legend overlay */}
        <Card className="absolute top-4 right-4 w-48 shadow-lg z-10">
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm flex items-center gap-2">
              <MapPin className="h-4 w-4 text-primary" />
              Legend
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <p className="text-xs text-muted-foreground">
              Outlet markers will appear here once data is connected.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
