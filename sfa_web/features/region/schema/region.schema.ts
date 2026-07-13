import { z } from 'zod'

export const createRegionSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
})

export const updateRegionSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  // Optimistic concurrency token (PostgreSQL xmin) read on GET, echoed back on update.
  rowVersion: z.number().int().min(1, 'Row version is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateRegionInput = z.infer<typeof createRegionSchema>
export type UpdateRegionInput = z.infer<typeof updateRegionSchema>
export type RegionFilterInput = z.infer<typeof filterSchema>

export type RegionDto = {
  id: number
  name: string
  isActive: boolean
  rowVersion: number
  createdAt: string
  updatedAt: string
}
