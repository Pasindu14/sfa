import type {
  RepTimelineEventDto,
  RepTimelineEventKind,
} from '../../schema/rep-route.schema'

/**
 * Shared by the map markers and the timeline list so a bill is the same green in both — the
 * colour is how an admin matches a dot on the map to a row in the list.
 */
export const MARKER_COLORS: Partial<Record<RepTimelineEventKind, string>> = {
  Bill: '#16a34a',
  NoSale: '#64748b',
  Stop: '#f59e0b',
  Unlock: '#7c3aed',
}

/**
 * Minutes between two activities at which the gap stops being "travelling to the next shop"
 * and becomes worth a look.
 */
export const IDLE_HIGHLIGHT_MINUTES = 45

/**
 * NotBilling reason codes from the phone. Unknown codes fall through verbatim so a newer app
 * version sending something new still shows it.
 */
const NO_SALE_REASONS: Record<string, string> = {
  OutletClosed: 'Outlet closed',
  OwnerAbsent: 'Owner absent',
  CreditIssue: 'Credit issue',
  NoOrder: 'No order',
  OutOfStock: 'Out of stock',
}

export function noSaleReasonLabel(code: string | null): string | null {
  if (!code) return null
  return NO_SALE_REASONS[code] ?? code
}

/**
 * Which list row is selected and where the click came from. The source decides the side
 * effect: a list click moves the map, a marker click scrolls the list — never both, or the
 * view you just clicked in would jump out from under you.
 */
export type TimelineSelection = { index: number; source: 'list' | 'map' }

export function hasPosition(
  e: RepTimelineEventDto,
): e is RepTimelineEventDto & { latitude: number; longitude: number } {
  return e.latitude != null && e.longitude != null
}

export function formatMinutes(minutes: number | null | undefined): string {
  if (minutes == null) return '—'
  const m = Math.round(minutes)
  if (m < 60) return `${m} min`
  const h = Math.floor(m / 60)
  const rest = m % 60
  return rest === 0 ? `${h}h` : `${h}h ${rest}m`
}

export function formatLkr(amount: number): string {
  return `LKR ${amount.toLocaleString('en-LK', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`
}

export function eventTitle(e: RepTimelineEventDto): string {
  switch (e.kind) {
    case 'DayStart':
      return 'Day started'
    case 'DayEnd':
      return 'Day ended'
    case 'Bill':
      return e.outletName ?? 'Bill'
    case 'NoSale': {
      const reason = noSaleReasonLabel(e.detail)
      const outlet = e.outletName ?? 'No-sale visit'
      return reason ? `${outlet} · ${reason}` : outlet
    }
    case 'Stop':
      return `Stopped ${formatMinutes(e.minutes)}`
    case 'GpsGap':
      return `No GPS for ${formatMinutes(e.minutes)}`
    case 'Unlock':
      return e.detail ?? 'Route unlock request'
    default:
      return e.kind
  }
}
