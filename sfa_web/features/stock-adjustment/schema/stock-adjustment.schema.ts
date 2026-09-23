import { z } from 'zod'

// ── Reasons ────────────────────────────────────────────────────────────────

export const stockAdjustmentReasonSchema = z.enum([
  'Damage',
  'Expiry',
  'CountCorrection',
  'DataEntryError',
  'Other',
])

export const STOCK_ADJUSTMENT_REASONS: { value: StockAdjustmentReason; label: string }[] = [
  { value: 'Damage', label: 'Damage' },
  { value: 'Expiry', label: 'Expiry' },
  { value: 'CountCorrection', label: 'Count Correction' },
  { value: 'DataEntryError', label: 'Data Entry Error' },
  { value: 'Other', label: 'Other' },
]

export function reasonLabel(reason: string): string {
  return STOCK_ADJUSTMENT_REASONS.find((r) => r.value === reason)?.label ?? reason
}

// ── Create request ─────────────────────────────────────────────────────────

export const stockAdjustmentLineInputSchema = z.object({
  productId: z.number().int().positive(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  // Balance the admin saw on screen (0 for added products) — the API rejects the adjustment
  // with STOCK_CHANGED if the live balance has moved since.
  expectedQuantity: z.number().int(),
  // Pieces — the UI enters CS + PCS and converts before sending.
  newQuantity: z.number().int().min(0),
})

export const createStockAdjustmentSchema = z
  .object({
    distributorId: z.number().int().positive(),
    reason: stockAdjustmentReasonSchema,
    notes: z.string().trim().max(500).optional(),
    lines: z.array(stockAdjustmentLineInputSchema).min(1, 'Change at least one line'),
  })
  .refine((d) => d.reason !== 'Other' || !!d.notes, {
    message: 'Notes are required when the reason is Other',
    path: ['notes'],
  })

// ── Responses ──────────────────────────────────────────────────────────────

export const stockAdjustmentSummarySchema = z.object({
  id: z.number(),
  adjustmentNumber: z.string(),
  distributorId: z.number(),
  distributorName: z.string(),
  reason: z.string(),
  notes: z.string().nullable(),
  adjustedByName: z.string().nullable(),
  adjustedAt: z.string(),
  lineCount: z.number(),
  totalIncrease: z.number(),
  totalDecrease: z.number(),
})

export const stockAdjustmentLineSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  quantityBefore: z.number(),
  newQuantity: z.number(),
  difference: z.number(),
  piecesPerPack: z.number(),
})

export const stockAdjustmentSchema = stockAdjustmentSummarySchema.extend({
  lines: z.array(stockAdjustmentLineSchema),
})

export const stockAdjustmentListResponseSchema = z.object({
  adjustments: z.array(stockAdjustmentSummarySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type StockAdjustmentReason = z.infer<typeof stockAdjustmentReasonSchema>
export type StockAdjustmentLineInput = z.infer<typeof stockAdjustmentLineInputSchema>
export type CreateStockAdjustmentInput = z.infer<typeof createStockAdjustmentSchema>
export type StockAdjustmentSummary = z.infer<typeof stockAdjustmentSummarySchema>
export type StockAdjustmentLine = z.infer<typeof stockAdjustmentLineSchema>
export type StockAdjustment = z.infer<typeof stockAdjustmentSchema>
export type StockAdjustmentListResponse = z.infer<typeof stockAdjustmentListResponseSchema>
