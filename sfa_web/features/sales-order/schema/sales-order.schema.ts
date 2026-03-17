import { z } from 'zod'

// Create order
// distributorId: null is valid for Distributor role (server resolves from JWT).
// Admin role: UI enforces non-null via required <Select> — schema permits null to avoid role-aware logic here.
export const createSalesOrderSchema = z.object({
  distributorId: z.number().int().positive().nullable(),
  notes: z.string().max(1000).nullable().optional(),
  items: z.array(z.object({
    productId: z.number().int().positive(),
    quantity: z.number().int().min(1, 'Quantity must be at least 1'),
    unitPrice: z.number().min(0),
    discount: z.literal(0),
  })).min(1, 'At least one item is required'),
})
export type CreateSalesOrderInput = z.infer<typeof createSalesOrderSchema>

// Update order (Draft only)
export const updateSalesOrderSchema = z.object({
  notes: z.string().max(1000).nullable().optional(),
  items: z.array(z.object({
    productId: z.number().int().positive(),
    quantity: z.number().int().min(1, 'Quantity must be at least 1'),
    unitPrice: z.number().min(0),
    discount: z.literal(0),
  })).min(1, 'At least one item is required'),
})
export type UpdateSalesOrderInput = z.infer<typeof updateSalesOrderSchema>

// Reject / Cancel reason
export const rejectSalesOrderSchema = z.object({
  reason: z.string()
    .min(5, 'Reason must be at least 5 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})
export type RejectSalesOrderInput = z.infer<typeof rejectSalesOrderSchema>
