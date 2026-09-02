'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { RepBillDetail, RepBillListItem, SupervisorOption } from '../schema/rep-bill.schema'

/**
 * Bills written by one sales rep over a business-date range.
 *
 * `salesRepId` is required by design: `GET /api/v1/billings` has no `supervisorId` filter, so
 * "every bill under supervisor X" cannot be asked for in one call. The supervisor picker on the
 * page narrows the rep list instead of narrowing the query.
 *
 * `dateFrom`/`dateTo` must already be Colombo `YYYY-MM-DD` strings — the API parses them with
 * `DateOnly.TryParse` and silently treats an unparseable value as "no filter", so a malformed
 * date widens the range instead of erroring.
 */
export const getRepBillsAction = createAction(
  { name: 'getRepBillsAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 20,
    salesRepId: number,
    dateFrom: string,
    dateTo: string,
    distributorStatus?: string,
    paymentType?: string,
  ) => {
    const res = await client.get('/api/v1/billings', {
      params: {
        page,
        pageSize,
        salesRepId,
        dateFrom,
        dateTo,
        distributorStatus: distributorStatus || undefined,
        paymentType: paymentType || undefined,
      },
    })
    const body = res.data
    return {
      bills: body.data as RepBillListItem[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    }
  },
)

export const getRepBillDetailAction = createAction(
  { name: 'getRepBillDetailAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/billings/${id}`)
    return res.data.data as RepBillDetail
  },
)

/**
 * Supervisors for the picker, filtered server-side.
 *
 * `role` and `isActive` are applied by the API rather than in the browser: a client-side filter
 * over one fixed page would quietly drop supervisors once the org outgrows that page. `search`
 * goes the same way, so the dropdown narrows at the source.
 */
export const getSupervisorsForSelectAction = createAction(
  { name: 'getSupervisorsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async (search?: string) => {
    const res = await client.get('/api/v1/users', {
      params: {
        page: 1,
        pageSize: 50,
        isActive: true,
        role: 'Supervisor',
        ...(search ? { search } : {}),
      },
    })
    return (res.data.data as { users: SupervisorOption[] }).users
  },
)
