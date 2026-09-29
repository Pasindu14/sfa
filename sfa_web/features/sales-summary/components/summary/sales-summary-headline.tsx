'use client'

import { BadgePercent, Layers, Package, Target, TrendingUp, Undo2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SalesSummaryResponse } from '../../schema/sales-summary.schema'
import { AchievementMeter } from './achievement-meter'

const money = (v: number) =>
  v.toLocaleString('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const qty = (v: number) => v.toLocaleString('en-LK', { maximumFractionDigits: 0 })

/**
 * The result, before the working: net sales and the target up top, then the four figures that
 * explain how gross became net. Every figure is a grand total over the whole report, never a page.
 */
export function SalesSummaryHeadline({ data }: { data: SalesSummaryResponse }) {
  const t = data.totals
  const returns = t.goodReturn + t.marketReturn
  const discounts = t.dbDiscount + t.discount

  return (
    <section className="grid gap-3 font-report sm:grid-cols-2 lg:grid-cols-4">
      <Tile
        className="sm:col-span-2"
        icon={TrendingUp}
        label="Net sales"
        value={money(t.netSaleValue)}
        size="hero"
        caption={`${qty(t.netSaleQty)} packs net of good returns · ${qty(t.saleQty)} sold`}
      />

      <Tile
        className="sm:col-span-2"
        icon={Target}
        label="Target"
        value={t.targetValue === null ? '—' : money(t.targetValue)}
        size="hero"
      >
        {t.achievementPercent === null ? (
          <p className="text-xs text-muted-foreground">
            {data.targetsAvailable
              ? 'No target was imported for this selection, so achievement cannot be measured.'
              : data.targetsUnavailableReason}
          </p>
        ) : (
          <AchievementMeter percent={t.achievementPercent} size="headline" />
        )}
      </Tile>

      <Tile
        icon={Package}
        label="Gross sales"
        value={money(t.grossSaleValue)}
        caption={`${qty(t.saleQty)} packs · after good returns`}
      />
      <Tile
        icon={Undo2}
        label="Returns"
        value={money(returns)}
        caption={`Good ${money(t.goodReturn)} · Market ${money(t.marketReturn)}`}
      />
      <Tile
        icon={BadgePercent}
        label="Discounts"
        value={money(discounts)}
        caption={`DB ${money(t.dbDiscount)} · Item ${money(t.discount)}`}
      />
      <Tile
        icon={Layers}
        label="Rows"
        value={data.groupCount.toLocaleString('en-LK')}
        caption="Combinations in this report"
      />
    </section>
  )
}

function Tile({
  icon: Icon,
  label,
  value,
  caption,
  size = 'normal',
  className,
  children,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  value: string
  caption?: string
  size?: 'normal' | 'hero'
  className?: string
  children?: React.ReactNode
}) {
  return (
    <div className={cn('flex flex-col gap-2 rounded-lg border bg-card p-5', className)}>
      <div className="flex items-center gap-2 text-muted-foreground">
        <span className="flex h-7 w-7 items-center justify-center rounded-md bg-muted">
          <Icon className="h-3.5 w-3.5" />
        </span>
        <span className="text-xs font-medium">{label}</span>
      </div>
      <p
        className={cn(
          'tabular-nums tracking-tight',
          size === 'hero' ? 'text-3xl font-semibold sm:text-4xl' : 'text-2xl font-semibold'
        )}
      >
        {value}
      </p>
      {caption && <p className="text-xs text-muted-foreground">{caption}</p>}
      {children}
    </div>
  )
}
