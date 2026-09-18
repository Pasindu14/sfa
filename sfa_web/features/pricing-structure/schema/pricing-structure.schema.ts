import { z } from 'zod'

// Limits mirror PricingStructureValidators.cs so the form rejects what the API would.
const name = z
  .string()
  .trim()
  .min(1, 'Name is required')
  .max(100, 'Name must not exceed 100 characters')

const description = z
  .string()
  .max(500, 'Description must not exceed 500 characters')
  .optional()
  .or(z.literal(''))

// Create schema
export const createPricingStructureSchema = z.object({
  name,
  description,
})

// Update schema (create shape + concurrency token)
export const updatePricingStructureSchema = createPricingStructureSchema.extend({
  rowVersion: z.number().int().min(1, 'Row version is required'),
})

// Duplicate schema — the copy gets its own name; prices come from the source server-side
export const duplicatePricingStructureSchema = createPricingStructureSchema

// Filter schema
export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Infer TypeScript types from schemas.
// `*Input` are the parsed OUTPUT types; `*FormInput` are the raw INPUT types the form holds.
export type CreatePricingStructureInput = z.infer<typeof createPricingStructureSchema>
export type UpdatePricingStructureInput = z.infer<typeof updatePricingStructureSchema>
export type DuplicatePricingStructureInput = z.infer<typeof duplicatePricingStructureSchema>
export type CreatePricingStructureFormInput = z.input<typeof createPricingStructureSchema>
export type UpdatePricingStructureFormInput = z.input<typeof updatePricingStructureSchema>
export type PricingStructureFilterInput = z.infer<typeof filterSchema>

// --- Price rules (PricingStructureValidators.cs → UpdatePricingStructureItemsRequest) ---

export const MIN_PRICE = 0.01
export const MAX_PRICE = 1_000_000

/** One changed row for `PUT /pricing-structures/{id}/items`. Null clears that price. */
export const pricingStructureItemUpdateSchema = z
  .object({
    productId: z.number().int().positive(),
    dealerPackPrice: z.number().min(MIN_PRICE).max(MAX_PRICE).nullable(),
    dealerCasePrice: z.number().min(MIN_PRICE).max(MAX_PRICE).nullable(),
    mrp: z.number().min(MIN_PRICE).max(MAX_PRICE).nullable(),
  })
  .refine(
    (r) => r.dealerPackPrice !== null || (r.dealerCasePrice === null && r.mrp === null),
    { message: 'Set a pack price before setting a case price or MRP', path: ['dealerPackPrice'] },
  )

export const updatePricingStructureItemsSchema = z.object({
  items: z.array(pricingStructureItemUpdateSchema).min(1).max(5000),
})

export type PricingStructureItemUpdate = z.infer<typeof pricingStructureItemUpdateSchema>
export type UpdatePricingStructureItemsInput = z.infer<typeof updatePricingStructureItemsSchema>

// --- DTO types (match API responses) ---

export type PricingStructureDto = {
  id: number
  name: string
  description: string | null
  isDefault: boolean
  isActive: boolean
  /** Products with a pack price in this structure. */
  pricedCount: number
  rowVersion: number
  createdAt: string
  updatedAt: string
}

/** One row of the prices grid — every non-deleted product, priced or not. */
export type PricingStructureItemRow = {
  productId: number
  productCode: string
  itemDescription: string
  piecesPerPack: number
  isProductActive: boolean
  /** Null = not priced in this structure. */
  dealerPackPrice: number | null
  dealerCasePrice: number | null
  mrp: number | null
}

/** `GET /pricing-structures/default/prices` — priced, active products of the default structure. */
export type DefaultStructurePrice = {
  productId: number
  dealerPackPrice: number
  dealerCasePrice: number | null
  mrp: number | null
}

export type DefaultStructurePrices = {
  pricingStructureId: number
  name: string
  items: DefaultStructurePrice[]
}
