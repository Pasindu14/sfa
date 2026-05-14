'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { DistributorBillingListItem, DistributorBillingDetail } from '../schema/distributor-billing.schema'

export const getMyBillingsAction = createAction(
  { name: 'getMyBillingsAction', requireAuth: true, requiredRole: 'Distributor' },
  async (
    page: number = 1,
    pageSize: number = 20,
    search?: string,
    status?: string,
    dateFrom?: string,
    dateTo?: string,
  ) => {
    const res = await client.get('/api/v1/billings/portal', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
      },
    })
    const body = res.data
    return {
      billings: body.data as DistributorBillingListItem[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    }
  }
)

export const getMyBillingDetailAction = createAction(
  { name: 'getMyBillingDetailAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.get(`/api/v1/billings/portal/${id}`)
    return res.data.data as DistributorBillingDetail
  }
)
