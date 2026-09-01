'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { RepOptionDto, RepRouteDto } from '../schema/rep-route.schema'

/**
 * One rep's travelled route for a single Sri Lanka business day.
 * `date` must already be a Colombo `YYYY-MM-DD` string — use `toColomboDateStr`, never
 * `Date.toISOString()`, or the day boundary shifts by the browser's offset.
 */
export const getRepRouteAction = createAction(
  { name: 'getRepRouteAction', requireAuth: true, requiredRole: 'Admin' },
  async (repId: number, date: string) => {
    const res = await client.get(`/api/v1/location-pings/rep/${repId}/route`, {
      params: { date },
    })
    return res.data.data as RepRouteDto
  },
)

/**
 * Sales reps for the picker, filtered server-side.
 *
 * `role` and `isActive` are applied by the API rather than in the browser: the system is
 * sized for ~500 reps, so a client-side filter over a fixed page would silently drop reps
 * off the end of the list. `search` is passed through for the same reason — the dropdown
 * narrows at the source instead of paging everything down the wire.
 */
export const getRepsForSelectAction = createAction(
  { name: 'getRepsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async (search?: string) => {
    const res = await client.get('/api/v1/users', {
      params: {
        page: 1,
        pageSize: 50,
        isActive: true,
        role: 'SalesRep',
        ...(search ? { search } : {}),
      },
    })
    return (res.data.data as { users: RepOptionDto[] }).users
  },
)
