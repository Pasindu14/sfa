import { z } from 'zod'

// ── Enums ────────────────────────────────────────────────────────────────────

export const PurchaseOrderStatus = {
  Draft: 'Draft',
  PendingRepApproval: 'PendingRepApproval',
  PendingManagerApproval: 'PendingManagerApproval',
  PendingDistributorFinalization: 'PendingDistributorFinalization',
  Finalized: 'Finalized',
  Cancelled: 'Cancelled',
  PendingDistributorAcknowledgement: 'PendingDistributorAcknowledgement',
} as const

export type PurchaseOrderStatusValue = (typeof PurchaseOrderStatus)[keyof typeof PurchaseOrderStatus]

export const purchaseOrderStatusLabels: Record<PurchaseOrderStatusValue, string> = {
  Draft: 'Draft',
  PendingRepApproval: 'Pending Rep Approval',
  PendingManagerApproval: 'Pending Manager Approval',
  PendingDistributorFinalization: 'Pending Finalization',
  Finalized: 'Finalized',
  Cancelled: 'Cancelled',
  PendingDistributorAcknowledgement: 'Pending Acknowledgement',
}

// ── Zod schemas ───────────────────────────────────────────────────────────────

export const myPurchaseOrderItemSchema = z.object({
  productId: z.number().int().positive('Product is required'),
  quantity: z.number().int().min(1, 'Quantity must be at least 1'),
  unitPrice: z.number().min(0, 'Unit price must be 0 or greater'),
  discount: z.number().min(0).max(100),
})

export const createMyPurchaseOrderSchema = z.object({
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional().or(z.literal('')),
  items: z.array(myPurchaseOrderItemSchema).min(1, 'At least one item is required'),
})

export const updateMyPurchaseOrderSchema = createMyPurchaseOrderSchema

export const cancelMyPurchaseOrderSchema = z.object({
  reason: z
    .string()
    .min(5, 'Reason must be at least 5 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})

// ── Inferred input types ──────────────────────────────────────────────────────

export type CreateMyPurchaseOrderInput = z.infer<typeof createMyPurchaseOrderSchema>
export type UpdateMyPurchaseOrderInput = z.infer<typeof updateMyPurchaseOrderSchema>
export type CancelMyPurchaseOrderInput = z.infer<typeof cancelMyPurchaseOrderSchema>
export type MyPurchaseOrderItemInput = z.infer<typeof myPurchaseOrderItemSchema>

// ── DTO types (mirror API camelCase response) ─────────────────────────────────

export type MyPurchaseOrderItemDto = {
  id: number
  productId: number
  productCode: string
  productDescription: string
  quantity: number
  unitPrice: number
  discount: number
  lineTotal: number
}

export type SnapshotItem = {
  productId: number
  quantity: number
  unitPrice: number
  discount: number
}

export type MyPurchaseOrderHistoryDto = {
  id: number
  action: string
  fromStatus: PurchaseOrderStatusValue | null
  toStatus: PurchaseOrderStatusValue | null
  performedBy: number
  performedByName: string | null
  performedAt: string
  notes: string | null
  itemsSnapshot: string | null
}

export type MyPurchaseOrderDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: PurchaseOrderStatusValue
  statusLabel: string
  notes: string | null
  items: MyPurchaseOrderItemDto[]
  history: MyPurchaseOrderHistoryDto[]
  totalAmount: number

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

export type MyPurchaseOrderSummaryDto = {
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

export type MyPurchaseOrderListDto = {
  purchaseOrders: MyPurchaseOrderSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export type MyPurchaseOrderStatsDto = {
  pendingRepApproval: number
  pendingManagerApproval: number
  pendingAcknowledgement: number
  finalized: number
  total: number
}

export type MyDistributorProfileDto = {
  id: number
  name: string
  category: string
}
