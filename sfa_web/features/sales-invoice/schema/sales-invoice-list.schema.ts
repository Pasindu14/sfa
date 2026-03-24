import { z } from 'zod'

// ── Sales Invoice List Item ────────────────────────────────────────────────

export const salesInvoiceListItemSchema = z.object({
  id: z.number(),
  vchBillNo: z.string(),
  busyOrderRequestNo: z.string().nullable(),
  sfaPoNumber: z.string().nullable(),
  distributorId: z.number(),
  distributorName: z.string(),
  invoiceDate: z.string(),
  invoiceType: z.enum(['Regular', 'FreeIssue']),
  totalAmount: z.number(),
  status: z.enum(['Pending', 'GrnReceived', 'Disputed']),
  batchNumber: z.string(),
  createdAt: z.string(),
})

// ── Sales Invoice Detail Item ──────────────────────────────────────────────

export const salesInvoiceItemSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productCode: z.string(),
  itemErpCode: z.string(),
  itemDescription: z.string(),
  quantity: z.number(),
  unit: z.string(),
  unitPrice: z.number(),
  totalPrice: z.number(),
  isFreeIssue: z.boolean(),
  lineNumber: z.number(),
})

export const salesInvoiceDetailSchema = salesInvoiceListItemSchema.extend({
  purchaseOrderId: z.number().nullable(),
  importBatchId: z.number(),
  items: z.array(salesInvoiceItemSchema),
})

// ── API response wrapper ───────────────────────────────────────────────────

export const salesInvoiceListResponseSchema = z.object({
  invoices: z.array(salesInvoiceListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type SalesInvoiceListItem = z.infer<typeof salesInvoiceListItemSchema>
export type SalesInvoiceItem = z.infer<typeof salesInvoiceItemSchema>
export type SalesInvoiceDetail = z.infer<typeof salesInvoiceDetailSchema>
export type SalesInvoiceListResponse = z.infer<typeof salesInvoiceListResponseSchema>
export type SalesInvoiceStatus = SalesInvoiceListItem['status']
export type SalesInvoiceType = SalesInvoiceListItem['invoiceType']
