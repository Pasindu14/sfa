import { z } from 'zod'

// ── Distributor Stock Item ─────────────────────────────────────────────────

export const distributorStockItemSchema = z.object({
  id: z.number(),
  distributorId: z.number(),
  distributorName: z.string(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  stockType: z.enum(['Normal', 'FreeIssue']),
  quantityOnHand: z.number(),
  // Units per case, from the product master. 0 means no pack size is configured — treat the
  // whole quantity as loose pieces.
  piecesPerPack: z.number(),
  // Null on zero-fill placeholders — an active product with no stock row yet, so nothing has
  // ever moved. Real stock rows always carry a timestamp.
  lastUpdatedAt: z.string().nullable(),
  // Denormalized from the distributor. Nullable: the distributor may have no fleet, and a
  // deactivated fleet resolves to a null name while the id stays set.
  fleetId: z.number().nullable(),
  fleetName: z.string().nullable(),
  // Admin balances only (absent on the distributor portal): current default pricing structure's
  // dealer prices, and quantityOnHand valued at them. Null when the product has no default price.
  dealerPackPrice: z.number().nullable().optional(),
  dealerCasePrice: z.number().nullable().optional(),
  stockValue: z.number().nullable().optional(),
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
