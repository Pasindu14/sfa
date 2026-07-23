'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getUserReportingLinesAction,
  getUserReportingLineByIdAction,
  createUserReportingLineAction,
  updateUserReportingLineAction,
  deactivateUserReportingLineAction,
  activateUserReportingLineAction,
  getUsersForSelectAction,
} from '../actions/user-reporting-line.actions'
import { useCreateDialog, useEditDialog, useDeactivateDialog, useActivateDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { userSelectKeys } from '@/lib/api/query-keys'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreateUserReportingLineInput,
  UpdateUserReportingLineInput,
} from '../schema/user-reporting-line.schema'

// --- Query key factory ---

export const userReportingLineKeys = {
  all: ['user-reporting-lines'] as const,
  lists: () => [...userReportingLineKeys.all, 'list'] as const,
  list: (filters: object) => [...userReportingLineKeys.lists(), filters] as const,
  details: () => [...userReportingLineKeys.all, 'detail'] as const,
  detail: (id: number) => [...userReportingLineKeys.details(), id] as const,
  // Shared key — invalidated by the Users feature's mutations so a deactivated
  // user drops out of the subordinate/manager pickers immediately.
  usersForSelect: userSelectKeys.reportingLine,
}

// --- Query options factory ---

export function userReportingLineQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: userReportingLineKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getUserReportingLinesAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useUserReportingLine(id: number | null) {
  return useQuery({
    queryKey: userReportingLineKeys.detail(id!),
    queryFn: async () => {
      const result = await getUserReportingLineByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// Preloads all users once with a 5-minute stale time — avoids repeated fetches
// each time the create/edit dialog opens
export function useUsersForSelect() {
  return useQuery({
    queryKey: userReportingLineKeys.usersForSelect,
    queryFn: async () => {
      const result = await getUsersForSelectAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

// --- DataTable hook ---

export function useUserReportingLineDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { role?: string; reportsToUserId?: number; isActive?: string },
) {
  return useQuery({
    queryKey: userReportingLineKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getUserReportingLinesAction(
        page,
        pageSize,
        search || undefined,
        customFilters?.role || undefined,
        customFilters?.reportsToUserId || undefined,
        customFilters?.isActive || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { userReportingLines, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: userReportingLines,
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

;(useUserReportingLineDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateUserReportingLine() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateUserReportingLineInput) => {
      const result = await createUserReportingLineAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userReportingLineKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Reporting line assigned successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'reporting line', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateUserReportingLine() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateUserReportingLineInput }) => {
      const result = await updateUserReportingLineAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userReportingLineKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Reporting line updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'reporting line', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeactivateUserReportingLine() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateUserReportingLineAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userReportingLineKeys.all })
      close()
      toast.success('Reporting line deactivated')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'reporting line', 'deactivate')
    },
  })
}

export function useActivateUserReportingLine() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateUserReportingLineAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userReportingLineKeys.all })
      close()
      toast.success('Reporting line activated')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'reporting line', 'activate')
    },
  })
}
