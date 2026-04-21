import { z } from 'zod'

// Create schema
export const createFleetSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
})

// Update schema (same shape as create)
export const updateFleetSchema = createFleetSchema

// Filter schema
export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Infer TypeScript types from schemas
export type CreateFleetInput = z.infer<typeof createFleetSchema>
export type UpdateFleetInput = z.infer<typeof updateFleetSchema>
export type FleetFilterInput = z.infer<typeof filterSchema>

// DTO type (matches API response)
export type FleetDto = {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
