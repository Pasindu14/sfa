import { z } from 'zod'

// ── Import payload shapes (what we send to the API) ───────────────────────

export const importInvoiceItemSchema = z.object({
  itemErpCode: z.string(),
  itemDescription: z.string(),
  quantity: z.number(),
  unit: z.string(),
  unitPrice: z.number(),
  totalPrice: z.number(),
  isFreeIssue: z.boolean(),
  lineNumber: z.number(),
})

export const importInvoiceSchema = z.object({
  vchBillNo: z.string(),
  busyOrderRequestNo: z.string().nullable().optional(),
  sfaPoNumber: z.string().nullable().optional(),
  distributorAlias: z.number(),
  invoiceDate: z.string(), // ISO date string — DateOnly on API side
  invoiceType: z.enum(['Regular', 'FreeIssue']),
  totalAmount: z.number(),
  items: z.array(importInvoiceItemSchema),
})

export const importSalesInvoicesPayloadSchema = z.object({
  fileName: z.string(),
  invoices: z.array(importInvoiceSchema),
})

export type ImportInvoiceItemPayload = z.infer<typeof importInvoiceItemSchema>
export type ImportInvoicePayload = z.infer<typeof importInvoiceSchema>
export type ImportSalesInvoicesPayload = z.infer<typeof importSalesInvoicesPayloadSchema>

// ── API response ──────────────────────────────────────────────────────────

export const importBatchErrorSchema = z.object({
  vchBillNo: z.string(),
  reason: z.string(),
})

export const importBatchResultSchema = z.object({
  batchId: z.number(),
  batchNumber: z.string(),
  totalInvoices: z.number(),
  importedInvoices: z.number(),
  skippedInvoices: z.number(),
  totalItems: z.number(),
  totalAmount: z.number(),
  status: z.string(),
  errors: z.array(importBatchErrorSchema),
})

export type ImportBatchError = z.infer<typeof importBatchErrorSchema>
export type ImportBatchResult = z.infer<typeof importBatchResultSchema>
