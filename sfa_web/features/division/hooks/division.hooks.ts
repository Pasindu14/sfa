'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getDivisionsAction,
  getDivisionByIdAction,
  createDivisionAction,
  updateDivisionAction,
  activateDivisionAction,
  deactivateDivisionAction,
  getActiveDivisionsAction,
} from '../actions/division.actions'
import {
  useCreateDialog,
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateDivisionInput, UpdateDivisionInput } from '../schema/division.schema'

// --- Query key factory ---

export const divisionKeys = {
  all: ['divisions'] as const,
  lists: () => [...divisionKeys.all, 'list'] as const,
  list: (filters: object) => [...divisionKeys.lists(), filters] as const,
  details: () => [...divisionKeys.all, 'detail'] as const,
  detail: (id: number) => [...divisionKeys.details(), id] as const,
}

// --- Query options factory ---

export function divisionQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: divisionKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getDivisionsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useDivisions(page: number, pageSize: number) {
  return useQuery(divisionQueryOptions(page, pageSize))
}

export function useDivision(id: number | null) {
  return useQuery({
    queryKey: divisionKeys.detail(id!),
    queryFn: async () => {
      const result = await getDivisionByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- Active divisions hook (for dropdowns/selects) ---

export function useActiveDivisions() {
  return useQuery({
    queryKey: [...divisionKeys.all, 'active'] as const,
    queryFn: async () => {
      const result = await getActiveDivisionsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useDivisionDataTable(
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
    queryKey: divisionKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      // Fetch all divisions so client-side search works across the full dataset
      const result = await getDivisionsAction(1, 1000)
      if (!result.success) throw new Error(result.error)
      const { divisions, totalCount } = result.data

      const term = search.trim().toLowerCase()
      const filtered = term
        ? divisions.filter(
            (d) =>
              d.name.toLowerCase().includes(term) ||
              d.territoryName.toLowerCase().includes(term) ||
              d.areaName.toLowerCase().includes(term) ||
              d.regionName.toLowerCase().includes(term),
          )
        : divisions

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

;(useDivisionDataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreateDivision() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateDivisionInput) => {
      const result = await createDivisionAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: divisionKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Division created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'division', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateDivision() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateDivisionInput }) => {
      const result = await updateDivisionAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: divisionKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Division updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'division', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateDivision() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateDivisionAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: divisionKeys.all })
      close()
      toast.success('Division activated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'division', 'activate')
    },
  })
}

export function useDeactivateDivision() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateDivisionAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: divisionKeys.all })
      close()
      toast.success('Division deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'division', 'deactivate')
    },
  })
}
