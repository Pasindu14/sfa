import { z } from 'zod'

// ── Enums ──────────────────────────────────────────────────────────────────

export const SalesOrderStatus = {
  Draft: 0,
  PendingRepApproval: 1,
  PendingManagerApproval: 2,
  PendingDistributorFinalization: 3,
  Finalized: 4,
  Cancelled: 5,
  PendingDistributorAcknowledgement: 6,
} as const

export type SalesOrderStatusValue = (typeof SalesOrderStatus)[keyof typeof SalesOrderStatus]

export const salesOrderStatusLabels: Record<SalesOrderStatusValue, string> = {
  0: 'Draft',
  1: 'Pending Rep Approval',
  2: 'Pending Manager Approval',
  3: 'Pending Distributor Finalization',
  4: 'Finalized',
  5: 'Cancelled',
  6: 'Pending Dist. Acknowledgement',
}

// ── Line item schemas ──────────────────────────────────────────────────────

export const salesOrderItemSchema = z.object({
  productId: z.number().int().positive('Product is required'),
  quantity: z.number().int().min(1, 'Quantity must be at least 1'),
  unitPrice: z.number().min(0, 'Unit price must be 0 or greater'),
  discount: z.number().min(0).max(100),
})

// ── Create / Update schemas ────────────────────────────────────────────────

export const createSalesOrderSchema = z.object({
  distributorId: z.number().int().positive().nullable().optional(),
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional().or(z.literal('')),
  items: z
    .array(salesOrderItemSchema)
    .min(1, 'At least one item is required'),
})

export const updateSalesOrderSchema = z.object({
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional().or(z.literal('')),
  items: z
    .array(salesOrderItemSchema)
    .min(1, 'At least one item is required'),
})

export const rejectSalesOrderSchema = z.object({
  reason: z
    .string()
    .min(5, 'Reason must be at least 5 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})

// ── Inferred input types ───────────────────────────────────────────────────

export type CreateSalesOrderInput = z.infer<typeof createSalesOrderSchema>
export type UpdateSalesOrderInput = z.infer<typeof updateSalesOrderSchema>
export type RejectSalesOrderInput = z.infer<typeof rejectSalesOrderSchema>
export type SalesOrderItemInput = z.infer<typeof salesOrderItemSchema>

// ── DTO types (match API camelCase response) ───────────────────────────────

export type SalesOrderItemDto = {
  id: number
  productId: number
  productCode: string
  productDescription: string
  quantity: number
  unitPrice: number
  discount: number
  lineTotal: number
}

export type SalesOrderHistoryDto = {
  id: number
  action: string
  fromStatus: SalesOrderStatusValue | null
  toStatus: SalesOrderStatusValue | null
  performedBy: number
  performedByName: string | null
  performedAt: string
  notes: string | null
}

export type SalesOrderDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: SalesOrderStatusValue
  statusLabel: string
  notes: string | null
  items: SalesOrderItemDto[]
  history: SalesOrderHistoryDto[]
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

export type SalesOrderSummaryDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: SalesOrderStatusValue
  statusLabel: string
  totalAmount: number
  itemCount: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  submittedAt: string | null
}

export type SalesOrderListDto = {
  salesOrders: SalesOrderSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
}
