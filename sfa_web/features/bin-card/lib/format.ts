const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** On-screen number: thousands separators, up to 2 decimals (always 2 for money). */
export function formatNumber(value: number, money = false): string {
  // Avoid "-0" / "-0.00" when a decimal total nets out to (almost) nothing.
  const v = Math.abs(value) < 0.005 ? 0 : value
  return v.toLocaleString('en-US', {
    minimumFractionDigits: money ? 2 : 0,
    maximumFractionDigits: 2,
  })
}

/** "2026-10-01" -> "1 Oct 2026". Parsed by hand so the calendar day never shifts with the time zone. */
export function formatDay(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return `${d} ${MONTHS[m - 1]} ${y}`
}

export function formatRange(from: string, to: string): string {
  return from === to ? formatDay(from) : `${formatDay(from)} to ${formatDay(to)}`
}
