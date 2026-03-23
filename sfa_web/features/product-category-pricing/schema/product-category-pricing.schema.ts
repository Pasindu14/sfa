import { z } from 'zod'

export const productCategoryPricingRowSchema = z.object({
  productId: z.number(),
  productCode: z.string(),
  itemDescription: z.string(),
  priceA: z.coerce.number().min(0),
  priceB: z.coerce.number().min(0),
  priceC: z.coerce.number().min(0),
  priceD: z.coerce.number().min(0),
})

export type ProductCategoryPricingRow = z.infer<typeof productCategoryPricingRowSchema>

export const bulkUpsertPricingSchema = z.object({
  items: z.array(
    z.object({
      productId: z.number(),
      priceA: z.coerce.number().min(0),
      priceB: z.coerce.number().min(0),
      priceC: z.coerce.number().min(0),
      priceD: z.coerce.number().min(0),
    })
  ),
})

export type BulkUpsertPricingInput = z.infer<typeof bulkUpsertPricingSchema>
