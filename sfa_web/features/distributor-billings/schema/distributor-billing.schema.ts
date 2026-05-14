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
  status: z.enum(['Submitted', 'Approved', 'Cancelled']),
  createdAt: z.string(),
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
  returnType: z.enum(['MarketResell', 'Damage', 'Expire']).nullable(),
  freeIssueSource: z.enum(['Company', 'Distributor']).nullable(),
  lineNumber: z.number(),
})

export const distributorBillingDetailSchema = distributorBillingListItemSchema.extend({
  subTotalAmount: z.number(),
  billDiscountRate: z.number(),
  billDiscountAmount: z.number(),
  returnValue: z.number(),
  freeIssueValue: z.number(),
  freeIssueValueCompany: z.number(),
  freeIssueValueDistributor: z.number(),
  notes: z.string().nullable(),
  items: z.array(billingItemSchema),
})

export type DistributorBillingListItem = z.infer<typeof distributorBillingListItemSchema>
export type DistributorBillingDetail = z.infer<typeof distributorBillingDetailSchema>
export type BillingLineItem = z.infer<typeof billingItemSchema>
