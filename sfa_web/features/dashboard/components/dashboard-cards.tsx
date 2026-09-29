'use client'

import { AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

/** A titled block of the dashboard. `aside` sits on the heading's right (a note or a control). */
export function Section({
  title,
  aside,
  children,
  className,
}: {
  title: string
  aside?: React.ReactNode
  children: React.ReactNode
  className?: string
}) {
  return (
    <section className={className}>
      <div className="mb-3 flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h2 className="text-[15px] font-medium tracking-tight">{title}</h2>
        {aside && <div className="text-xs text-muted-foreground">{aside}</div>}
      </div>
      {children}
    </section>
  )
}

/** The one surface style. Panels differ by what's inside them, not by decoration. */
export function Panel({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn('rounded-2xl border bg-card', className)}>{children}</div>
}

export function SectionError({
  what,
  onRetry,
  className,
}: {
  what: string
  onRetry: () => void
  className?: string
}) {
  return (
    <Panel className={cn('flex flex-col items-center justify-center gap-3 p-6 text-center', className)}>
      <AlertCircle className="h-5 w-5 text-muted-foreground" />
      <p className="text-sm text-muted-foreground">{what} didn&apos;t load. Check your connection and try again.</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        Try again
      </Button>
    </Panel>
  )
}

/** Dims a block while it still shows the previous day's numbers for a newly picked day. */
export function Stale({ stale, children }: { stale: boolean; children: React.ReactNode }) {
  return (
    <div className={cn('transition-opacity duration-200', stale && 'pointer-events-none opacity-55')} aria-busy={stale}>
      {children}
    </div>
  )
}

/**
 * A thin share bar. Pine once the goal is met, neutral stone while short — over a part-finished
 * month nearly everything is short, and a wall of warning colour would alarm without informing.
 */
export function Meter({ percent, className }: { percent: number | null; className?: string }) {
  const p = percent ?? 0
  return (
    <div className={cn('h-1.5 overflow-hidden rounded-full bg-muted', className)} aria-hidden>
      <div
        className={cn(
          'h-full rounded-full',
          p >= 100 ? 'bg-[#2F6B57] dark:bg-[#5FAF93]' : 'bg-[oklch(0.62_0.03_107)]',
        )}
        style={{ width: `${Math.min(Math.max(p, 0), 100)}%`, minWidth: p > 0 ? 3 : 0 }}
      />
    </div>
  )
}
