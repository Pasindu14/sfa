'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getGrnsAction, getGrnByIdAction, createGrnAction, confirmGrnAction, deleteGrnAction } from '../actions/grn.actions'
import { useConfirmDialog, useDeleteDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateGrnInput, ConfirmGrnInput } from '../schema/grn.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const grnKeys = {
  all: ['grns'] as const,
  lists: () => [...grnKeys.all, 'list'] as const,
  list: (filters: object) => [...grnKeys.lists(), filters] as const,
  details: () => [...grnKeys.all, 'detail'] as const,
  detail: (id: number) => [...grnKeys.details(), id] as const,
}

// ── List query hook ────────────────────────────────────────────────────────

export function useGrns(
  page: number,
  pageSize: number,
  status?: string,
  distributorId?: number,
) {
  return useQuery({
    queryKey: grnKeys.list({ page, pageSize, status, distributorId }),
    queryFn: async () => {
      const result = await getGrnsAction(page, pageSize, status || undefined, distributorId || undefined)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    placeholderData: keepPreviousData,
  })
}

// ── Detail query hook ──────────────────────────────────────────────────────

export function useGrn(id: number | null) {
  return useQuery({
    queryKey: grnKeys.detail(id!),
    queryFn: async () => {
      const result = await getGrnByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// ── Create GRN mutation ────────────────────────────────────────────────────

export function useCreateGrn() {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateGrnInput) => {
      const result = await createGrnAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grnKeys.all })
      setFieldErrors(null)
      toast.success('GRN created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'GRN', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

// ── Delete GRN mutation ────────────────────────────────────────────────────

export function useDeleteGrn() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteGrnAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grnKeys.all })
      close()
      toast.success('GRN deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'GRN', 'delete')
    },
  })
}

// ── Confirm GRN mutation ───────────────────────────────────────────────────

export function useConfirmGrn() {
  const queryClient = useQueryClient()
  const { close } = useConfirmDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: ConfirmGrnInput }) => {
      const result = await confirmGrnAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: grnKeys.all })
      setFieldErrors(null)
      close()
      toast.success('GRN confirmed successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'GRN', 'confirm')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
