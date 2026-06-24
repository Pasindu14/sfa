import { z } from 'zod'

// Create schema
export const createProductSchema = z.object({
  code: z.string().min(1, 'Code is required').max(50, 'Code must not exceed 50 characters'),
  itemDescription: z
    .string()
    .min(1, 'Item description is required')
    .max(255, 'Item description must not exceed 255 characters'),
  printDescription: z
    .string()
    .max(255, 'Print description must not exceed 255 characters')
    .optional()
    .or(z.literal('')),
  piecesPerPack: z
    .number()
    .int('Pieces per pack must be a whole number')
    .min(0, 'Pieces per pack must be 0 or greater'),
  imageUrl: z
    .string()
    .max(500, 'Image URL must not exceed 500 characters')
    .optional()
    .or(z.literal('')),
  remarks: z.string().optional().or(z.literal('')),
  fleetId: z.number().int().positive('Must select a valid fleet').optional(),
  categoryId: z.number().int().positive('Must select a valid category').optional(),
  dealerPackPrice: z.number().min(0, 'Dealer pack price must be 0 or greater'),
  dealerCasePrice: z.number().min(0, 'Dealer case price must be 0 or greater'),
  mrp: z.number().min(0, 'MRP must be 0 or greater'),
})

// Update schema (create shape + concurrency token)
export const updateProductSchema = createProductSchema.extend({
  rowVersion: z.number().int().min(1, 'Row version is required'),
})

// Filter schema
export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Infer TypeScript types from schemas
export type CreateProductInput = z.infer<typeof createProductSchema>
export type UpdateProductInput = z.infer<typeof updateProductSchema>
export type ProductFilterInput = z.infer<typeof filterSchema>

// DTO type (matches API response)
export type ProductDto = {
  id: number
  code: string
  itemDescription: string
  printDescription: string | null
  piecesPerPack: number
  imageUrl: string | null
  remarks: string | null
  fleetId: number | null
  fleetName: string | null
  categoryId: number | null
  categoryName: string | null
  isActive: boolean
  rowVersion: number
  dealerPackPrice: number
  dealerCasePrice: number
  mrp: number
  createdAt: string
  updatedAt: string
}
