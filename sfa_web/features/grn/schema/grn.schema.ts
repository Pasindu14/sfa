import { z } from 'zod'

// ── GRN Item ───────────────────────────────────────────────────────────────

export const grnItemSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productName: z.string(),
  quantity: z.number(),
  unit: z.string(),
  notes: z.string().nullable(),
})

// ── GRN List Item ──────────────────────────────────────────────────────────

export const grnListItemSchema = z.object({
  id: z.number(),
  grnNumber: z.string(),
  salesInvoiceId: z.number(),
  salesInvoiceVchBillNo: z.string(),
  distributorId: z.number(),
  distributorName: z.string(),
  status: z.enum(['Pending', 'Confirmed', 'Disputed']),
  receivedAt: z.string().nullable(),
  confirmedBy: z.number().nullable(),
  confirmedByName: z.string().nullable(),
  confirmedAt: z.string().nullable(),
  notes: z.string().nullable(),
  createdAt: z.string(),
  items: z.array(grnItemSchema),
})

// ── API list response ──────────────────────────────────────────────────────

export const grnListResponseSchema = z.object({
  grns: z.array(grnListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
})

// ── Create GRN input ───────────────────────────────────────────────────────

export const createGrnSchema = z.object({
  salesInvoiceId: z.number().min(1, 'Sales invoice is required'),
})

// ── Confirm GRN input ──────────────────────────────────────────────────────

export const confirmGrnSchema = z.object({
  receivedAt: z.string().min(1, 'Received date is required'),
  notes: z.string().optional(),
})

// ── Inferred types ─────────────────────────────────────────────────────────

export type GrnItem = z.infer<typeof grnItemSchema>
export type GrnListItem = z.infer<typeof grnListItemSchema>
export type GrnListResponse = z.infer<typeof grnListResponseSchema>
export type CreateGrnInput = z.infer<typeof createGrnSchema>
export type ConfirmGrnInput = z.infer<typeof confirmGrnSchema>
export type GrnStatus = GrnListItem['status']
