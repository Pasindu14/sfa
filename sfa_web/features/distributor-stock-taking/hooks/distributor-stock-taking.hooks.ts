'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getOpenPeriodsAction,
  getMySubmissionAction,
  upsertDraftAction,
  submitStockTakingAction,
  upsertAndSubmitAction,
} from '../actions/distributor-stock-taking.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { UpsertDraftInput } from '../schema/distributor-stock-taking.schema'

export const distributorStockTakingKeys = {
  all: ['distributor-stock-taking'] as const,
  periods: () => [...distributorStockTakingKeys.all, 'periods'] as const,
  submission: (periodId: number) =>
    [...distributorStockTakingKeys.all, 'submission', periodId] as const,
}

export function useOpenPeriods() {
  return useQuery({
    queryKey: distributorStockTakingKeys.periods(),
    queryFn: async () => {
      const result = await getOpenPeriodsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

export function useMySubmission(periodId: number | null) {
  return useQuery({
    queryKey: distributorStockTakingKeys.submission(periodId ?? 0),
    queryFn: async () => {
      const result = await getMySubmissionAction(periodId!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: periodId !== null,
  })
}

export function useUpsertDraft(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: UpsertDraftInput) => {
      const result = await upsertDraftAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: distributorStockTakingKeys.all })
      toast.success('Draft saved successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'draft', 'save')
    },
  })
}

export function useSubmitStockTaking(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (periodId: number) => {
      const result = await submitStockTakingAction(periodId)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorStockTakingKeys.all })
      toast.success('Stock count submitted successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'submission', 'submit')
    },
  })
}

export function useUpsertAndSubmit(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: UpsertDraftInput) => {
      const result = await upsertAndSubmitAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorStockTakingKeys.all })
      toast.success('Stock count submitted successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'submission', 'submit')
    },
  })
}
