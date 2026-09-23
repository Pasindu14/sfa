'use client'

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { ActionError } from '@/lib/actions/action-error'
import type { ActionFailure } from '@/lib/types/actions'
import { stockKeys } from '@/features/stock/hooks/stock.hooks'
import {
  createStockAdjustmentAction,
  getStockAdjustmentAction,
  getStockAdjustmentsAction,
} from '../actions/stock-adjustment.actions'
import type { CreateStockAdjustmentInput, StockAdjustment } from '../schema/stock-adjustment.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const stockAdjustmentKeys = {
  all: ['stock-adjustments'] as const,
  lists: () => [...stockAdjustmentKeys.all, 'list'] as const,
  list: (filters: object) => [...stockAdjustmentKeys.lists(), filters] as const,
  details: () => [...stockAdjustmentKeys.all, 'detail'] as const,
  detail: (id: number) => [...stockAdjustmentKeys.details(), id] as const,
}

// ── DataTable hook (fetchDataFn with isQueryHook = true) ───────────────────

export function useStockAdjustmentDataTable(
  page: number,
  pageSize: number,
  _search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: unknown,
) {
  return useQuery({
    queryKey: stockAdjustmentKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getStockAdjustmentsAction(page, pageSize)
      if (!result.success) throw new ActionError(result)
      const { adjustments, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: adjustments,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(useStockAdjustmentDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Detail query hook ──────────────────────────────────────────────────────

export function useStockAdjustmentDetail(id: number | null) {
  return useQuery({
    queryKey: stockAdjustmentKeys.detail(id!),
    queryFn: async () => {
      const result = await getStockAdjustmentAction(id!)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    enabled: id !== null,
  })
}

// ── Create mutation hook ───────────────────────────────────────────────────

const ADJUSTMENT_ERROR_MESSAGES: Record<string, string> = {
  STOCK_CHANGED:
    'Stock changed since it was loaded. The balances have been reloaded — review your changes and try again.',
  NO_CHANGES: 'None of the lines differ from the current balance, so there is nothing to adjust.',
  STOCK_ADJUSTMENT_IN_PROGRESS:
    'Another stock adjustment for this distributor is in progress. Please wait a moment and try again.',
}

export function useCreateStockAdjustment(onSuccess?: (adjustment: StockAdjustment) => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ data, idempotencyKey }: { data: CreateStockAdjustmentInput; idempotencyKey: string }) => {
      const result = await createStockAdjustmentAction(data, idempotencyKey)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (adjustment) => {
      queryClient.invalidateQueries({ queryKey: stockKeys.all })
      queryClient.invalidateQueries({ queryKey: stockAdjustmentKeys.all })
      toast.success(`Stock adjustment ${adjustment.adjustmentNumber} saved`)
      onSuccess?.(adjustment)
    },
    onError: (error: unknown) => {
      const code = (error as { code?: string } | null)?.code
      const friendly = code ? ADJUSTMENT_ERROR_MESSAGES[code] : undefined
      if (friendly) {
        toast.error(friendly)
        if (code === 'STOCK_CHANGED') {
          queryClient.invalidateQueries({ queryKey: stockKeys.all })
        }
        return
      }
      handleErrorToast(error as ActionFailure, 'stock adjustment', 'create')
    },
  })
}
