'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getRegionsAction,
  getRegionByIdAction,
  createRegionAction,
  updateRegionAction,
  activateRegionAction,
  deactivateRegionAction,
  getActiveRegionsAction,
} from '../actions/region.actions'
import {
  useCreateDialog,
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateRegionInput, UpdateRegionInput } from '../schema/region.schema'

// --- Query key factory ---

export const regionKeys = {
  all: ['regions'] as const,
  lists: () => [...regionKeys.all, 'list'] as const,
  list: (filters: object) => [...regionKeys.lists(), filters] as const,
  details: () => [...regionKeys.all, 'detail'] as const,
  detail: (id: number) => [...regionKeys.details(), id] as const,
}

// --- Query options factory ---

export function regionQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: regionKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getRegionsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useRegions(page: number, pageSize: number) {
  return useQuery(regionQueryOptions(page, pageSize))
}

export function useRegion(id: number | null) {
  return useQuery({
    queryKey: regionKeys.detail(id!),
    queryFn: async () => {
      const result = await getRegionByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- Active regions hook (for dropdowns/selects) ---

export function useActiveRegions() {
  return useQuery({
    queryKey: [...regionKeys.all, 'active'] as const,
    queryFn: async () => {
      const result = await getActiveRegionsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useRegionDataTable(
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
    queryKey: regionKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      // Fetch all regions so client-side search works across the full dataset
      const result = await getRegionsAction(1, 1000)
      if (!result.success) throw new Error(result.error)
      const { regions } = result.data

      const term = search.trim().toLowerCase()
      const filtered = term
        ? regions.filter((r) => r.name.toLowerCase().includes(term))
        : regions

      filtered.sort((a, b) => a.name.localeCompare(b.name))

      const start = (page - 1) * pageSize
      const paginated = filtered.slice(start, start + pageSize)

      return {
        success: true as const,
        data: paginated,
        pagination: {
          page,
          limit: pageSize,
          total_pages: Math.ceil(filtered.length / pageSize),
          total_items: filtered.length,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(useRegionDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreateRegion() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateRegionInput) => {
      const result = await createRegionAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: regionKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Region created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'region', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateRegion() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateRegionInput }) => {
      const result = await updateRegionAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: regionKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Region updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'region', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateRegion() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateRegionAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: regionKeys.all })
      close()
      toast.success('Region activated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'region', 'activate')
    },
  })
}

export function useDeactivateRegion() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateRegionAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: regionKeys.all })
      close()
      toast.success('Region deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'region', 'deactivate')
    },
  })
}
