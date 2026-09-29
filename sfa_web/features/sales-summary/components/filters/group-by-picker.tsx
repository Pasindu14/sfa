'use client'

import { ChevronDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import {
  GROUP_BY_OPTIONS,
  MAX_GROUP_BY,
  type SalesSummaryGroupBy,
} from '../../schema/sales-summary.schema'

const labelOf = (d: SalesSummaryGroupBy) => GROUP_BY_OPTIONS.find((o) => o.value === d)?.label ?? d

/**
 * Multi-dimension "Group by". Ticking adds a dimension at the END, so the order you tick is the
 * column order and the nesting order (Sales Rep → Territory → Distributor). The menu stays open
 * while ticking so several can be picked in one go.
 */
export function GroupByPicker({
  value,
  onToggle,
}: {
  value: SalesSummaryGroupBy[]
  onToggle: (dim: SalesSummaryGroupBy) => void
}) {
  const atMax = value.length >= MAX_GROUP_BY

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className="h-9 w-full justify-between gap-2 px-3 font-normal"
          aria-label={`Group by ${value.map(labelOf).join(', ')}`}
        >
          <span className="flex min-w-0 items-center gap-1 overflow-hidden">
            {value.map((d, i) => (
              <span
                key={d}
                className="inline-flex shrink-0 items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs"
              >
                <span className="tabular-nums text-muted-foreground">{i + 1}</span>
                {labelOf(d)}
              </span>
            ))}
          </span>
          <ChevronDown className="h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="start" className="w-60">
        <DropdownMenuLabel className="text-xs font-normal text-muted-foreground">
          Tick in the order you want the columns — up to {MAX_GROUP_BY}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {GROUP_BY_OPTIONS.map((o) => {
          const pos = value.indexOf(o.value)
          const checked = pos >= 0
          // Keep at least one dimension; stop adding at the API's limit.
          const disabled = checked ? value.length === 1 : atMax
          return (
            <DropdownMenuCheckboxItem
              key={o.value}
              checked={checked}
              disabled={disabled}
              onSelect={(e) => e.preventDefault()}
              onCheckedChange={() => onToggle(o.value)}
            >
              <span className="flex-1">{o.label}</span>
              <span
                className={cn(
                  'ml-2 w-4 text-right text-xs tabular-nums text-muted-foreground',
                  !checked && 'invisible'
                )}
              >
                {pos + 1}
              </span>
            </DropdownMenuCheckboxItem>
          )
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
