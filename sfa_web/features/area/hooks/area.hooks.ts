'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getAreasAction,
  getAreaByIdAction,
  createAreaAction,
  updateAreaAction,
  activateAreaAction,
  deactivateAreaAction,
} from '../actions/area.actions'
import {
  useCreateDialog,
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateAreaInput, UpdateAreaInput } from '../schema/area.schema'

// --- Query key factory ---

export const areaKeys = {
  all: ['areas'] as const,
  lists: () => [...areaKeys.all, 'list'] as const,
  list: (filters: object) => [...areaKeys.lists(), filters] as const,
  details: () => [...areaKeys.all, 'detail'] as const,
  detail: (id: number) => [...areaKeys.details(), id] as const,
}

// --- Query options factory ---

export function areaQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: areaKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getAreasAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useAreas(page: number, pageSize: number) {
  return useQuery(areaQueryOptions(page, pageSize))
}

export function useArea(id: number | null) {
  return useQuery({
    queryKey: areaKeys.detail(id!),
    queryFn: async () => {
      const result = await getAreaByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useAreaDataTable(
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
    queryKey: areaKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getAreasAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      const { areas, page: p, pageSize: ps, totalCount } = result.data

      const term = search.trim().toLowerCase()
      const filtered = term
        ? areas.filter(
            (a) =>
              a.name.toLowerCase().includes(term) ||
              a.regionName.toLowerCase().includes(term),
          )
        : areas

      return {
        success: true as const,
        data: filtered,
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

;(useAreaDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreateArea() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateAreaInput) => {
      const result = await createAreaAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: areaKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Area created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'area', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateArea() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateAreaInput }) => {
      const result = await updateAreaAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: areaKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Area updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'area', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateArea() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateAreaAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: areaKeys.all })
      close()
      toast.success('Area activated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'area', 'activate')
    },
  })
}

export function useDeactivateArea() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateAreaAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: areaKeys.all })
      close()
      toast.success('Area deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'area', 'deactivate')
    },
  })
}
