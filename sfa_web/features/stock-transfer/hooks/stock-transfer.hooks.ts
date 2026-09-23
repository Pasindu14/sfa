'use client'

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { ActionError } from '@/lib/actions/action-error'
import { stockKeys } from '@/features/stock/hooks/stock.hooks'
import {
  createStockTransferAction,
  getStockTransferAction,
  getStockTransfersAction,
} from '../actions/stock-transfer.actions'
import type { CreateStockTransferInput, StockTransfer } from '../schema/stock-transfer.schema'
import type { ActionFailure } from '@/lib/types/actions'

// ── Query key factory ──────────────────────────────────────────────────────

export const stockTransferKeys = {
  all: ['stock-transfers'] as const,
  lists: () => [...stockTransferKeys.all, 'list'] as const,
  list: (filters: object) => [...stockTransferKeys.lists(), filters] as const,
  details: () => [...stockTransferKeys.all, 'detail'] as const,
  detail: (id: number) => [...stockTransferKeys.details(), id] as const,
}

// ── DataTable hook (fetchDataFn with isQueryHook = true) ───────────────────

export function useStockTransferDataTable(
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
    queryKey: stockTransferKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getStockTransfersAction(page, pageSize)
      if (!result.success) throw new ActionError(result)
      const { transfers, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: transfers,
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

;(useStockTransferDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Detail query hook ──────────────────────────────────────────────────────

export function useStockTransferDetail(id: number | null) {
  return useQuery({
    queryKey: stockTransferKeys.detail(id!),
    queryFn: async () => {
      const result = await getStockTransferAction(id!)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    enabled: id !== null,
  })
}

// ── Create mutation hook ───────────────────────────────────────────────────

const TRANSFER_ERROR_MESSAGES: Record<string, string> = {
  SOURCE_DISTRIBUTOR_ACTIVE:
    'The source distributor is still active. Only closed distributors can transfer out their stock.',
  TARGET_DISTRIBUTOR_INACTIVE:
    'The target distributor is inactive. Choose an active distributor to receive the stock.',
  INSUFFICIENT_STOCK:
    'Stock changed since it was loaded — one or more lines exceed the current balance. Reload and try again.',
  STOCK_TRANSFER_IN_PROGRESS:
    'Another stock transfer for this distributor is in progress. Please wait a moment and try again.',
}

export function useCreateStockTransfer(onSuccess?: (transfer: StockTransfer) => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ data, idempotencyKey }: { data: CreateStockTransferInput; idempotencyKey: string }) => {
      const result = await createStockTransferAction(data, idempotencyKey)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (transfer) => {
      queryClient.invalidateQueries({ queryKey: stockKeys.all })
      queryClient.invalidateQueries({ queryKey: stockTransferKeys.all })
      toast.success(`Stock transfer ${transfer.transferNumber} completed`)
      onSuccess?.(transfer)
    },
    onError: (error: unknown) => {
      const code = (error as { code?: string } | null)?.code
      const friendly = code ? TRANSFER_ERROR_MESSAGES[code] : undefined
      if (friendly) {
        toast.error(friendly)
        if (code === 'INSUFFICIENT_STOCK') {
          queryClient.invalidateQueries({ queryKey: stockKeys.all })
        }
        return
      }
      handleErrorToast(error as ActionFailure, 'stock transfer', 'create')
    },
  })
}
