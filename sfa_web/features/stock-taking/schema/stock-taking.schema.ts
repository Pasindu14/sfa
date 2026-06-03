import { z } from 'zod'

export const createPeriodSchema = z.object({
  month: z.number().int().min(1).max(12),
  year: z.number().int().min(2000).max(2100),
})

export const adjustLineSchema = z.object({
  adjustedQuantity: z.number().min(0, 'Adjusted quantity cannot be negative'),
})

export type CreatePeriodInput = z.infer<typeof createPeriodSchema>
export type AdjustLineInput = z.infer<typeof adjustLineSchema>

export type StockTakingPeriodDto = {
  id: number
  month: number
  year: number
  status: string
  lockedAt: string | null
  lockedBy: number | null
  lockedByName: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type StockTakingLineDto = {
  id: number
  stockTakingSubmissionId: number
  productId: number
  productCode: string
  productDescription: string
  stockType: string
  countedQuantity: number
  systemQuantity: number
  variance: number
  isAdjusted: boolean
  adjustedQuantity: number | null
  adjustedBy: number | null
  adjustedByName: string | null
  adjustedAt: string | null
}

export type StockTakingSubmissionDto = {
  id: number
  stockTakingPeriodId: number
  month: number
  year: number
  distributorId: number
  distributorName: string
  status: string
  submittedAt: string | null
  submittedBy: number | null
  submittedByName: string | null
  createdAt: string
  updatedAt: string
  lines: StockTakingLineDto[]
}

export type StockTakingPeriodsListResponse = {
  items: StockTakingPeriodDto[]
  totalCount: number
  page: number
  pageSize: number
}
