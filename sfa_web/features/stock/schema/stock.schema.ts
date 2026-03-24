import { z } from 'zod'

// ── Distributor Stock Item ─────────────────────────────────────────────────

export const distributorStockItemSchema = z.object({
  id: z.number(),
  distributorId: z.number(),
  distributorName: z.string(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  quantityOnHand: z.number(),
  lastUpdatedAt: z.string(),
})

// ── Stock Transactions (for product drill-down) ────────────────────────────

export const stockTransactionSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  transactionType: z.string(),
  quantity: z.number(),
  unit: z.string(),
  referenceId: z.number().nullable(),
  referenceType: z.string().nullable(),
  notes: z.string().nullable(),
  createdAt: z.string(),
})

export const stockTransactionListResponseSchema = z.object({
  transactions: z.array(stockTransactionSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type DistributorStockItem = z.infer<typeof distributorStockItemSchema>
export type StockTransaction = z.infer<typeof stockTransactionSchema>
export type StockTransactionListResponse = z.infer<typeof stockTransactionListResponseSchema>
