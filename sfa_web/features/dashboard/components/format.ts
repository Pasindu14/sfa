const LKR = new Intl.NumberFormat('en-LK', {
  style: 'currency',
  currency: 'LKR',
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
})

const COUNT = new Intl.NumberFormat('en-LK')

/** Exact amount, e.g. "LKR 1,234,567". */
export function money(amount: number | null | undefined): string {
  return amount === null || amount === undefined ? '—' : LKR.format(amount)
}

/** Compact amount for tiles, e.g. "LKR 1.25M", "LKR 850K", "LKR 4.4K". */
export function moneyShort(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) return '—'
  const abs = Math.abs(amount)
  const sign = amount < 0 ? '-' : ''
  if (abs >= 1_000_000) return `${sign}LKR ${(abs / 1_000_000).toFixed(abs >= 10_000_000 ? 1 : 2)}M`
  // One decimal below 100K — "4K" for 4,388 hides too much on a small day.
  if (abs >= 1_000) return `${sign}LKR ${(abs / 1_000).toFixed(abs >= 100_000 ? 0 : 1)}K`
  return LKR.format(amount)
}

export function count(n: number): string {
  return COUNT.format(n)
}

export function percent(p: number | null | undefined, digits = 1): string {
  return p === null || p === undefined ? '—' : `${p.toFixed(digits)}%`
}
