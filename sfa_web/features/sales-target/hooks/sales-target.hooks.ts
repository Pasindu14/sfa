'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import {
  getSalesTargetsAction,
  importSalesTargetsAction,
  getImportBatchesAction,
  updateSalesTargetAction,
} from '../actions/sales-target.actions'
import { useImportTargetDialog, useEditTargetDialog } from '../store/sales-target-dialog.store'
import type { ImportSalesTargetsPayload, ImportSalesTargetsResult, UpdateTargetQuantityInput } from '../schema/sales-target.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const salesTargetKeys = {
  all: ['sales-targets'] as const,
  lists: () => [...salesTargetKeys.all, 'list'] as const,
  list: (filters: object) => [...salesTargetKeys.lists(), filters] as const,
  batches: () => [...salesTargetKeys.all, 'batches'] as const,
  batchList: (filters: object) => [...salesTargetKeys.batches(), filters] as const,
}

// ── Targets DataTable hook ─────────────────────────────────────────────────

export function useSalesTargetsDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { year?: number; month?: number; salesRepId?: number },
) {
  return useQuery({
    queryKey: salesTargetKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getSalesTargetsAction(
        page,
        pageSize,
        search || undefined,
        customFilters?.year,
        customFilters?.month,
        customFilters?.salesRepId,
      )
      if (!result.success) throw new Error(result.error)
      const { targets, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: targets,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
  })
}

;(useSalesTargetsDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Import batch history DataTable hook ───────────────────────────────────

export function useImportBatchesDataTable(
  page: number,
  pageSize: number,
  _search: string,
) {
  return useQuery({
    queryKey: salesTargetKeys.batchList({ page, pageSize }),
    queryFn: async () => {
      const result = await getImportBatchesAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      const { batches, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: batches,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
  })
}

;(useImportBatchesDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Update quantity mutation hook ─────────────────────────────────────────

export function useUpdateSalesTarget() {
  const queryClient = useQueryClient()
  const { close } = useEditTargetDialog()

  return useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateTargetQuantityInput }) => {
      const result = await updateSalesTargetAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesTargetKeys.all })
      close()
      toast.success('Target quantity updated')
    },
    onError: (error: unknown) => {
      handleErrorToast(error as any, 'sales target', 'update')
    },
  })
}

// ── Import mutation hook ───────────────────────────────────────────────────

export function useImportSalesTargets() {
  const { close } = useImportTargetDialog()
  const queryClient = useQueryClient()
  const [importResult, setImportResult] = useState<ImportSalesTargetsResult | null>(null)

  const mutation = useMutation({
    mutationFn: async (payload: ImportSalesTargetsPayload) => {
      const result = await importSalesTargetsAction(payload)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      setImportResult(data)
      queryClient.invalidateQueries({ queryKey: salesTargetKeys.all })
      if (data.skippedRows === 0) {
        toast.success(`Imported ${data.insertedRows + data.updatedRows} targets (${data.batchNumber})`)
      } else if (data.insertedRows + data.updatedRows > 0) {
        toast.warning(`Imported ${data.insertedRows + data.updatedRows}/${data.totalRows} — ${data.skippedRows} skipped`)
      } else {
        toast.error('Import failed — no targets were imported')
      }
    },
    onError: (error: unknown) => {
      handleErrorToast(error as any, 'sales target', 'import')
    },
  })

  function reset() {
    setImportResult(null)
    mutation.reset()
  }

  return {
    ...mutation,
    importResult,
    reset,
    closeAndReset: () => { reset(); close() },
  }
}
