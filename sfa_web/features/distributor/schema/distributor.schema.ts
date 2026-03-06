import { z } from 'zod'

// Validation rules
const emailRules = z.string().min(1, 'Email is required').email('Invalid email format')
const phoneRules = z.string().min(10, 'Phone must be at least 10 characters').regex(/^[0-9+\-\s()]+$/, 'Invalid phone format')
const aliasRules = z.coerce.number().int('Alias must be a whole number').positive('Alias must be greater than 0')
const tradeDiscountRules = z.coerce.number().min(0, 'Trade discount is required and cannot be negative').max(100, 'Trade discount cannot exceed 100%')
const commissionRules = z.coerce.number().min(0, 'Commission is required and cannot be negative').max(100, 'Commission cannot exceed 100%')

// Create schema (all fields needed for creation)
export const createDistributorSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  address: z.string().min(1, 'Address is required'),
  phone: phoneRules,
  email: emailRules,
  alias: aliasRules,
  tradeDiscount: tradeDiscountRules,
  commission: commissionRules,
  remark: z.string().optional(),
  vatRegNo: z.string().optional(),
  latitude: z.coerce.number().optional(),
  longitude: z.coerce.number().optional(),
})

// Update schema (same as create)
export const updateDistributorSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  address: z.string().min(1, 'Address is required'),
  phone: phoneRules,
  email: emailRules,
  alias: aliasRules,
  tradeDiscount: tradeDiscountRules,
  commission: commissionRules,
  remark: z.string().optional(),
  vatRegNo: z.string().optional(),
  latitude: z.coerce.number().optional(),
  longitude: z.coerce.number().optional(),
})

// Filter schema (for search and pagination)
export const filterSchema = z.object({
  search: z.string().optional(),
  status: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Infer TypeScript types from schemas
export type CreateDistributorInput = z.infer<typeof createDistributorSchema>
export type UpdateDistributorInput = z.infer<typeof updateDistributorSchema>
export type DistributorFilterInput = z.infer<typeof filterSchema>

// DTO type (matches API response)
export type DistributorDto = {
  id: number
  name: string
  address: string
  phone: string
  email: string
  alias: number
  tradeDiscount: number
  commission: number
  remark: string | null
  vatRegNo: string | null
  latitude: number | null
  longitude: number | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}
