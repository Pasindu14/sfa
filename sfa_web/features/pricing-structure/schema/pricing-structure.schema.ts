import { z } from 'zod'

export const createPricingStructureSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  description: z.string().max(500, 'Description must not exceed 500 characters').optional().or(z.literal('')),
  isDefault: z.boolean(),
})

export const updatePricingStructureSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  description: z.string().max(500, 'Description must not exceed 500 characters').optional().or(z.literal('')),
  isDefault: z.boolean(),
})

export const pricingStructureItemRequestSchema = z.object({
  productId: z.number().int().positive('Product is required'),
  dealerPackPrice: z.number().min(0, 'Dealer pack price must be 0 or greater').optional(),
  dealerCasePrice: z.number().min(0, 'Dealer case price must be 0 or greater').optional(),
  promotionalPrice: z.number().min(0, 'Promotional price must be 0 or greater').optional(),
})

export const bulkUpdateItemsSchema = z.object({
  items: z.array(pricingStructureItemRequestSchema).min(1, 'At least one item is required'),
})

export type CreatePricingStructureInput = z.infer<typeof createPricingStructureSchema>
export type UpdatePricingStructureInput = z.infer<typeof updatePricingStructureSchema>
export type PricingStructureItemRequestInput = z.infer<typeof pricingStructureItemRequestSchema>
export type BulkUpdateItemsInput = z.infer<typeof bulkUpdateItemsSchema>

export type PricingStructureDto = {
  id: number
  name: string
  description?: string
  isDefault: boolean
  isActive: boolean
  itemCount: number
  createdAt: string
  updatedAt: string
}

export type PricingStructureItemDto = {
  id: number
  pricingStructureId: number
  productId: number
  productCode: string
  productItemDescription: string
  dealerPackPrice?: number
  dealerCasePrice?: number
  promotionalPrice?: number
}

export type PricingStructureDetailDto = PricingStructureDto & {
  items: PricingStructureItemDto[]
}

export type PricingStructureListDto = {
  pricingStructures: PricingStructureDto[]
  totalCount: number
  page: number
  pageSize: number
}
