import { z } from 'zod'

// ── Bin Card Row (one per SKU) ──────────────────────────────────────────────

export const binCardRowSchema = z.object({
  itemCode: z.string(),
  itemDescription: z.string(),
  itemPrice: z.number(),
  openStock: z.number(),
  invoiceQuantity: z.number(),
  marketResaleable: z.number(),
  deletedInv: z.number(),
  stockAdjustment: z.number(),
  soldQty: z.number(),
  freeIssues: z.number(),
  companyFreeIssues: z.number(),
  repReturnQtyDE: z.number(),
  endStock: z.number(),
  currentStock: z.number().nullable(),
  closingStockValue: z.number(),
  stockVariance: z.number().nullable(),
})

// ── Grand totals row ────────────────────────────────────────────────────────

export const binCardTotalsSchema = z.object({
  openStock: z.number(),
  invoiceQuantity: z.number(),
  marketResaleable: z.number(),
  deletedInv: z.number(),
  stockAdjustment: z.number(),
  soldQty: z.number(),
  freeIssues: z.number(),
  companyFreeIssues: z.number(),
  repReturnQtyDE: z.number(),
  endStock: z.number(),
  closingStockValue: z.number(),
})

// ── Full response ───────────────────────────────────────────────────────────

export const binCardResponseSchema = z.object({
  distributorId: z.number(),
  distributorName: z.string(),
  from: z.string(),
  to: z.string(),
  recordCount: z.number(),
  rows: z.array(binCardRowSchema),
  totals: binCardTotalsSchema,
})

// ── Inferred types ──────────────────────────────────────────────────────────

export type BinCardRow = z.infer<typeof binCardRowSchema>
export type BinCardTotals = z.infer<typeof binCardTotalsSchema>
export type BinCardResponse = z.infer<typeof binCardResponseSchema>
