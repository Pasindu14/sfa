import { formatInTimeZone } from 'date-fns-tz'

/**
 * Sri Lanka business timezone — the single reference for all date handling in the web app.
 * The API stores timestamps in UTC and treats business dates as Sri Lanka local (UTC+5:30).
 * Pinning to this zone (instead of the viewer's browser timezone) keeps filters and displays
 * correct regardless of where the admin's browser is, and during Next.js server-side rendering
 * (where the Node process runs in UTC).
 */
export const COLOMBO_TZ = 'Asia/Colombo'

/**
 * Serialize a Date to a `YYYY-MM-DD` string in Sri Lanka time — use for date-range filters
 * sent to the API. Replaces `date.getFullYear()/getMonth()/getDate()`, which used the
 * browser's timezone.
 */
export function toColomboDateStr(date: Date): string {
  return formatInTimeZone(date, COLOMBO_TZ, 'yyyy-MM-dd')
}

/**
 * Format a server date or timestamp (UTC ISO string or Date) for display in Sri Lanka time.
 * Replaces `new Date(x).toLocaleDateString(...)`, which rendered in the browser's timezone.
 * @param value UTC ISO string, Date, or null/undefined
 * @param fmt date-fns format (default `d MMM yyyy`; pass e.g. `d MMM yyyy, HH:mm` for timestamps)
 */
export function formatColombo(
  value: string | Date | null | undefined,
  fmt = 'd MMM yyyy',
): string {
  if (!value) return '—'
  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return '—'
  return formatInTimeZone(date, COLOMBO_TZ, fmt)
}
