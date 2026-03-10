import { z } from 'zod'

export const createRouteSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  pinColor: z
    .string()
    .min(1, 'Pin color is required')
    .max(50, 'Pin color must not exceed 50 characters'),
  description: z
    .string()
    .max(500, 'Description must not exceed 500 characters')
    .optional(),
  divisionId: z
    .number({ error: 'Division is required' })
    .int()
    .min(1, 'Division is required'),
})

export const updateRouteSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(100, 'Name must not exceed 100 characters'),
  pinColor: z
    .string()
    .min(1, 'Pin color is required')
    .max(50, 'Pin color must not exceed 50 characters'),
  description: z
    .string()
    .max(500, 'Description must not exceed 500 characters')
    .optional(),
  divisionId: z
    .number({ error: 'Division is required' })
    .int()
    .min(1, 'Division is required'),
})

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateRouteInput = z.infer<typeof createRouteSchema>
export type UpdateRouteInput = z.infer<typeof updateRouteSchema>
export type RouteFilterInput = z.infer<typeof filterSchema>

export type RouteDto = {
  id: number
  name: string
  pinColor: string
  description?: string | null
  divisionId: number
  divisionName: string
  territoryId: number
  territoryName: string
  areaId: number
  areaName: string
  regionId: number
  regionName: string
  createdAt: string
  updatedAt: string
}
