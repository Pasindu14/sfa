'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getRoutesAction,
  getRouteByIdAction,
  createRouteAction,
  updateRouteAction,
  deleteRouteAction,
} from '../actions/route.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateRouteInput, UpdateRouteInput } from '../schema/route.schema'

// --- Query key factory ---

export const routeKeys = {
  all: ['routes'] as const,
  lists: () => [...routeKeys.all, 'list'] as const,
  list: (filters: object) => [...routeKeys.lists(), filters] as const,
  details: () => [...routeKeys.all, 'detail'] as const,
  detail: (id: number) => [...routeKeys.details(), id] as const,
}

// --- Query options factory ---

export function routeQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: routeKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getRoutesAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useRoutes(page: number, pageSize: number) {
  return useQuery(routeQueryOptions(page, pageSize))
}

export function useRoute(id: number | null) {
  return useQuery({
    queryKey: routeKeys.detail(id!),
    queryFn: async () => {
      const result = await getRouteByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---
// Uses server-side pagination + search — the API does all filtering.

export function useRouteDataTable(
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
    queryKey: routeKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getRoutesAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { routes, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: routes,
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

;(useRouteDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreateRoute() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateRouteInput) => {
      const result = await createRouteAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Route created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'route', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateRoute() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateRouteInput }) => {
      const result = await updateRouteAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Route updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'route', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteRoute() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteRouteAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeKeys.all })
      close()
      toast.success('Route deleted successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'route', 'delete')
    },
  })
}
