'use client'

import { useState } from 'react'
import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getActiveExemptionsAction,
  getExemptionHistoryAction,
  getCurrentExemptionAction,
  grantExemptionAction,
  revokeExemptionAction,
} from '../actions/proximity-exemption.actions'
import { useGrantDialog, useRevokeDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  GrantExemptionInput,
  GrantExemptionWithRepInput,
} from '../schema/proximity-exemption.schema'
import { ActionError } from '@/lib/actions/action-error'

export const exemptionKeys = {
  all: ['proximity-exemptions'] as const,
  lists: () => [...exemptionKeys.all, 'list'] as const,
  list: (filters: object) => [...exemptionKeys.lists(), filters] as const,
  byUser: (userId: number) => [...exemptionKeys.all, 'user', userId] as const,
  currentByUser: (userId: number) =>
    [...exemptionKeys.all, 'user', userId, 'current'] as const,
}

type QueryClientLike = ReturnType<typeof useQueryClient>

function invalidateForUser(queryClient: QueryClientLike, userId: number) {
  queryClient.invalidateQueries({ queryKey: exemptionKeys.byUser(userId) })
  queryClient.invalidateQueries({ queryKey: exemptionKeys.currentByUser(userId) })
  // The oversight list counts live grants, so it goes stale on every grant and
  // revoke too — invalidating only the per-user keys would leave a revoked rep
  // still showing as exempt on the list page.
  queryClient.invalidateQueries({ queryKey: exemptionKeys.lists() })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useProximityExemptionDataTable(
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
    queryKey: exemptionKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getActiveExemptionsAction(page, pageSize, search || undefined)
      if (!result.success) throw new ActionError(result)
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
    // Short, because a grant expiring silently drops a row off this list and the
    // page should not keep asserting someone is exempt after their window closed.
    staleTime: 30 * 1000,
  })
}

; (useProximityExemptionDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Queries ---

export function useExemptionHistory(userId: number | null) {
  return useQuery({
    queryKey: exemptionKeys.byUser(userId!),
    queryFn: async () => {
      const result = await getExemptionHistoryAction(userId!)
      if (!result.success) throw new ActionError(result)
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
      if (!result.success) throw new ActionError(result)
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

/// List-page variant of granting: the rep is chosen inside the form, so the user
/// id arrives with the submission rather than being fixed when the hook is made.
export function useGrantExemptionForRep() {
  const queryClient = useQueryClient()
  const { close } = useGrantDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ userId, ...data }: GrantExemptionWithRepInput) => {
      const result = await grantExemptionAction(userId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (_data, variables) => {
      invalidateForUser(queryClient, variables.userId)
      setFieldErrors(null)
      // Unlike the Users-page dialog, this one closes: the admin's next move is
      // reading the row that just appeared in the list behind it.
      close()
      toast.success('Proximity exemption granted')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'proximity exemption', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

/// List-page variant: the rep differs per row, so the user id travels with the
/// mutation rather than being fixed when the hook is created.
export function useRevokeExemptionFromList() {
  const queryClient = useQueryClient()
  const { close } = useRevokeDialog()

  return useMutation({
    mutationFn: async ({
      exemptionId,
      rowVersion,
    }: {
      exemptionId: number
      rowVersion: number
      userId: number
    }) => {
      const result = await revokeExemptionAction(exemptionId, rowVersion)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (_data, variables) => {
      invalidateForUser(queryClient, variables.userId)
      close()
      toast.success('Proximity exemption revoked')
    },
    onError: (error: ActionFailure, variables) => {
      handleErrorToast(error, 'proximity exemption', 'update')
      // Refetch either way: a CONCURRENCY_CONFLICT means someone else already
      // changed this grant, and the list should show what is actually true now.
      invalidateForUser(queryClient, variables.userId)
      close()
    },
  })
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
