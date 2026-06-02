'use client'

import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getMyGrnsAction, getMyGrnDetailAction, confirmMyGrnAction } from '../actions/distributor-grn.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { MyGrnListItem, ConfirmMyGrnInput } from '../schema/distributor-grn.schema'
import type { ActionFailure } from '@/lib/types/actions'

export const myGrnKeys = {
  all: ['my-grns'] as const,
  list: (params: object) => [...myGrnKeys.all, 'list', params] as const,
  detail: (id: number) => [...myGrnKeys.all, 'detail', id] as const,
}

export function useMyGrnsDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  const status = customFilters?.status
  const dateFrom = dateRange?.from_date
  const dateTo = dateRange?.to_date

  return useQuery({
    queryKey: myGrnKeys.list({ page, pageSize, search, status, dateFrom, dateTo }),
    queryFn: async () => {
      const result = await getMyGrnsAction(
        page, pageSize,
        status || undefined,
        dateFrom || undefined,
        dateTo || undefined,
        search || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { grns, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: grns as MyGrnListItem[],
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

;(useMyGrnsDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useMyGrnPendingCount() {
  return useQuery({
    queryKey: [...myGrnKeys.all, 'pending-count'] as const,
    queryFn: async () => {
      const result = await getMyGrnsAction(1, 1, 'Pending')
      if (!result.success) throw new Error(result.error)
      return result.data.totalCount
    },
    staleTime: 60_000,
  })
}

export function useMyGrnDetail(id: number | null) {
  return useQuery({
    queryKey: myGrnKeys.detail(id!),
    queryFn: async () => {
      const result = await getMyGrnDetailAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function useConfirmMyGrn(onSuccess?: () => void) {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: ConfirmMyGrnInput }) => {
      const result = await confirmMyGrnAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myGrnKeys.all })
      setFieldErrors(null)
      toast.success('GRN confirmed successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'GRN', 'confirm')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
