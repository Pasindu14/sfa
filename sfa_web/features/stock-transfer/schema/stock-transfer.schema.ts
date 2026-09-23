import { z } from 'zod'

// ── Create request ─────────────────────────────────────────────────────────

export const stockTransferLineInputSchema = z.object({
  productId: z.number().int().positive(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  // Pieces — the UI enters CS + PCS and converts before sending.
  quantity: z.number().int().positive(),
})

export const createStockTransferSchema = z
  .object({
    sourceDistributorId: z.number().int().positive(),
    targetDistributorId: z.number().int().positive(),
    notes: z.string().trim().max(500).optional(),
    lines: z.array(stockTransferLineInputSchema).min(1, 'At least one line must have a quantity'),
  })
  .refine((d) => d.sourceDistributorId !== d.targetDistributorId, {
    message: 'Target distributor must differ from the source',
    path: ['targetDistributorId'],
  })

// ── Responses ──────────────────────────────────────────────────────────────

export const stockTransferSummarySchema = z.object({
  id: z.number(),
  transferNumber: z.string(),
  sourceDistributorId: z.number(),
  sourceDistributorName: z.string(),
  targetDistributorId: z.number(),
  targetDistributorName: z.string(),
  notes: z.string().nullable(),
  transferredByName: z.string().nullable(),
  transferredAt: z.string(),
  lineCount: z.number(),
  totalQuantity: z.number(),
})

export const stockTransferLineSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  quantity: z.number(),
  piecesPerPack: z.number(),
})

export const stockTransferSchema = stockTransferSummarySchema.extend({
  lines: z.array(stockTransferLineSchema),
})

export const stockTransferListResponseSchema = z.object({
  transfers: z.array(stockTransferSummarySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type StockTransferLineInput = z.infer<typeof stockTransferLineInputSchema>
export type CreateStockTransferInput = z.infer<typeof createStockTransferSchema>
export type StockTransferSummary = z.infer<typeof stockTransferSummarySchema>
export type StockTransferLine = z.infer<typeof stockTransferLineSchema>
export type StockTransfer = z.infer<typeof stockTransferSchema>
export type StockTransferListResponse = z.infer<typeof stockTransferListResponseSchema>
