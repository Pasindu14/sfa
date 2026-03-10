import { z } from 'zod'

export const createAreaSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  regionId: z
    .number({ error: 'Region is required' })
    .int()
    .min(1, 'Region is required'),
})

export const updateAreaSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  regionId: z
    .number({ error: 'Region is required' })
    .int()
    .min(1, 'Region is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateAreaInput = z.infer<typeof createAreaSchema>
export type UpdateAreaInput = z.infer<typeof updateAreaSchema>
export type AreaFilterInput = z.infer<typeof filterSchema>

export type AreaDto = {
  id: number
  name: string
  regionId: number
  regionName: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
