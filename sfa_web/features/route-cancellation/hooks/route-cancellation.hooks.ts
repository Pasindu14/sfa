'use client'

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPendingCancellationsAction,
  approveCancellationAction,
  rejectCancellationAction,
} from '../actions/route-cancellation.actions'
import { useApproveDialog, useRejectDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { RejectCancellationInput } from '../schema/route-cancellation.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const routeCancellationKeys = {
  all: ['routeCancellations'] as const,
  lists: () => [...routeCancellationKeys.all, 'list'] as const,
  list: (filters: object) => [...routeCancellationKeys.lists(), filters] as const,
}

// ── DataTable hook ─────────────────────────────────────────────────────────

export function useRouteCancellationDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: routeCancellationKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getPendingCancellationsAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { assignments, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: assignments,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
    staleTime: 30 * 1000,
  })
}

; (useRouteCancellationDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Mutation hooks ─────────────────────────────────────────────────────────

export function useApproveCancellation() {
  const queryClient = useQueryClient()
  const { close } = useApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await approveCancellationAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeCancellationKeys.all })
      close()
      toast.success('Cancellation approved — assignment deleted')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'cancellation request', 'approve')
    },
  })
}

export function useRejectCancellation() {
  const queryClient = useQueryClient()
  const { close } = useRejectDialog()

  return useMutation({
    mutationFn: async ({ id, data }: { id: number; data: RejectCancellationInput }) => {
      const result = await rejectCancellationAction(id, data)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeCancellationKeys.all })
      close()
      toast.success('Cancellation rejected — supervisor will be notified')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'cancellation request', 'reject')
    },
  })
}
