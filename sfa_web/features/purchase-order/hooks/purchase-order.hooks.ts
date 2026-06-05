'use client'

import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import {
  getPurchaseOrdersAction,
  getPurchaseOrderByIdAction,
  getPurchaseOrderStatsAction,
  createPurchaseOrderAction,
  updatePurchaseOrderAction,
  submitPurchaseOrderAction,
  repApprovePurchaseOrderAction,
  approvePurchaseOrderAction,
  rejectPurchaseOrderAction,
  acknowledgePurchaseOrderAction,
  finalizePurchaseOrderAction,
  cancelPurchaseOrderAction,
} from '../actions/purchase-order.actions'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../store'
import { usePurchaseOrderFilterStore } from '../store/purchase-order.filter-store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreatePurchaseOrderInput,
  UpdatePurchaseOrderInput,
  RejectPurchaseOrderInput,
} from '../schema/purchase-order.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const purchaseOrderKeys = {
  all: ['purchaseOrders'] as const,
  lists: () => [...purchaseOrderKeys.all, 'list'] as const,
  list: (filters: object) => [...purchaseOrderKeys.lists(), filters] as const,
  details: () => [...purchaseOrderKeys.all, 'detail'] as const,
  detail: (id: number) => [...purchaseOrderKeys.details(), id] as const,
  stats: ['purchaseOrders', 'stats'] as const,
}

// ── Query hooks ────────────────────────────────────────────────────────────

export function usePurchaseOrder(id: number) {
  return useQuery({
    queryKey: purchaseOrderKeys.detail(id),
    queryFn: async () => {
      const result = await getPurchaseOrderByIdAction(id)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id > 0,
  })
}

// ── Stats hook (aggregates counts by status for the list page KPI cards) ───

export function usePurchaseOrderStats(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: [...purchaseOrderKeys.stats, { fromDate, toDate }],
    queryFn: async () => {
      const result = await getPurchaseOrderStatsAction(fromDate, toDate)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 60 * 1000,
  })
}

// ── DataTable hook ─────────────────────────────────────────────────────────

export function usePurchaseOrderDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  const setFromDate = usePurchaseOrderFilterStore((s) => s.setFromDate)
  const setToDate = usePurchaseOrderFilterStore((s) => s.setToDate)

  useEffect(() => {
    setFromDate(dateRange?.from_date ?? '')
    setToDate(dateRange?.to_date ?? '')
  }, [dateRange?.from_date, dateRange?.to_date, setFromDate, setToDate])

  return useQuery({
    queryKey: purchaseOrderKeys.list({ page, pageSize, search, dateRange, customFilters }),
    queryFn: async () => {
      const result = await getPurchaseOrdersAction(
        page,
        pageSize,
        search || undefined,
        customFilters?.status || undefined,
        dateRange?.from_date || undefined,
        dateRange?.to_date || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { purchaseOrders, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: purchaseOrders,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
    staleTime: 30 * 1000,
  })
}

;(usePurchaseOrderDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Create / Update mutations ──────────────────────────────────────────────

export function useCreatePurchaseOrder() {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreatePurchaseOrderInput) => {
      const result = await createPurchaseOrderAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      setFieldErrors(null)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'purchase order', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdatePurchaseOrder(orderId: number) {
  const queryClient = useQueryClient()
  const router = useRouter()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: UpdatePurchaseOrderInput) => {
      const result = await updatePurchaseOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      setFieldErrors(null)
      toast.success('Purchase order updated successfully')
      router.push(`/purchase-orders/${orderId}`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'purchase order', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

// ── Workflow mutations (dialog-based) ─────────────────────────────────────

export function useSubmitPurchaseOrder() {
  const queryClient = useQueryClient()
  const { close } = useSubmitDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await submitPurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      close()
      toast.success('Order submitted for rep approval')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'submit')
    },
  })
}

export function useRepApprovePurchaseOrder() {
  const queryClient = useQueryClient()
  const { close } = useRepApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await repApprovePurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      close()
      toast.success('Order approved by rep')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'rep-approve')
    },
  })
}

export function useApprovePurchaseOrder() {
  const queryClient = useQueryClient()
  const { close } = useApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await approvePurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      close()
      toast.success('Order approved by manager')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'approve')
    },
  })
}

export function useAcknowledgePurchaseOrder() {
  const queryClient = useQueryClient()
  const { close } = useAcknowledgeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await acknowledgePurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      close()
      toast.success('Rejection acknowledged — order cancelled')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'acknowledge')
    },
  })
}

export function useFinalizePurchaseOrder() {
  const queryClient = useQueryClient()
  const { close } = useFinalizeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await finalizePurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      close()
      toast.success('Order finalized')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'finalize')
    },
  })
}

// ── Inline form mutations (reject / cancel) ────────────────────────────────

export function useRejectPurchaseOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectPurchaseOrderInput) => {
      const result = await rejectPurchaseOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      toast.success('Order rejected')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'reject')
    },
  })
}

export function useCancelPurchaseOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectPurchaseOrderInput) => {
      const result = await cancelPurchaseOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: purchaseOrderKeys.all })
      toast.success('Order cancelled')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'purchase order', 'cancel')
    },
  })
}
