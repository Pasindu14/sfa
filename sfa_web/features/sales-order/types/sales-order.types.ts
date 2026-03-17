export type SalesOrderStatus =
  | 0  // Draft
  | 1  // PendingRepApproval
  | 2  // PendingManagerApproval
  | 3  // PendingDistributorFinalization
  | 4  // Finalized
  | 5  // Cancelled
  | 6  // PendingDistributorAcknowledgement

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
  fromStatus: SalesOrderStatus | null
  toStatus: SalesOrderStatus | null
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
  status: SalesOrderStatus
  statusLabel: string
  notes: string | null
  items: SalesOrderItemDto[]
  totalAmount: number
  history: SalesOrderHistoryDto[]
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
  status: SalesOrderStatus
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

export type PricingStructureItemDto = {
  id: number
  pricingStructureId: number
  productId: number
  productCode: string
  productItemDescription: string
  dealerPackPrice: number | null
  dealerCasePrice: number | null
  promotionalPrice: number | null
}

export type DefaultPricingStructureDto = {
  id: number
  name: string
  description: string | null
  isDefault: boolean
  isActive: boolean
  itemCount: number
  items: PricingStructureItemDto[]
}
