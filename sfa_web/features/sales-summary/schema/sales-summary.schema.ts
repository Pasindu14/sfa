import { z } from 'zod'

// ── Grouping dimension ──────────────────────────────────────────────────────
// Must stay in step with the API's SalesSummaryGroupBy enum. Route and Outlet
// have no counterpart on SalesTarget, so the API returns null target columns
// for them and says why in `targetsUnavailableReason`.

export const SALES_SUMMARY_GROUP_BY = [
  'SalesRep',
  'Supervisor',
  'Asm',
  'Rsm',
  'Nsm',
  'Distributor',
  'Outlet',
  'Route',
  'Division',
  'Territory',
  'Area',
  'Region',
  'Product',
] as const

export const salesSummaryGroupBySchema = z.enum(SALES_SUMMARY_GROUP_BY)

/** Labels for the Group by dropdown, in the order a manager is most likely to want them. */
export const GROUP_BY_OPTIONS: { value: SalesSummaryGroupBy; label: string }[] = [
  { value: 'SalesRep', label: 'Sales Rep' },
  { value: 'Product', label: 'Product' },
  { value: 'Territory', label: 'Territory' },
  { value: 'Division', label: 'Division' },
  { value: 'Area', label: 'Area' },
  { value: 'Region', label: 'Region' },
  { value: 'Route', label: 'Route' },
  { value: 'Outlet', label: 'Outlet' },
  { value: 'Distributor', label: 'Distributor' },
  { value: 'Supervisor', label: 'Supervisor' },
  { value: 'Asm', label: 'ASM' },
  { value: 'Rsm', label: 'RSM' },
  { value: 'Nsm', label: 'NSM' },
]

// ── One report row ──────────────────────────────────────────────────────────
// Target columns are nullable on purpose: null means "not measurable for this
// grouping", which is not the same statement as a target of zero.

export const salesSummaryRowSchema = z.object({
  groupKey: z.number().nullable(),
  groupCode: z.string(),
  groupName: z.string(),
  targetValue: z.number().nullable(),
  targetQty: z.number().nullable(),
  grossSaleValue: z.number(),
  saleQty: z.number(),
  goodReturn: z.number(),
  goodReturnQty: z.number(),
  marketReturn: z.number(),
  marketReturnQty: z.number(),
  dbDiscount: z.number(),
  discount: z.number(),
  netSaleValue: z.number(),
  netSaleQty: z.number(),
  achievementPercent: z.number().nullable(),
})

export const salesSummaryTotalsSchema = salesSummaryRowSchema.omit({
  groupKey: true,
  groupCode: true,
  groupName: true,
})

// ── Full response ───────────────────────────────────────────────────────────

export const salesSummaryResponseSchema = z.object({
  groupBy: salesSummaryGroupBySchema,
  // Echoed back for the export title line only — there is no Date Range column.
  from: z.string(),
  to: z.string(),
  targetsAvailable: z.boolean(),
  targetsUnavailableReason: z.string().nullable(),
  groupCount: z.number(),
  rows: z.array(salesSummaryRowSchema),
  totals: salesSummaryTotalsSchema,
})

// ── Inferred types ──────────────────────────────────────────────────────────

export type SalesSummaryGroupBy = (typeof SALES_SUMMARY_GROUP_BY)[number]
export type SalesSummaryRow = z.infer<typeof salesSummaryRowSchema>
export type SalesSummaryTotals = z.infer<typeof salesSummaryTotalsSchema>
export type SalesSummaryResponse = z.infer<typeof salesSummaryResponseSchema>

/** Every optional narrowing filter the report accepts. All are AND-ed server-side. */
export interface SalesSummaryFilterIds {
  regionId: number | null
  areaId: number | null
  territoryId: number | null
  divisionId: number | null
  routeId: number | null
  distributorId: number | null
  salesRepId: number | null
  supervisorId: number | null
  productId: number | null
}

/** A user as returned by the supervisor/rep pickers. */
export interface UserOption {
  id: number
  name: string
  username?: string
  isActive?: boolean
}
