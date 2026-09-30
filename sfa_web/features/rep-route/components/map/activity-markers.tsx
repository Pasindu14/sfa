'use client'

import { useEffect, useRef } from 'react'
import { useMap } from '@vis.gl/react-google-maps'
import type { RepTimelineEventDto } from '../../schema/rep-route.schema'
import { formatColombo } from '@/lib/utils/datetime'
import {
  MARKER_COLORS,
  eventTitle,
  hasPosition,
  type TimelineSelection,
} from '../timeline/timeline-display'

const MARKER_SCALE = 7
const SELECTED_SCALE = 11
const FOCUS_ZOOM = 17

function markerIcon(color: string, selected: boolean): google.maps.Symbol {
  return {
    path: google.maps.SymbolPath.CIRCLE,
    scale: selected ? SELECTED_SCALE : MARKER_SCALE,
    fillColor: color,
    fillOpacity: 1,
    strokeColor: selected ? '#0f172a' : '#ffffff',
    strokeWeight: selected ? 3 : 2,
  }
}

/**
 * Bills, no-sale visits, unrecorded stops and unlock requests as coloured dots over the trail.
 * Must live inside <Map>, like RouteTrail.
 *
 * Markers are built once per `events` array and restyled in place on selection — rebuilding
 * every overlay on each click would flicker and drop the map's own hover state.
 */
export function ActivityMarkers({
  events,
  selection,
  onSelect,
  fitToEvents,
}: {
  events: RepTimelineEventDto[]
  selection: TimelineSelection | null
  onSelect: (index: number) => void
  /** Frame the markers when there is no trail to frame instead (RouteTrail owns fitBounds otherwise). */
  fitToEvents: boolean
}) {
  const map = useMap()
  const markersRef = useRef(new Map<number, google.maps.Marker>())

  // Read through a ref so a new callback identity doesn't tear down every marker.
  const onSelectRef = useRef(onSelect)
  useEffect(() => {
    onSelectRef.current = onSelect
  }, [onSelect])

  useEffect(() => {
    if (!map) return
    const markers = markersRef.current
    const bounds = new google.maps.LatLngBounds()

    events.forEach((e, index) => {
      const color = MARKER_COLORS[e.kind]
      if (!color || !hasPosition(e)) return

      const position = { lat: e.latitude, lng: e.longitude }
      bounds.extend(position)

      const marker = new google.maps.Marker({
        position,
        map,
        title: `${formatColombo(e.at, 'HH:mm')} — ${eventTitle(e)}`,
        icon: markerIcon(color, false),
        zIndex: 4,
      })
      marker.addListener('click', () => onSelectRef.current(index))
      markers.set(index, marker)
    })

    if (fitToEvents && !bounds.isEmpty()) map.fitBounds(bounds, 64)

    return () => {
      markers.forEach((m) => {
        google.maps.event.clearInstanceListeners(m)
        m.setMap(null)
      })
      markers.clear()
    }
  }, [map, events, fitToEvents])

  useEffect(() => {
    if (!map) return

    markersRef.current.forEach((marker, index) => {
      const kind = events[index]?.kind
      const color = kind ? MARKER_COLORS[kind] : undefined
      if (!color) return
      const selected = selection?.index === index
      marker.setIcon(markerIcon(color, selected))
      marker.setZIndex(selected ? 10 : 4)
    })

    // Only a click in the list moves the map; a marker click already has the map where the
    // admin is looking.
    if (selection?.source !== 'list') return
    const e = events[selection.index]
    if (!e || !hasPosition(e)) return
    map.panTo({ lat: e.latitude, lng: e.longitude })
    map.setZoom(FOCUS_ZOOM)
  }, [map, events, selection])

  return null
}
