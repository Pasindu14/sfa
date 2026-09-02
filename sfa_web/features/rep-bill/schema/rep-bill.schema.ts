import { z } from 'zod'

/**
 * Staff-side view of a bill. Mirrors the API's `BillingListDto`.
 *
 * Deliberately NOT reusing `distributor-billing.schema.ts`: that one models the *portal*
 * payload, which the staff endpoints do not return field-for-field (the staff detail carries
 * the org and geo chains, the portal one does not). Sharing a schema across the two would
 * silently mis-type whichever side drifts first.
 */
export const repBillListItemSchema = z.object({
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
})

/** One line on a bill. `expireDate` is staff-only — the portal DTO omits it. */
export const repBillItemSchema = z.object({
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
  expireDate: z.string().nullable(),
  lineNumber: z.number(),
})

/**
 * `GET /api/v1/billings/{id}` — the staff detail. Richer than the portal equivalent: it
 * denormalises the full reporting chain and geo chain onto the bill, which is what lets the
 * dialog show who the rep answered to *at the time the bill was written* rather than today.
 */
export const repBillDetailSchema = repBillListItemSchema.extend({
  // Org chain, frozen at write time
  supervisorUserId: z.number().nullable(),
  asmUserId: z.number().nullable(),
  asmName: z.string().nullable(),
  rsmUserId: z.number().nullable(),
  rsmName: z.string().nullable(),
  nsmUserId: z.number().nullable(),
  nsmName: z.string().nullable(),

  // Geo chain
  routeId: z.number().nullable(),
  divisionId: z.number().nullable(),
  territoryId: z.number().nullable(),
  areaId: z.number().nullable(),
  regionId: z.number().nullable(),

  // Amounts
  subTotalAmount: z.number(),
  billDiscountRate: z.number(),
  billDiscountAmount: z.number(),
  returnValue: z.number(),
  freeIssueValue: z.number(),
  freeIssueValueCompany: z.number(),
  freeIssueValueDistributor: z.number(),
  itemWiseTotalDiscount: z.number(),
  totalDiscount: z.number(),

  rejectionReason: z.string().nullable(),
  notes: z.string().nullable(),
  latitude: z.number().nullable(),
  longitude: z.number().nullable(),
  items: z.array(repBillItemSchema),
})

/** A supervisor option, from `GET /api/v1/users?role=Supervisor`. */
export const supervisorOptionSchema = z.object({
  id: z.number(),
  name: z.string(),
  username: z.string().nullable().optional(),
})

/** A direct report, from `GET /api/v1/user-reporting-lines/{id}/subordinates?depth=1`. */
export const repOptionSchema = z.object({
  userId: z.number(),
  userName: z.string(),
  userRole: z.string(),
  isActive: z.boolean(),
})

export type RepBillListItem = z.infer<typeof repBillListItemSchema>
export type RepBillDetail = z.infer<typeof repBillDetailSchema>
export type RepBillLineItem = z.infer<typeof repBillItemSchema>
export type SupervisorOption = z.infer<typeof supervisorOptionSchema>
export type RepOption = z.infer<typeof repOptionSchema>
