import { z } from 'zod'

export const distributorBillingListItemSchema = z.object({
  id: z.number(),
  billingNumber: z.string(),
  billingDate: z.string(),
  outletId: z.number(),
  outletName: z.string(),
  salesRepId: z.number(),
  salesRepName: z.string(),
  supervisorName: z.string().nullable(),
  distributorId: z.number(),
  distributorName: z.string(),
  totalAmount: z.number(),
  repStatus: z.enum(['Submitted', 'Cancelled']),
  distributorStatus: z.enum(['Pending', 'Approved', 'Rejected']),
  paymentType: z.enum(['Cash', 'Credit']),
  isCashCollected: z.boolean(),
  createdAt: z.string(),
  isAdjusted: z.boolean().default(false),
})

export const billingItemSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  discountRate: z.number(),
  discountAmount: z.number(),
  totalPrice: z.number(),
  billingItemType: z.enum(['Sale', 'Return', 'FreeIssue']),
  // 'DistributorReturn' is a quantity the distributor struck off during review — a strict enum
  // without it throws on every adjusted bill.
  returnType: z.enum(['MarketResell', 'Damage', 'Expire', 'DistributorReturn']).nullable(),
  freeIssueSource: z.enum(['Company', 'Distributor']).nullable(),
  lineNumber: z.number(),
  source: z.enum(['SalesRep', 'DistributorReturn']).default('SalesRep'),
  sourceBillingItemId: z.number().nullable().default(null),
  originalQuantity: z.number().nullable().default(null),
})

export const billingAdjustmentLineSchema = z.object({
  billingItemId: z.number(),
  productId: z.number(),
  productCode: z.string(),
  productDescription: z.string(),
  oldQuantity: z.number(),
  newQuantity: z.number(),
  oldTotalPrice: z.number(),
  newTotalPrice: z.number(),
  returnedQuantity: z.number(),
  returnValue: z.number(),
})

export const billingAdjustmentSchema = z.object({
  id: z.number(),
  adjustedByUserId: z.number(),
  adjustedByName: z.string(),
  adjustedAt: z.string(),
  note: z.string().nullable(),
  oldTotalAmount: z.number(),
  newTotalAmount: z.number(),
  lines: z.array(billingAdjustmentLineSchema),
})

export const distributorBillingDetailSchema = distributorBillingListItemSchema.extend({
  subTotalAmount: z.number(),
  billDiscountRate: z.number(),
  billDiscountAmount: z.number(),
  returnValue: z.number(),
  freeIssueValue: z.number(),
  freeIssueValueCompany: z.number(),
  freeIssueValueDistributor: z.number(),
  distributorReturnValue: z.number().default(0),
  rejectionReason: z.string().nullable().optional(),
  notes: z.string().nullable(),
  items: z.array(billingItemSchema),
  lastAdjustedAt: z.string().nullable().default(null),
  adjustmentCount: z.number().default(0),
  adjustments: z.array(billingAdjustmentSchema).default([]),
})

export const rejectBillingSchema = z.object({
  reason: z.string().optional(),
})

export const adjustBillingItemsSchema = z.object({
  items: z.array(
    z.object({
      billingItemId: z.number(),
      quantity: z.number().min(0, 'Quantity cannot be negative'),
    }),
  ),
  note: z.string().max(1000, 'Note must not exceed 1000 characters').optional(),
})

export type DistributorBillingListItem = z.infer<typeof distributorBillingListItemSchema>
export type DistributorBillingDetail = z.infer<typeof distributorBillingDetailSchema>
export type BillingLineItem = z.infer<typeof billingItemSchema>
export type BillingAdjustment = z.infer<typeof billingAdjustmentSchema>
export type BillingAdjustmentLine = z.infer<typeof billingAdjustmentLineSchema>
export type RejectBillingInput = z.infer<typeof rejectBillingSchema>
export type AdjustBillingItemsInput = z.infer<typeof adjustBillingItemsSchema>

// GET /api/v1/billings/portal/dashboard-summary — server-side aggregates for the distributor dashboard.
// Revenue fields exclude rep-cancelled bills; counts include every bill issued.
export const distributorBillingDashboardDaySchema = z.object({
  date: z.string(), // YYYY-MM-DD (Sri Lanka business date)
  totalRevenue: z.number(),
  totalCount: z.number(),
  approvedRevenue: z.number(),
  approvedCount: z.number(),
  pendingRevenue: z.number(),
  pendingCount: z.number(),
})

export const distributorBillingDashboardSummarySchema = distributorBillingDashboardDaySchema
  .omit({ date: true })
  .extend({
    dateFrom: z.string(),
    dateTo: z.string(),
    days: z.array(distributorBillingDashboardDaySchema),
  })

export type DistributorBillingDashboardDay = z.infer<typeof distributorBillingDashboardDaySchema>
export type DistributorBillingDashboardSummary = z.infer<typeof distributorBillingDashboardSummarySchema>
