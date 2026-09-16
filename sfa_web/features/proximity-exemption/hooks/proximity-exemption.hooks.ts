'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getExemptionHistoryAction,
  getCurrentExemptionAction,
  grantExemptionAction,
  revokeExemptionAction,
} from '../actions/proximity-exemption.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { GrantExemptionInput } from '../schema/proximity-exemption.schema'

export const exemptionKeys = {
  all: ['proximity-exemptions'] as const,
  byUser: (userId: number) => [...exemptionKeys.all, 'user', userId] as const,
  currentByUser: (userId: number) =>
    [...exemptionKeys.all, 'user', userId, 'current'] as const,
}

type QueryClientLike = ReturnType<typeof useQueryClient>

function invalidateForUser(queryClient: QueryClientLike, userId: number) {
  queryClient.invalidateQueries({ queryKey: exemptionKeys.byUser(userId) })
  queryClient.invalidateQueries({ queryKey: exemptionKeys.currentByUser(userId) })
}

// --- Queries ---

export function useExemptionHistory(userId: number | null) {
  return useQuery({
    queryKey: exemptionKeys.byUser(userId!),
    queryFn: async () => {
      const result = await getExemptionHistoryAction(userId!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: userId !== null,
  })
}

export function useCurrentExemption(userId: number | null) {
  return useQuery({
    queryKey: exemptionKeys.currentByUser(userId!),
    queryFn: async () => {
      const result = await getCurrentExemptionAction(userId!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: userId !== null,
  })
}

// --- Mutations ---

export function useGrantExemption(userId: number | null) {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: GrantExemptionInput) => {
      const result = await grantExemptionAction(userId!, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      if (userId !== null) invalidateForUser(queryClient, userId)
      setFieldErrors(null)
      // The dialog deliberately stays open: the admin's next move is almost
      // always to check the grant now shows as active, and closing would hide it.
      toast.success('Proximity exemption granted')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'proximity exemption', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useRevokeExemption(userId: number | null) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      exemptionId,
      rowVersion,
    }: {
      exemptionId: number
      rowVersion: number
    }) => {
      const result = await revokeExemptionAction(exemptionId, rowVersion)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      if (userId !== null) invalidateForUser(queryClient, userId)
      toast.success('Proximity exemption revoked')
    },
    onError: (error: ActionFailure) => {
      // A CONCURRENCY_CONFLICT here means another admin already changed this
      // grant; handleErrorToast says so, and the refetch below shows the truth.
      handleErrorToast(error, 'proximity exemption', 'update')
      if (userId !== null) invalidateForUser(queryClient, userId)
    },
  })
}
