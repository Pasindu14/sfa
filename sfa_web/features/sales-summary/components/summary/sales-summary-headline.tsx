'use client'

import type { SalesSummaryResponse } from '../../schema/sales-summary.schema'
import { AchievementMeter } from './achievement-meter'

const money = (v: number) =>
  v.toLocaleString('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const qty = (v: number) =>
  v.toLocaleString('en-LK', { maximumFractionDigits: 0 })

/**
 * The result, not a banner.
 *
 * This replaced a large empty title block. The page exists to answer "did we hit the number", so
 * the answer itself opens the page: net sales against target, with the meter carrying the
 * comparison. Everything below is the detail behind it.
 */
export function SalesSummaryHeadline({ data }: { data: SalesSummaryResponse }) {
  const t = data.totals

  return (
    <section className="rounded-lg border bg-card font-report">
      <div className="flex flex-col gap-6 px-6 py-5 sm:flex-row sm:items-end sm:justify-between">
        <Figure label="Net sales" value={money(t.netSaleValue)} tone="strong" />
        <Figure label="Sold" value={`${qty(t.saleQty)} packs`} />
        <Figure
          label="Target"
          value={t.targetValue === null ? '—' : money(t.targetValue)}
          align="right"
        />
      </div>

      <div className="border-t px-6 py-4">
        {t.achievementPercent === null ? (
          <p className="text-sm text-muted-foreground">
            {data.targetsAvailable
              ? 'No target was imported for this selection, so achievement cannot be measured.'
              : data.targetsUnavailableReason}
          </p>
        ) : (
          <AchievementMeter percent={t.achievementPercent} size="headline" />
        )}
      </div>
    </section>
  )
}

function Figure({
  label,
  value,
  tone = 'normal',
  align = 'left',
}: {
  label: string
  value: string
  tone?: 'normal' | 'strong'
  align?: 'left' | 'right'
}) {
  return (
    <div className={align === 'right' ? 'sm:text-right' : undefined}>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p
        className={
          tone === 'strong'
            ? 'mt-1 text-3xl font-semibold tabular-nums tracking-tight sm:text-4xl'
            : 'mt-1 text-xl font-medium tabular-nums text-foreground/80'
        }
      >
        {value}
      </p>
    </div>
  )
}
