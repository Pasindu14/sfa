import { z } from 'zod'

export const createTerritorySchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  areaId: z
    .number({ error: 'Area is required' })
    .int()
    .min(1, 'Area is required'),
})

export const updateTerritorySchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  areaId: z
    .number({ error: 'Area is required' })
    .int()
    .min(1, 'Area is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateTerritoryInput = z.infer<typeof createTerritorySchema>
export type UpdateTerritoryInput = z.infer<typeof updateTerritorySchema>
export type TerritoryFilterInput = z.infer<typeof filterSchema>

export type TerritoryDto = {
  id: number
  name: string
  areaId: number
  areaName: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
