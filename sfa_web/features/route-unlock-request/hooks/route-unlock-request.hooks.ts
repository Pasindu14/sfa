'use client'

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getRouteUnlockRequestsAction,
  getRouteUnlockPendingCountAction,
  getRouteUnlockRequestAction,
  approveRouteUnlockAction,
  rejectRouteUnlockAction,
  revokeRouteUnlockAction,
} from '../actions/route-unlock-request.actions'
import { useApproveUnlockDialog, useRejectUnlockDialog, useRevokeUnlockDialog } from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  ApproveRouteUnlockInput,
  RouteUnlockListFilters,
  RouteUnlockReasonInput,
  RouteUnlockStatusValue,
} from '../schema/route-unlock-request.schema'
import { ActionError } from '@/lib/actions/action-error'

// ── Query key factory ──────────────────────────────────────────────────────

export const routeUnlockKeys = {
  all: ['routeUnlockRequests'] as const,
  lists: () => [...routeUnlockKeys.all, 'list'] as const,
  list: (filters: object) => [...routeUnlockKeys.lists(), filters] as const,
  detail: (id: number) => [...routeUnlockKeys.all, 'detail', id] as const,
  pendingCount: () => [...routeUnlockKeys.all, 'pendingCount'] as const,
}

// A pending request is time-critical (the rep is standing at an outlet), so the
// pending views poll instead of waiting for a manual reload.
const PENDING_REFETCH_MS = 60_000

// Codes that mean "the row moved on under you" — the visible data is stale.
const STALE_ROW_CODES = new Set([
  'CONCURRENCY_CONFLICT',
  'ROUTE_UNLOCK_INVALID_STATE',
  'ROUTE_UNLOCK_EXPIRED',
  'ROUTE_UNLOCK_ASSIGNMENT_CHANGED',
])

// ── DataTable hooks ────────────────────────────────────────────────────────

function useRouteUnlockListQuery(
  page: number,
  pageSize: number,
  search: string,
  filters: RouteUnlockListFilters,
  refetchInterval?: number,
) {
  return useQuery({
    queryKey: routeUnlockKeys.list({ page, pageSize, search, ...filters }),
    queryFn: async () => {
      const result = await getRouteUnlockRequestsAction(
        page,
        pageSize,
        search || undefined,
        filters,
      )
      if (!result.success) throw new ActionError(result)
      const { items, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: items,
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
    refetchInterval,
  })
}

/** Pending tab — today's pending requests only, polled. */
export function usePendingRouteUnlockDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: Record<string, unknown>,
) {
  return useRouteUnlockListQuery(
    page,
    pageSize,
    search,
    { status: 'Pending' },
    PENDING_REFETCH_MS,
  )
}

; (usePendingRouteUnlockDataTable as unknown as Record<string, unknown>).isQueryHook = true

/** All tab — status comes from the custom filter, from/to from the date filter. */
export function useRouteUnlockDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  return useRouteUnlockListQuery(page, pageSize, search, {
    status: (customFilters?.status || undefined) as RouteUnlockStatusValue | undefined,
    from: dateRange?.from_date || undefined,
    to: dateRange?.to_date || undefined,
  })
}

; (useRouteUnlockDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Queries ────────────────────────────────────────────────────────────────

export function useRouteUnlockPendingCount() {
  return useQuery({
    queryKey: routeUnlockKeys.pendingCount(),
    queryFn: async () => {
      const result = await getRouteUnlockPendingCountAction()
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    staleTime: 30 * 1000,
    refetchInterval: PENDING_REFETCH_MS,
  })
}

export function useRouteUnlockRequest(id: number | null) {
  return useQuery({
    queryKey: routeUnlockKeys.detail(id ?? 0),
    queryFn: async () => {
      const result = await getRouteUnlockRequestAction(id!)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    enabled: id !== null,
    staleTime: 30 * 1000,
  })
}

// ── Mutation hooks ─────────────────────────────────────────────────────────

function useStaleRowRecovery() {
  const queryClient = useQueryClient()
  return (error: ActionFailure) => {
    if (error.code && STALE_ROW_CODES.has(error.code)) {
      queryClient.invalidateQueries({ queryKey: routeUnlockKeys.all })
    }
  }
}

export function useApproveRouteUnlock() {
  const queryClient = useQueryClient()
  const recover = useStaleRowRecovery()
  const { close } = useApproveUnlockDialog()

  return useMutation({
    mutationFn: async ({
      id,
      rowVersion,
      data,
    }: {
      id: number
      rowVersion: number
      data: ApproveRouteUnlockInput
    }) => {
      const result = await approveRouteUnlockAction(id, rowVersion, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeUnlockKeys.all })
      close()
      toast.success('Unlock approved — the rep can reach every outlet on the route today')
    },
    onError: (error: ActionFailure) => {
      recover(error)
      handleErrorToast(error, 'unlock request', 'approve')
    },
  })
}

export function useRejectRouteUnlock() {
  const queryClient = useQueryClient()
  const recover = useStaleRowRecovery()
  const { close } = useRejectUnlockDialog()

  return useMutation({
    mutationFn: async ({
      id,
      rowVersion,
      data,
    }: {
      id: number
      rowVersion: number
      data: RouteUnlockReasonInput
    }) => {
      const result = await rejectRouteUnlockAction(id, rowVersion, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeUnlockKeys.all })
      close()
      toast.success('Unlock request rejected — the rep will be notified')
    },
    onError: (error: ActionFailure) => {
      recover(error)
      handleErrorToast(error, 'unlock request', 'reject')
    },
  })
}

export function useRevokeRouteUnlock() {
  const queryClient = useQueryClient()
  const recover = useStaleRowRecovery()
  const { close } = useRevokeUnlockDialog()

  return useMutation({
    mutationFn: async ({
      id,
      rowVersion,
      data,
    }: {
      id: number
      rowVersion: number
      data: RouteUnlockReasonInput
    }) => {
      const result = await revokeRouteUnlockAction(id, rowVersion, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: routeUnlockKeys.all })
      close()
      toast.success('Unlock revoked — the geofence applies again on the rep\'s next sync')
    },
    onError: (error: ActionFailure) => {
      recover(error)
      handleErrorToast(error, 'unlock request', 'revoke')
    },
  })
}
