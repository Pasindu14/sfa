'use client'

import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getMyPurchaseOrdersAction,
  getMyPurchaseOrderAction,
  getMyPurchaseOrderStatsAction,
  getMyDistributorProfileAction,
  getMyProductCategoryPricingsAction,
  createMyPurchaseOrderAction,
  updateMyPurchaseOrderAction,
  submitMyPurchaseOrderAction,
  acknowledgeMyPurchaseOrderAction,
  finalizeMyPurchaseOrderAction,
  cancelMyPurchaseOrderAction,
} from '../actions/distributor-purchase-order.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type {
  CreateMyPurchaseOrderInput,
  UpdateMyPurchaseOrderInput,
  CancelMyPurchaseOrderInput,
  MyPurchaseOrderSummaryDto,
} from '../schema/distributor-purchase-order.schema'
import type { ActionFailure } from '@/lib/types/actions'

// ── Query key factory ─────────────────────────────────────────────────────

export const myPurchaseOrderKeys = {
  all: ['my-purchase-orders'] as const,
  list: (params: object) => [...myPurchaseOrderKeys.all, 'list', params] as const,
  detail: (id: number) => [...myPurchaseOrderKeys.all, 'detail', id] as const,
  stats: ['my-purchase-orders', 'stats'] as const,
  profile: ['my-distributor-profile'] as const,
  pricing: ['my-distributor-pricing'] as const,
}

// ── DataTable hook ────────────────────────────────────────────────────────

export function useMyPurchaseOrdersDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  const status = customFilters?.status
  const dateFrom = dateRange?.from_date
  const dateTo = dateRange?.to_date

  return useQuery({
    queryKey: myPurchaseOrderKeys.list({ page, pageSize, search, status, dateFrom, dateTo }),
    queryFn: async () => {
      const result = await getMyPurchaseOrdersAction(
        page, pageSize,
        search || undefined,
        status || undefined,
        dateFrom || undefined,
        dateTo || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { purchaseOrders, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: purchaseOrders as MyPurchaseOrderSummaryDto[],
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
  })
}

;(useMyPurchaseOrdersDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Detail ────────────────────────────────────────────────────────────────

export function useMyPurchaseOrder(id: number | null) {
  return useQuery({
    queryKey: myPurchaseOrderKeys.detail(id!),
    queryFn: async () => {
      const result = await getMyPurchaseOrderAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// ── Stats ─────────────────────────────────────────────────────────────────

export function useMyPurchaseOrderStats(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: [...myPurchaseOrderKeys.stats, { fromDate, toDate }],
    queryFn: async () => {
      const result = await getMyPurchaseOrderStatsAction(fromDate, toDate)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 60_000,
  })
}

// ── Profile + Pricing ─────────────────────────────────────────────────────

export function useMyDistributorProfile() {
  return useQuery({
    queryKey: myPurchaseOrderKeys.profile,
    queryFn: async () => {
      const result = await getMyDistributorProfileAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60_000,
  })
}

export function useMyProductCategoryPricings() {
  return useQuery({
    queryKey: myPurchaseOrderKeys.pricing,
    queryFn: async () => {
      const result = await getMyProductCategoryPricingsAction()
      if (!result.success) throw new Error(result.error)
      return result.data ?? []
    },
    staleTime: 5 * 60_000,
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────

export function useCreateMyPurchaseOrder() {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateMyPurchaseOrderInput) => {
      const result = await createMyPurchaseOrderAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      setFieldErrors(null)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'Purchase Order', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateMyPurchaseOrder(id: number, onSuccess?: () => void) {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: UpdateMyPurchaseOrderInput) => {
      const result = await updateMyPurchaseOrderAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      setFieldErrors(null)
      toast.success('Order updated successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'Purchase Order', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

// These hooks accept the ID at mutate() call time — used by both the create page chain and detail page actions.
export function useSubmitMyPurchaseOrder(onSuccess?: (id: number) => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await submitMyPurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      toast.success('Order submitted for approval')
      onSuccess?.(data.id)
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'Purchase Order', 'submit')
    },
  })
}

export function useFinalizeMyPurchaseOrder(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await finalizeMyPurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      toast.success('Order finalized successfully')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'Purchase Order', 'finalize')
    },
  })
}

export function useAcknowledgeMyPurchaseOrder(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await acknowledgeMyPurchaseOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      toast.success('Rejection acknowledged. Order cancelled.')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'Purchase Order', 'acknowledge')
    },
  })
}

export function useCancelMyPurchaseOrder(orderId: number, onSuccess?: () => void) {
  const queryClient = useQueryClient()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CancelMyPurchaseOrderInput) => {
      const result = await cancelMyPurchaseOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myPurchaseOrderKeys.all })
      setFieldErrors(null)
      toast.success('Order cancelled')
      onSuccess?.()
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'Purchase Order', 'cancel')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
