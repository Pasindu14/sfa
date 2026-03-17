'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import {
  getSalesOrdersAction,
  getSalesOrderByIdAction,
  getDefaultPricingStructureAction,
  createSalesOrderAction,
  updateSalesOrderAction,
  submitSalesOrderAction,
  repApproveSalesOrderAction,
  approveSalesOrderAction,
  rejectSalesOrderAction,
  acknowledgeSalesOrderAction,
  finalizeSalesOrderAction,
  cancelSalesOrderAction,
} from '../actions/sales-order.actions'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreateSalesOrderInput,
  UpdateSalesOrderInput,
  RejectSalesOrderInput,
} from '../schema/sales-order.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const salesOrderKeys = {
  all: ['salesOrders'] as const,
  lists: () => [...salesOrderKeys.all, 'list'] as const,
  list: (filters: object) => [...salesOrderKeys.lists(), filters] as const,
  details: () => [...salesOrderKeys.all, 'detail'] as const,
  detail: (id: number) => [...salesOrderKeys.details(), id] as const,
  defaultPricing: ['defaultPricingStructure'] as const,
}

// ── Query hooks ────────────────────────────────────────────────────────────

export function useSalesOrder(id: number) {
  return useQuery({
    queryKey: salesOrderKeys.detail(id),
    queryFn: async () => {
      const result = await getSalesOrderByIdAction(id)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id > 0,
  })
}

export function useDefaultPricingStructure() {
  return useQuery({
    queryKey: salesOrderKeys.defaultPricing,
    queryFn: async () => {
      const result = await getDefaultPricingStructureAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

// ── DataTable hook ─────────────────────────────────────────────────────────

export function useSalesOrderDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  return useQuery({
    queryKey: salesOrderKeys.list({ page, pageSize, search, dateRange, customFilters }),
    queryFn: async () => {
      const result = await getSalesOrdersAction(
        page,
        pageSize,
        search || undefined,
        customFilters?.status || undefined,
        dateRange?.from_date || undefined,
        dateRange?.to_date || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { salesOrders, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: salesOrders,
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

;(useSalesOrderDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Create / Update mutations ──────────────────────────────────────────────

export function useCreateSalesOrder() {
  const queryClient = useQueryClient()
  const router = useRouter()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateSalesOrderInput) => {
      const result = await createSalesOrderAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      setFieldErrors(null)
      toast.success('Sales order created successfully')
      router.push(`/sales-orders/${data.id}`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'sales order', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateSalesOrder(orderId: number) {
  const queryClient = useQueryClient()
  const router = useRouter()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: UpdateSalesOrderInput) => {
      const result = await updateSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      setFieldErrors(null)
      toast.success('Sales order updated successfully')
      router.push(`/sales-orders/${orderId}`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'sales order', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

// ── Workflow mutations (dialog-based) ─────────────────────────────────────

export function useSubmitSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useSubmitDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await submitSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      close()
      toast.success('Order submitted for rep approval')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'submit')
    },
  })
}

export function useRepApproveSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useRepApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await repApproveSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      close()
      toast.success('Order approved by rep')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'rep-approve')
    },
  })
}

export function useApproveSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await approveSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      close()
      toast.success('Order approved by manager')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'approve')
    },
  })
}

export function useAcknowledgeSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useAcknowledgeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await acknowledgeSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      close()
      toast.success('Rejection acknowledged — order cancelled')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'acknowledge')
    },
  })
}

export function useFinalizeSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useFinalizeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await finalizeSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      close()
      toast.success('Order finalized')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'finalize')
    },
  })
}

// ── Inline form mutations (reject / cancel) ────────────────────────────────

export function useRejectSalesOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectSalesOrderInput) => {
      const result = await rejectSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      toast.success('Order rejected')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'reject')
    },
  })
}

export function useCancelSalesOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectSalesOrderInput) => {
      const result = await cancelSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      toast.success('Order cancelled')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'cancel')
    },
  })
}
