'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPricingStructuresAction,
  getPricingStructureByIdAction,
  createPricingStructureAction,
  updatePricingStructureAction,
  deletePricingStructureAction,
  activatePricingStructureAction,
  getPricingStructureItemsAction,
  bulkUpdatePricingStructureItemsAction,
} from '../actions/pricing-structure.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useManageItemsDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type {
  CreatePricingStructureInput,
  UpdatePricingStructureInput,
  BulkUpdateItemsInput,
} from '../schema/pricing-structure.schema'

// --- Query key factory ---

export const pricingStructureKeys = {
  all: ['pricing-structures'] as const,
  lists: () => [...pricingStructureKeys.all, 'list'] as const,
  list: (filters: object) => [...pricingStructureKeys.lists(), filters] as const,
  details: () => [...pricingStructureKeys.all, 'detail'] as const,
  detail: (id: number) => [...pricingStructureKeys.details(), id] as const,
  items: (id: number) => [...pricingStructureKeys.all, 'items', id] as const,
}

// --- Query options factory ---

export function pricingStructureQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: pricingStructureKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getPricingStructuresAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function usePricingStructures(page: number, pageSize: number) {
  return useQuery(pricingStructureQueryOptions(page, pageSize))
}

export function usePricingStructure(id: number | null) {
  return useQuery({
    queryKey: pricingStructureKeys.detail(id!),
    queryFn: async () => {
      const result = await getPricingStructureByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function usePricingStructureItems(id: number | null) {
  return useQuery({
    queryKey: pricingStructureKeys.items(id!),
    queryFn: async () => {
      const result = await getPricingStructureItemsAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---

export function usePricingStructureDataTable(
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
    queryKey: pricingStructureKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getPricingStructuresAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { pricingStructures, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: pricingStructures,
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

;(usePricingStructureDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreatePricingStructureInput) => {
      const result = await createPricingStructureAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Pricing structure created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'pricing structure', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdatePricingStructureInput }) => {
      const result = await updatePricingStructureAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Pricing structure updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'pricing structure', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeletePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deletePricingStructureAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.all })
      close()
      toast.success('Pricing structure deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'pricing structure', 'delete')
    },
  })
}

export function useActivatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activatePricingStructureAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.all })
      close()
      toast.success('Pricing structure activated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'pricing structure', 'activate')
    },
  })
}

export function useBulkUpdatePricingStructureItems() {
  const queryClient = useQueryClient()
  const { close } = useManageItemsDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: BulkUpdateItemsInput }) => {
      const result = await bulkUpdatePricingStructureItemsAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.all })
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.items(id) })
      setFieldErrors(null)
      close()
      toast.success('Pricing structure items updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'pricing structure items', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
