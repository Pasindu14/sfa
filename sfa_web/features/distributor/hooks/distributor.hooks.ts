'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getDistributorsAction,
  getDistributorByIdAction,
  createDistributorAction,
  updateDistributorAction,
  deleteDistributorAction,
  activateDistributorAction,
  deactivateDistributorAction,
} from '../actions/distributor.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateDistributorInput, UpdateDistributorInput } from '../schema/distributor.schema'

// --- Query key factory ---

export const distributorKeys = {
  all: ['distributors'] as const,
  lists: () => [...distributorKeys.all, 'list'] as const,
  list: (filters: object) => [...distributorKeys.lists(), filters] as const,
  details: () => [...distributorKeys.all, 'detail'] as const,
  detail: (id: number) => [...distributorKeys.details(), id] as const,
}

// --- Query options factory ---

export function distributorQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: distributorKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getDistributorsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useDistributors(page: number, pageSize: number) {
  return useQuery(distributorQueryOptions(page, pageSize))
}

export function useDistributor(id: number | null) {
  return useQuery({
    queryKey: distributorKeys.detail(id!),
    queryFn: async () => {
      const result = await getDistributorByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---

export function useDistributorDataTable(
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
    queryKey: distributorKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getDistributorsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      const { distributors, page: p, pageSize: ps, totalCount } = result.data

      // Client-side search filtering
      const term = search.trim().toLowerCase()
      const filtered = term
        ? distributors.filter((item) =>
            item.name.toLowerCase().includes(term) ||
            item.email.toLowerCase().includes(term) ||
            item.phone.toLowerCase().includes(term) ||
            item.alias.toString().includes(term)
          )
        : distributors

      // Apply custom status filter if provided
      let finalData = filtered
      if (customFilters?.status) {
        const statusValue = customFilters.status === 'active'
        finalData = finalData.filter((item) => item.isActive === statusValue)
      }

      return {
        success: true as const,
        data: finalData,
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

;(useDistributorDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreateDistributor() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateDistributorInput) => {
      const result = await createDistributorAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Distributor created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'distributor', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateDistributor() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateDistributorInput }) => {
      const result = await updateDistributorAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Distributor updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'distributor', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteDistributor() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteDistributorAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorKeys.all })
      close()
      toast.success('Distributor deleted successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'distributor', 'delete')
    },
  })
}

export function useActivateDistributor() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateDistributorAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorKeys.all })
      close()
      toast.success('Distributor activated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'distributor', 'activate')
    },
  })
}

export function useDeactivateDistributor() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateDistributorAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: distributorKeys.all })
      close()
      toast.success('Distributor deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'distributor', 'deactivate')
    },
  })
}
