import { formatColombo } from '@/lib/utils/datetime'

/** `businessDate` is a DateOnly (`yyyy-MM-dd`) — format it without a timezone shift. */
export function formatBusinessDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '—'
  const [year, month, day] = dateStr.split('T')[0].split('-').map(Number)
  if (!year || !month || !day) return '—'
  return formatColombo(new Date(Date.UTC(year, month - 1, day, 12)), 'd MMM yyyy')
}

export function formatTimestamp(value: string | null | undefined): string {
  return formatColombo(value, 'd MMM yyyy, HH:mm')
}

export function formatRelativeTime(dateStr: string | null | undefined): string {
  if (!dateStr) return '—'
  const diffMs = Date.now() - new Date(dateStr).getTime()
  const diffMinutes = Math.floor(diffMs / 60_000)
  const diffHours = Math.floor(diffMs / 3_600_000)
  const diffDays = Math.floor(diffMs / 86_400_000)
  if (diffMinutes < 1) return 'Just now'
  if (diffMinutes < 60) return `${diffMinutes}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays}d ago`
  return formatColombo(dateStr, 'd MMM')
}

export function formatLkr(amount: number): string {
  return `LKR ${amount.toLocaleString('en-LK', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`
}
