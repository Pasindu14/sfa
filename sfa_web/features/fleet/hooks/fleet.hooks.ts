'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getFleetsAction,
  getFleetByIdAction,
  createFleetAction,
  updateFleetAction,
  activateFleetAction,
  deactivateFleetAction,
} from '../actions/fleet.actions'
import {
  useCreateDialog,
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateFleetInput, UpdateFleetInput } from '../schema/fleet.schema'

// --- Query key factory ---

export const fleetKeys = {
  all: ['fleets'] as const,
  lists: () => [...fleetKeys.all, 'list'] as const,
  list: (filters: object) => [...fleetKeys.lists(), filters] as const,
  details: () => [...fleetKeys.all, 'detail'] as const,
  detail: (id: number) => [...fleetKeys.details(), id] as const,
}

// --- Query hooks ---

export function useFleet(id: number | null) {
  return useQuery({
    queryKey: fleetKeys.detail(id!),
    queryFn: async () => {
      const result = await getFleetByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---

export function useFleetDataTable(
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
    queryKey: fleetKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getFleetsAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { fleets, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: fleets,
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

;(useFleetDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateFleet() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateFleetInput) => {
      const result = await createFleetAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fleetKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Fleet created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'fleet', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateFleet() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateFleetInput }) => {
      const result = await updateFleetAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fleetKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Fleet updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'fleet', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateFleet() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateFleetAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fleetKeys.all })
      close()
      toast.success('Fleet activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'fleet', 'activate')
    },
  })
}

export function useDeactivateFleet() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateFleetAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fleetKeys.all })
      close()
      toast.success('Fleet deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'fleet', 'deactivate')
    },
  })
}
