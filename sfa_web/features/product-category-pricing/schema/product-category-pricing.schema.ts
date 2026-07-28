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

/**
 * Distributor portal shape — `GET /product-category-pricings/portal` resolves the
 * caller's category tier server-side and returns a single `unitPrice`, never the
 * full A/B/C/D spread (that would leak other tiers to a Distributor-role caller).
 * Do NOT substitute `productCategoryPricingRowSchema` here.
 */
export const distributorProductPriceSchema = z.object({
  productId: z.number(),
  productCode: z.string(),
  itemDescription: z.string(),
  unitPrice: z.coerce.number().min(0),
})

export type DistributorProductPrice = z.infer<typeof distributorProductPriceSchema>

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
