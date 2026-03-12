'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getOutletsAction,
  getOutletByIdAction,
  getActiveOutletsAction,
  getOutletMapPointsAction,
  createOutletAction,
  updateOutletAction,
  deleteOutletAction,
  activateOutletAction,
  deactivateOutletAction,
} from '../actions/outlet.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateOutletInput, UpdateOutletInput } from '../schema/outlet.schema'

// --- Query key factory ---

export const outletKeys = {
  all: ['outlets'] as const,
  lists: () => [...outletKeys.all, 'list'] as const,
  list: (filters: object) => [...outletKeys.lists(), filters] as const,
  details: () => [...outletKeys.all, 'detail'] as const,
  detail: (id: number) => [...outletKeys.details(), id] as const,
  active: () => [...outletKeys.all, 'active'] as const,
}

// --- Query options factory ---

export function outletQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: outletKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getOutletsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useOutlets(page: number, pageSize: number) {
  return useQuery(outletQueryOptions(page, pageSize))
}

export function useOutlet(id: number | null) {
  return useQuery({
    queryKey: outletKeys.detail(id!),
    queryFn: async () => {
      const result = await getOutletByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function useActiveOutlets() {
  return useQuery({
    queryKey: outletKeys.active(),
    queryFn: async () => {
      const result = await getActiveOutletsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

export function useOutletsForMap() {
  return useQuery({
    queryKey: [...outletKeys.all, 'map'] as const,
    queryFn: async () => {
      const result = await getOutletMapPointsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- DataTable hook ---

export function useOutletDataTable(
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
    queryKey: outletKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const status = customFilters?.status as string | undefined
      const result = await getOutletsAction(page, pageSize, search || undefined, status || undefined)
      if (!result.success) throw new Error(result.error)
      const { outlets, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: outlets,
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

;(useOutletDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateOutlet() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateOutletInput) => {
      const result = await createOutletAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: outletKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Outlet created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'outlet', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateOutlet() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateOutletInput }) => {
      const result = await updateOutletAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: outletKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Outlet updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'outlet', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteOutlet() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteOutletAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: outletKeys.all })
      close()
      toast.success('Outlet deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'outlet', 'delete')
    },
  })
}

export function useActivateOutlet() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateOutletAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: outletKeys.all })
      close()
      toast.success('Outlet activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'outlet', 'activate')
    },
  })
}

export function useDeactivateOutlet() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateOutletAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: outletKeys.all })
      close()
      toast.success('Outlet deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'outlet', 'deactivate')
    },
  })
}
