'use client'

import { Tags } from 'lucide-react'

/**
 * Pricing-structure display bits shared by the distributor portal and the staff Rep Bills screen,
 * so the two views of the same bill can't drift. Props are structural on purpose — each feature
 * infers its own zod types.
 */
export interface BillingPricedLine {
  pricingStructureId?: number | null
  pricingStructureName?: string | null
  priceBasis?: 'Pack' | 'Case' | 'Manual' | null
  listUnitPrice?: number | null
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

/**
 * True when the lines were priced from more than one structure. Only then is a per-line chip
 * worth its space — on a single-structure bill the header already says it.
 */
export function usesMultipleStructures(items: BillingPricedLine[]) {
  const ids = new Set<number>()
  for (const item of items) {
    if (item.pricingStructureId != null) ids.add(item.pricingStructureId)
  }
  return ids.size > 1
}

/** "Price list: X" for the dialog header; legacy bills (no structure recorded) show "—". */
export function PriceListLabel({ name }: { name: string | null | undefined }) {
  return (
    <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
      <Tags className="h-3 w-3" />
      Price list: <span className="font-medium text-foreground">{name ?? '—'}</span>
    </span>
  )
}

/** Small per-line chip naming the structure that priced the line. */
export function PricingStructureChip({ name }: { name: string | null | undefined }) {
  if (!name) return null
  return (
    <span
      className="inline-flex max-w-[160px] items-center truncate rounded border bg-muted/60 px-1 py-px text-[10px] leading-none text-muted-foreground"
      title={`Priced from ${name}`}
    >
      {name}
    </span>
  )
}

/**
 * Secondary text under the unit price for a case-priced line. `unitPrice` is the per-pack
 * equivalent; `listUnitPrice` is the exact case price the rep sold at.
 */
export function CaseListPrice({ line }: { line: BillingPricedLine }) {
  if (line.priceBasis !== 'Case' || line.listUnitPrice == null) return null
  return (
    <p className="text-[10px] font-normal text-muted-foreground">
      Case @ {formatCurrency(line.listUnitPrice)}
    </p>
  )
}
