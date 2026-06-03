import { z } from 'zod'

export const upsertDraftSchema = z.object({
  periodId: z.number().int().positive(),
  lines: z.array(
    z.object({
      productId: z.number().int().positive('Select a product'),
      stockType: z.enum(['Normal', 'FreeIssue']),
      countedQuantity: z.number().min(0, 'Quantity cannot be negative'),
    })
  ).min(1, 'Add at least one product'),
})

export type UpsertDraftInput = z.infer<typeof upsertDraftSchema>

export type DistributorStockTakingPeriodDto = {
  id: number
  month: number
  year: number
  status: string
}

export type DistributorStockTakingLineDto = {
  id: number
  productId: number
  productCode: string
  productDescription: string
  stockType: string
  countedQuantity: number
  systemQuantity: number
  variance: number
  isAdjusted: boolean
  adjustedQuantity: number | null
}

export type DistributorStockTakingSubmissionDto = {
  id: number
  stockTakingPeriodId: number
  month: number
  year: number
  distributorId: number
  distributorName: string
  status: string
  submittedAt: string | null
  lines: DistributorStockTakingLineDto[]
}

export type ProductForSelect = {
  id: number
  code: string
  itemDescription: string
}
