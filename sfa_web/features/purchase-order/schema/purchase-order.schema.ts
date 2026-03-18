import { z } from 'zod'

// ── Enums ──────────────────────────────────────────────────────────────────

export const PurchaseOrderStatus = {
  Draft: 0,
  PendingRepApproval: 1,
  PendingManagerApproval: 2,
  PendingDistributorFinalization: 3,
  Finalized: 4,
  Cancelled: 5,
  PendingDistributorAcknowledgement: 6,
} as const

export type PurchaseOrderStatusValue = (typeof PurchaseOrderStatus)[keyof typeof PurchaseOrderStatus]

export const purchaseOrderStatusLabels: Record<PurchaseOrderStatusValue, string> = {
  0: 'Draft',
  1: 'Pending Rep Approval',
  2: 'Pending Manager Approval',
  3: 'Pending Distributor Finalization',
  4: 'Finalized',
  5: 'Cancelled',
  6: 'Pending Dist. Acknowledgement',
}

// ── Line item schemas ──────────────────────────────────────────────────────

export const purchaseOrderItemSchema = z.object({
  productId: z.number().int().positive('Product is required'),
  quantity: z.number().int().min(1, 'Quantity must be at least 1'),
  unitPrice: z.number().min(0, 'Unit price must be 0 or greater'),
  discount: z.number().min(0).max(100),
})

// ── Create / Update schemas ────────────────────────────────────────────────

export const createPurchaseOrderSchema = z.object({
  distributorId: z.number().int().positive().nullable().optional(),
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional().or(z.literal('')),
  items: z
    .array(purchaseOrderItemSchema)
    .min(1, 'At least one item is required'),
})

export const updatePurchaseOrderSchema = z.object({
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional().or(z.literal('')),
  items: z
    .array(purchaseOrderItemSchema)
    .min(1, 'At least one item is required'),
})

export const rejectPurchaseOrderSchema = z.object({
  reason: z
    .string()
    .min(5, 'Reason must be at least 5 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})

// ── Inferred input types ───────────────────────────────────────────────────

export type CreatePurchaseOrderInput = z.infer<typeof createPurchaseOrderSchema>
export type UpdatePurchaseOrderInput = z.infer<typeof updatePurchaseOrderSchema>
export type RejectPurchaseOrderInput = z.infer<typeof rejectPurchaseOrderSchema>
export type PurchaseOrderItemInput = z.infer<typeof purchaseOrderItemSchema>

// ── DTO types (match API camelCase response) ───────────────────────────────

export type PurchaseOrderItemDto = {
  id: number
  productId: number
  productCode: string
  productDescription: string
  quantity: number
  unitPrice: number
  discount: number
  lineTotal: number
}

export type PurchaseOrderHistoryDto = {
  id: number
  action: string
  fromStatus: PurchaseOrderStatusValue | null
  toStatus: PurchaseOrderStatusValue | null
  performedBy: number
  performedByName: string | null
  performedAt: string
  notes: string | null
}

export type PurchaseOrderDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: PurchaseOrderStatusValue
  statusLabel: string
  notes: string | null
  items: PurchaseOrderItemDto[]
  history: PurchaseOrderHistoryDto[]
  totalAmount: number

  // Audit trail
  submittedBy: number | null
  submittedAt: string | null
  repApprovedBy: number | null
  repApprovedAt: string | null
  managerApprovedBy: number | null
  managerApprovedAt: string | null
  finalizedBy: number | null
  finalizedAt: string | null
  cancelledBy: number | null
  cancelledAt: string | null
  cancelReason: string | null
  acknowledgedBy: number | null
  acknowledgedAt: string | null

  isActive: boolean
  createdAt: string
  updatedAt: string
  createdBy: number | null
  updatedBy: number | null
}

export type PurchaseOrderSummaryDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: PurchaseOrderStatusValue
  statusLabel: string
  totalAmount: number
  itemCount: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  submittedAt: string | null
}

export type PurchaseOrderListDto = {
  purchaseOrders: PurchaseOrderSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
}
