import { z } from 'zod'

// ── Enums + labels ─────────────────────────────────────────────────────────

export const stockTransactionTypeSchema = z.enum([
  'GRNReceipt',
  'Sale',
  'FreeIssue',
  'Return',
  'Damage',
  'Opening',
  'BillingReversal',
  'StockTakingAdjustment',
  'Correction',
  'TransferOut',
  'TransferIn',
])

export const stockDirectionSchema = z.enum(['In', 'Out'])

export const STOCK_TRANSACTION_TYPES: { value: StockTransactionType; label: string }[] = [
  { value: 'GRNReceipt', label: 'GRN Receipt' },
  { value: 'Sale', label: 'Sale' },
  { value: 'FreeIssue', label: 'Free Issue' },
  { value: 'Return', label: 'Return' },
  { value: 'Damage', label: 'Damage' },
  { value: 'Opening', label: 'Opening' },
  { value: 'BillingReversal', label: 'Billing Reversal' },
  { value: 'StockTakingAdjustment', label: 'Stock Taking Adjustment' },
  // Correction rows are written by the admin Stock Adjustment feature.
  { value: 'Correction', label: 'Stock Adjustment' },
  { value: 'TransferOut', label: 'Transfer Out' },
  { value: 'TransferIn', label: 'Transfer In' },
]

export function transactionTypeLabel(type: string): string {
  return STOCK_TRANSACTION_TYPES.find((t) => t.value === type)?.label ?? type
}

/** Max inclusive span of the from/to range — mirrors the API's limit. */
export const MAX_RANGE_DAYS = 93

// ── Activity row ───────────────────────────────────────────────────────────

export const stockActivitySchema = z.object({
  id: z.number(),
  transactedAt: z.string(),
  transactedById: z.number().nullable(),
  transactedByName: z.string().nullable(),
  distributorId: z.number(),
  distributorName: z.string(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  piecesPerPack: z.number(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  transactionType: z.string(),
  direction: stockDirectionSchema,
  quantity: z.number(),
  quantityBefore: z.number(),
  quantityAfter: z.number(),
  referenceType: z.string().nullable(),
  referenceId: z.number().nullable(),
  referenceNumber: z.string().nullable(),
  notes: z.string().nullable(),
})

export const stockActivityUserSchema = z.object({
  id: z.number(),
  name: z.string(),
})

// ── Query filters ──────────────────────────────────────────────────────────

export const stockActivityFiltersSchema = z.object({
  from: z.string(),
  to: z.string(),
  distributorId: z.number().nullable(),
  productId: z.number().nullable(),
  userId: z.number().nullable(),
  transactionType: stockTransactionTypeSchema.nullable(),
  direction: stockDirectionSchema.nullable(),
})

export const stockActivityListResponseSchema = z.object({
  items: z.array(stockActivitySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type StockTransactionType = z.infer<typeof stockTransactionTypeSchema>
export type StockDirection = z.infer<typeof stockDirectionSchema>
export type StockActivity = z.infer<typeof stockActivitySchema>
export type StockActivityUser = z.infer<typeof stockActivityUserSchema>
export type StockActivityFilters = z.infer<typeof stockActivityFiltersSchema>
export type StockActivityListResponse = z.infer<typeof stockActivityListResponseSchema>
