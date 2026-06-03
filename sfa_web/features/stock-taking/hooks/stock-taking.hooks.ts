'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPeriodsAction,
  getPeriodByIdAction,
  createPeriodAction,
  lockPeriodAction,
  unlockPeriodAction,
  getSubmissionForAdminAction,
  adjustLineAction,
} from '../actions/stock-taking.actions'
import {
  useCreateDialog,
  useLockDialog,
  useUnlockDialog,
  useAdjustDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreatePeriodInput, AdjustLineInput } from '../schema/stock-taking.schema'

export const stockTakingKeys = {
  all: ['stock-taking'] as const,
  lists: () => [...stockTakingKeys.all, 'list'] as const,
  list: (filters: object) => [...stockTakingKeys.lists(), filters] as const,
  details: () => [...stockTakingKeys.all, 'detail'] as const,
  detail: (id: number) => [...stockTakingKeys.details(), id] as const,
  submission: (periodId: number, distributorId: number) =>
    [...stockTakingKeys.all, 'submission', periodId, distributorId] as const,
}

// --- DataTable hook (8-arg pattern) ---

export function useStockTakingDataTable(
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
    queryKey: stockTakingKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getPeriodsAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { items, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: items,
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

;(useStockTakingDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Single period ---

export function usePeriod(id: number | null) {
  return useQuery({
    queryKey: stockTakingKeys.detail(id!),
    queryFn: async () => {
      const result = await getPeriodByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- Admin submission view ---

export function useSubmissionForAdmin(periodId: number, distributorId: number | null) {
  return useQuery({
    queryKey: stockTakingKeys.submission(periodId, distributorId ?? 0),
    queryFn: async () => {
      const result = await getSubmissionForAdminAction(periodId, distributorId!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: distributorId !== null && distributorId > 0,
  })
}

// --- Mutations ---

export function useCreatePeriod() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreatePeriodInput) => {
      const result = await createPeriodAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTakingKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Stock taking period created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'period', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useLockPeriod() {
  const queryClient = useQueryClient()
  const { close } = useLockDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await lockPeriodAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTakingKeys.all })
      close()
      toast.success('Period locked successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'period', 'lock')
    },
  })
}

export function useUnlockPeriod() {
  const queryClient = useQueryClient()
  const { close } = useUnlockDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await unlockPeriodAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTakingKeys.all })
      close()
      toast.success('Period unlocked successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'period', 'unlock')
    },
  })
}

export function useAdjustLine() {
  const queryClient = useQueryClient()
  const { close } = useAdjustDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ lineId, data }: { lineId: number; data: AdjustLineInput }) => {
      const result = await adjustLineAction(lineId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: stockTakingKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Stock adjusted successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'line', 'adjust')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
