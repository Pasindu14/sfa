import { z } from 'zod'

export const createProductCategorySchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
})

export const updateProductCategorySchema = createProductCategorySchema

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateProductCategoryInput = z.infer<typeof createProductCategorySchema>
export type UpdateProductCategoryInput = z.infer<typeof updateProductCategorySchema>
export type ProductCategoryFilterInput = z.infer<typeof filterSchema>

export type ProductCategoryDto = {
  id: number
  name: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
