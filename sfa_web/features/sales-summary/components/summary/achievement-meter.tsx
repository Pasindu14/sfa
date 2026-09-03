'use client'

import { cn } from '@/lib/utils'

/**
 * A bullet meter: one measure against one target.
 *
 * The whole report answers a single question — did we hit the number — so that comparison gets the
 * only strong visual treatment on the page and everything else stays quiet. A bar plus a target
 * tick reads at a glance in a way a percentage in a column of percentages does not.
 *
 * Colour carries one bit of meaning: at or above target, or not. Shortfall is neutral rather than
 * red on purpose — over a part-finished period almost every row is short, and a wall of red would
 * be alarming without being informative. The brand orange is reserved for things you can click.
 */
export function AchievementMeter({
  percent,
  size = 'row',
  className,
}: {
  /** Null when there is no target — renders as an em dash, not an empty bar. */
  percent: number | null
  size?: 'row' | 'headline'
  className?: string
}) {
  if (percent === null) {
    return (
      <span className={cn('text-muted-foreground/60', className)} aria-label="No target">
        —
      </span>
    )
  }

  const met = percent >= 100
  // The track shows 0–100. Anything beyond fills the bar and is called out by the figure itself,
  // so an outlier cannot squash every other row into invisibility.
  const filled = Math.max(0, Math.min(percent, 100))
  const isHeadline = size === 'headline'

  return (
    <div
      className={cn('flex items-center gap-2', isHeadline && 'gap-4', className)}
      role="meter"
      aria-valuenow={Math.round(percent)}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={`${percent.toFixed(1)}% of target`}
    >
      <div
        className={cn(
          'relative flex-1 overflow-hidden rounded-[2px] bg-[#EDE9E4]',
          isHeadline ? 'h-3' : 'h-1.5 min-w-14'
        )}
      >
        <div
          className={cn(
            'h-full rounded-[2px] transition-[width] duration-500 motion-reduce:transition-none',
            met ? 'bg-[#1E6B54]' : 'bg-[#8A8078]'
          )}
          style={{ width: `${filled}%` }}
        />
        {/* Target tick. Without it a short bar reads as "small number" rather than "missed". */}
        <span
          aria-hidden
          className="absolute inset-y-0 right-0 w-px bg-[#1A1815]/45"
        />
      </div>
      <span
        className={cn(
          'shrink-0 tabular-nums',
          isHeadline ? 'text-2xl font-semibold' : 'w-14 text-right text-xs',
          met ? 'text-[#1E6B54]' : 'text-foreground/75'
        )}
      >
        {percent.toFixed(isHeadline ? 1 : 0)}%
      </span>
    </div>
  )
}
