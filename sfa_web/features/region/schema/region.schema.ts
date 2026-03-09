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
  createdAt: string
  updatedAt: string
}
