'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getTerritoriesAction,
  getTerritoryByIdAction,
  createTerritoryAction,
  updateTerritoryAction,
  deleteTerritoryAction,
  activateTerritoryAction,
  deactivateTerritoryAction,
} from '../actions/territory.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateTerritoryInput, UpdateTerritoryInput } from '../schema/territory.schema'

// --- Query key factory ---

export const territoryKeys = {
  all: ['territories'] as const,
  lists: () => [...territoryKeys.all, 'list'] as const,
  list: (filters: object) => [...territoryKeys.lists(), filters] as const,
  details: () => [...territoryKeys.all, 'detail'] as const,
  detail: (id: number) => [...territoryKeys.details(), id] as const,
}

// --- Query options factory ---

export function territoryQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: territoryKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getTerritoriesAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useTerritories(page: number, pageSize: number) {
  return useQuery(territoryQueryOptions(page, pageSize))
}

export function useTerritory(id: number | null) {
  return useQuery({
    queryKey: territoryKeys.detail(id!),
    queryFn: async () => {
      const result = await getTerritoryByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useTerritoryDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: territoryKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getTerritoriesAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { territories, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: territories,
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

;(useTerritoryDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateTerritory() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateTerritoryInput) => {
      const result = await createTerritoryAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: territoryKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Territory created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'territory', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateTerritory() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateTerritoryInput }) => {
      const result = await updateTerritoryAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: territoryKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Territory updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'territory', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteTerritory() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteTerritoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: territoryKeys.all })
      close()
      toast.success('Territory deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'territory', 'delete')
    },
  })
}

export function useActivateTerritory() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateTerritoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: territoryKeys.all })
      close()
      toast.success('Territory activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'territory', 'activate')
    },
  })
}

export function useDeactivateTerritory() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateTerritoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: territoryKeys.all })
      close()
      toast.success('Territory deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'territory', 'deactivate')
    },
  })
}
