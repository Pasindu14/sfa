import { z } from 'zod'

// ── Import payload (sent to API) ──────────────────────────────────────────

export const targetRowRequestSchema = z.object({
  rowIndex: z.number(),
  repsCode: z.number(),
  itemCode: z.string(),
  targetQty: z.number(),
})

export const importSalesTargetsPayloadSchema = z.object({
  fileName: z.string(),
  year: z.number(),
  month: z.number(),
  rows: z.array(targetRowRequestSchema),
})

export type TargetRowRequest = z.infer<typeof targetRowRequestSchema>
export type ImportSalesTargetsPayload = z.infer<typeof importSalesTargetsPayloadSchema>

// ── API response DTOs ─────────────────────────────────────────────────────

export const salesTargetDtoSchema = z.object({
  id: z.number(),
  importBatchId: z.number(),
  year: z.number(),
  month: z.number(),
  salesRepId: z.number(),
  salesRepName: z.string(),
  productId: z.number(),
  productCode: z.string(),
  productName: z.string(),
  targetQuantity: z.number(),
  supervisorUserId: z.number().nullable().optional(),
  supervisorName: z.string().nullable().optional(),
  asmUserId: z.number().nullable().optional(),
  rsmUserId: z.number().nullable().optional(),
  nsmUserId: z.number().nullable().optional(),
  distributorId: z.number().nullable().optional(),
  divisionId: z.number().nullable().optional(),
  territoryId: z.number().nullable().optional(),
  areaId: z.number().nullable().optional(),
  regionId: z.number().nullable().optional(),
  updatedAt: z.string(),
})

export const salesTargetImportBatchDtoSchema = z.object({
  id: z.number(),
  batchNumber: z.string(),
  fileName: z.string(),
  year: z.number(),
  month: z.number(),
  totalRows: z.number(),
  insertedRows: z.number(),
  updatedRows: z.number(),
  skippedRows: z.number(),
  status: z.string(),
  importedBy: z.number(),
  importedByName: z.string(),
  importedAt: z.string(),
})

export const importSalesTargetsErrorSchema = z.object({
  rowIndex: z.number(),
  repsCode: z.number(),
  itemCode: z.string(),
  reason: z.string(),
})

export const importSalesTargetsResultSchema = z.object({
  batchId: z.number(),
  batchNumber: z.string(),
  year: z.number(),
  month: z.number(),
  totalRows: z.number(),
  insertedRows: z.number(),
  updatedRows: z.number(),
  skippedRows: z.number(),
  status: z.string(),
  errors: z.array(importSalesTargetsErrorSchema),
})

export type SalesTargetDto = z.infer<typeof salesTargetDtoSchema>
export type SalesTargetImportBatchDto = z.infer<typeof salesTargetImportBatchDtoSchema>
export type ImportSalesTargetsError = z.infer<typeof importSalesTargetsErrorSchema>
export type ImportSalesTargetsResult = z.infer<typeof importSalesTargetsResultSchema>
