'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  ApproveRouteUnlockInput,
  RouteUnlockListDto,
  RouteUnlockListFilters,
  RouteUnlockReasonInput,
  RouteUnlockRequestDetailDto,
  RouteUnlockRequestDto,
} from '../schema/route-unlock-request.schema'

// Every action is Admin-gated here as well as on the API endpoint. Supervisors
// review from mobile; the web page is the admin fallback.

// ── Read ───────────────────────────────────────────────────────────────────

/// Paged list. `status` takes an effectiveStatus value; `from`/`to` are yyyy-MM-dd
/// business dates. Totals come from the `pagination` envelope, not the data array.
export const getRouteUnlockRequestsAction = createAction(
  { name: 'getRouteUnlockRequestsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    filters: RouteUnlockListFilters = {},
  ): Promise<RouteUnlockListDto> => {
    const res = await client.get('/api/v1/route-unlock-requests', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: filters.status || undefined,
        from: filters.from || undefined,
        to: filters.to || undefined,
      },
    })
    return {
      items: (res.data.data ?? []) as RouteUnlockRequestDto[],
      totalCount: (res.data.pagination?.total ?? 0) as number,
      page: (res.data.pagination?.page ?? page) as number,
      pageSize: (res.data.pagination?.pageSize ?? pageSize) as number,
    }
  }
)

export const getRouteUnlockPendingCountAction = createAction(
  { name: 'getRouteUnlockPendingCountAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/route-unlock-requests/pending-count')
    return (res.data.data?.count ?? 0) as number
  }
)

export const getRouteUnlockRequestAction = createAction(
  { name: 'getRouteUnlockRequestAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/route-unlock-requests/${id}`)
    return res.data.data as RouteUnlockRequestDetailDto
  }
)

// ── Workflow actions ───────────────────────────────────────────────────────

export const approveRouteUnlockAction = createAction(
  { name: 'approveRouteUnlockAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, rowVersion: number, data: ApproveRouteUnlockInput) => {
    const res = await client.post(`/api/v1/route-unlock-requests/${id}/approve`, {
      rowVersion,
      note: data.note?.trim() || undefined,
    })
    revalidatePath('/route-unlock-requests')
    return res.data.data as RouteUnlockRequestDto
  }
)

export const rejectRouteUnlockAction = createAction(
  { name: 'rejectRouteUnlockAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, rowVersion: number, data: RouteUnlockReasonInput) => {
    const res = await client.post(`/api/v1/route-unlock-requests/${id}/reject`, {
      rowVersion,
      reason: data.reason,
    })
    revalidatePath('/route-unlock-requests')
    return res.data.data as RouteUnlockRequestDto
  }
)

export const revokeRouteUnlockAction = createAction(
  { name: 'revokeRouteUnlockAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, rowVersion: number, data: RouteUnlockReasonInput) => {
    const res = await client.post(`/api/v1/route-unlock-requests/${id}/revoke`, {
      rowVersion,
      reason: data.reason,
    })
    revalidatePath('/route-unlock-requests')
    return res.data.data as RouteUnlockRequestDto
  }
)
