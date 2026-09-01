'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getDailyRouteAssignmentsAction,
  getSupervisorsForSelectAction,
  getSupervisorRepsAction,
  getRepRoutesAction,
  createDailyRouteAssignmentAction,
  deleteDailyRouteAssignmentAction,
} from '../actions/daily-route-assignment.actions'
import { useCreateDialog, useDeleteDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { userSelectKeys } from '@/lib/api/query-keys'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateDailyRouteAssignmentInput } from '../schema/daily-route-assignment.schema'

// --- Query key factory ---

export const dailyRouteAssignmentKeys = {
  all: ['daily-route-assignments'] as const,
  lists: () => [...dailyRouteAssignmentKeys.all, 'list'] as const,
  list: (filters: object) => [...dailyRouteAssignmentKeys.lists(), filters] as const,
  reps: (supervisorId: number) => [...dailyRouteAssignmentKeys.all, 'reps', supervisorId] as const,
  routes: (userId: number) => [...dailyRouteAssignmentKeys.all, 'routes', userId] as const,
  // Shared key — invalidated by the Users feature's mutations so a deactivated
  // supervisor drops out of the picker immediately.
  supervisorsForSelect: userSelectKeys.routeAssignment,
}

// --- Select-picker hooks ---

// Preloads all active users once with a 5-minute stale time — filtered to
// role === 'Supervisor' by the caller.
export function useSupervisorsForSelect() {
  return useQuery({
    queryKey: dailyRouteAssignmentKeys.supervisorsForSelect,
    queryFn: async () => {
      const result = await getSupervisorsForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useSupervisorReps(supervisorId: number) {
  return useQuery({
    queryKey: dailyRouteAssignmentKeys.reps(supervisorId),
    queryFn: async () => {
      const result = await getSupervisorRepsAction(supervisorId)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: supervisorId > 0,
  })
}

export function useRepRoutes(userId: number) {
  return useQuery({
    queryKey: dailyRouteAssignmentKeys.routes(userId),
    queryFn: async () => {
      const result = await getRepRoutesAction(userId)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: userId > 0,
  })
}

// --- DataTable hook ---

export function useDailyRouteAssignmentDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { userId?: number; routeId?: number; date?: string },
) {
  return useQuery({
    queryKey: dailyRouteAssignmentKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getDailyRouteAssignmentsAction(
        page,
        pageSize,
        customFilters?.userId || undefined,
        customFilters?.routeId || undefined,
        customFilters?.date || undefined,
      )
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
  })
}

;(useDailyRouteAssignmentDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateDailyRouteAssignment() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateDailyRouteAssignmentInput) => {
      const result = await createDailyRouteAssignmentAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: dailyRouteAssignmentKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Route assigned successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'route assignment', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteDailyRouteAssignment() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteDailyRouteAssignmentAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: dailyRouteAssignmentKeys.all })
      close()
      toast.success('Route assignment removed')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'route assignment', 'delete')
    },
  })
}
