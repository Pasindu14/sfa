import { z } from 'zod'

export const createDivisionSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  territoryId: z
    .number({ error: 'Territory is required' })
    .int()
    .min(1, 'Territory is required'),
})

export const updateDivisionSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  territoryId: z
    .number({ error: 'Territory is required' })
    .int()
    .min(1, 'Territory is required'),
  // Optimistic concurrency token (PostgreSQL xmin) read on GET, echoed back on update.
  rowVersion: z
    .number()
    .int()
    .min(1, 'Row version is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateDivisionInput = z.infer<typeof createDivisionSchema>
export type UpdateDivisionInput = z.infer<typeof updateDivisionSchema>
export type DivisionFilterInput = z.infer<typeof filterSchema>

export type DivisionDto = {
  id: number
  name: string
  territoryId: number
  territoryName: string
  areaId: number
  areaName: string
  regionId: number
  regionName: string
  isActive: boolean
  rowVersion: number
  createdAt: string
  updatedAt: string
}
