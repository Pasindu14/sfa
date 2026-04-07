'use client'

import { useState } from 'react'
import {
  queryOptions,
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getUserAssignmentsAction,
  getUserAssignmentByIdAction,
  getUserAssignmentStatsAction,
  createUserAssignmentAction,
  updateUserAssignmentAction,
  deactivateUserAssignmentAction,
  activateUserAssignmentAction,
  getUsersForGeoSelectAction,
  getActiveRegionsForSelectAction,
  getActiveAreasForSelectAction,
  getActiveTerritoriesForSelectAction,
  getActiveDivisionsForSelectAction,
} from '../actions/user-geo-assignment.actions'
import { useCreateDialog, useEditDialog, useDeactivateDialog, useActivateDialog } from '../store'
import { useUserGeoAssignmentFilterStore } from '../store/user-geo-assignment.filter-store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreateUserGeoAssignmentInput,
  UpdateUserGeoAssignmentInput,
} from '../schema/user-geo-assignment.schema'

// --- Query key factory ---

export const userGeoAssignmentKeys = {
  all: ['user-geo-assignments'] as const,
  lists: () => [...userGeoAssignmentKeys.all, 'list'] as const,
  list: (filters: object) => [...userGeoAssignmentKeys.lists(), filters] as const,
  details: () => [...userGeoAssignmentKeys.all, 'detail'] as const,
  detail: (id: number) => [...userGeoAssignmentKeys.details(), id] as const,
  stats: ['user-geo-assignments', 'stats'] as const,
  usersForSelect: ['geo-users-for-select'] as const,
  regionsForSelect: ['geo-regions-for-select'] as const,
  areasForSelect: ['geo-areas-for-select'] as const,
  territoriesForSelect: ['geo-territories-for-select'] as const,
  divisionsForSelect: ['geo-divisions-for-select'] as const,
}

// --- Query options factory ---

export function userGeoAssignmentQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: userGeoAssignmentKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getUserAssignmentsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useUserAssignment(id: number | null) {
  return useQuery({
    queryKey: userGeoAssignmentKeys.detail(id!),
    queryFn: async () => {
      const result = await getUserAssignmentByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function useUserAssignmentStats() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.stats,
    queryFn: async () => {
      const result = await getUserAssignmentStatsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 60 * 1000,
  })
}

// Pre-load all select data with 5-minute stale time — avoids repeated fetches
// each time the create/edit dialog opens.

export function useUsersForGeoSelect() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.usersForSelect,
    queryFn: async () => {
      const result = await getUsersForGeoSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useRegionsForSelect() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.regionsForSelect,
    queryFn: async () => {
      const result = await getActiveRegionsForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useAreasForSelect() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.areasForSelect,
    queryFn: async () => {
      const result = await getActiveAreasForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useTerritoriesForSelect() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.territoriesForSelect,
    queryFn: async () => {
      const result = await getActiveTerritoriesForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

export function useDivisionsForSelect() {
  return useQuery({
    queryKey: userGeoAssignmentKeys.divisionsForSelect,
    queryFn: async () => {
      const result = await getActiveDivisionsForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

// --- DataTable hook ---
// Reads committed filters directly from the store — the DataTable's own
// customFilters arg is intentionally ignored so the query only fires when
// the user clicks "Load Results" (committed !== null).

export function useUserGeoAssignmentDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: unknown,
) {
  const { committed } = useUserGeoAssignmentFilterStore()

  const query = useQuery({
    queryKey: userGeoAssignmentKeys.list({ page, pageSize, search, committed }),
    enabled: committed !== null,
    queryFn: async () => {
      const result = await getUserAssignmentsAction(
        page,
        pageSize,
        search || undefined,
        committed?.role,
        committed?.regionId,
        committed?.areaId,
        committed?.territoryId,
        committed?.divisionId,
        committed?.isActive,
      )
      if (!result.success) throw new Error(result.error)
      const { userAssignments, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: userAssignments,
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

  // Expose isFetching as isLoading so the DataTable spinner fires on every
  // re-fetch (filter change, page change) — not just the very first load.
  return { ...query, isLoading: query.isLoading || query.isFetching }
}

;(useUserGeoAssignmentDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateUserAssignment() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateUserGeoAssignmentInput) => {
      const result = await createUserAssignmentAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userGeoAssignmentKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Geo assignment saved successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'geo assignment', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateUserAssignment() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateUserGeoAssignmentInput }) => {
      const result = await updateUserAssignmentAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userGeoAssignmentKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Geo assignment updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'geo assignment', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeactivateUserAssignment() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateUserAssignmentAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userGeoAssignmentKeys.all })
      close()
      toast.success('Geo assignment deactivated')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'geo assignment', 'deactivate')
    },
  })
}

export function useActivateUserAssignment() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateUserAssignmentAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userGeoAssignmentKeys.all })
      close()
      toast.success('Geo assignment activated')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'geo assignment', 'activate')
    },
  })
}
