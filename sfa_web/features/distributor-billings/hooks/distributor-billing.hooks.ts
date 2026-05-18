'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getMyBillingsAction,
  getMyBillingDetailAction,
  approveBillingAction,
  rejectBillingAction,
} from '../actions/distributor-billing.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { DistributorBillingListItem } from '../schema/distributor-billing.schema'

function toLocalDateStr(d: Date) {
  return [
    d.getFullYear(),
    String(d.getMonth() + 1).padStart(2, '0'),
    String(d.getDate()).padStart(2, '0'),
  ].join('-')
}

export const myBillingKeys = {
  all: ['my-billings'] as const,
  list: (params: object) => [...myBillingKeys.all, 'list', params] as const,
  detail: (id: number) => [...myBillingKeys.all, 'detail', id] as const,
}

export function useMyBillingsDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: unknown,
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { repStatus?: string; distributorStatus?: string; dateFrom?: string; dateTo?: string },
) {
  const repStatus = customFilters?.repStatus
  const distributorStatus = customFilters?.distributorStatus
  const dateFrom = customFilters?.dateFrom
  const dateTo = customFilters?.dateTo

  return useQuery({
    queryKey: myBillingKeys.list({ page, pageSize, search, repStatus, distributorStatus, dateFrom, dateTo }),
    queryFn: async () => {
      const result = await getMyBillingsAction(
        page, pageSize, search || undefined,
        repStatus || undefined, distributorStatus || undefined,
        dateFrom || undefined, dateTo || undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { billings, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: billings as DistributorBillingListItem[],
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

;(useMyBillingsDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useMyBillingsTodaySummary() {
  const today = toLocalDateStr(new Date())
  return useQuery({
    queryKey: myBillingKeys.list({ page: 1, pageSize: 500, dateFrom: today, dateTo: today, _summary: true }),
    queryFn: async () => {
      const result = await getMyBillingsAction(1, 500, undefined, undefined, undefined, today, today)
      if (!result.success) throw new Error(result.error)
      const bills = result.data.billings
      return {
        totalRevenue: bills.reduce((s, b) => s + b.totalAmount, 0),
        totalCount: result.data.totalCount,
        approvedRevenue: bills.filter(b => b.distributorStatus === 'Approved').reduce((s, b) => s + b.totalAmount, 0),
        approvedCount: bills.filter(b => b.distributorStatus === 'Approved').length,
        pendingCount: bills.filter(b => b.distributorStatus === 'Pending').length,
      }
    },
    staleTime: 60_000,
  })
}

export function useMyBillingDetail(id: number | null) {
  return useQuery({
    queryKey: myBillingKeys.detail(id!),
    queryFn: async () => {
      const result = await getMyBillingDetailAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function useApproveBilling(onSuccess?: () => void) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: number) => {
      const result = await approveBillingAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myBillingKeys.all })
      toast.success('Billing approved successfully')
      onSuccess?.()
    },
    onError: (error: any) => {
      handleErrorToast(error, 'billing', 'approve')
    },
  })
}

export function useRejectBilling(onSuccess?: () => void) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, reason }: { id: number; reason?: string }) => {
      const result = await rejectBillingAction(id, reason)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myBillingKeys.all })
      toast.success('Billing rejected')
      onSuccess?.()
    },
    onError: (error: any) => {
      handleErrorToast(error, 'billing', 'reject')
    },
  })
}
