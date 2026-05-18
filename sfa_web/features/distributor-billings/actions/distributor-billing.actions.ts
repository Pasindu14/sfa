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
    repStatus?: string,
    distributorStatus?: string,
    dateFrom?: string,
    dateTo?: string,
  ) => {
    const res = await client.get('/api/v1/billings/portal', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        repStatus: repStatus || undefined,
        distributorStatus: distributorStatus || undefined,
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

export const approveBillingAction = createAction(
  { name: 'approveBillingAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number) => {
    const res = await client.patch(`/api/v1/billings/${id}/approve`)
    return res.data.data as DistributorBillingDetail
  }
)

export const rejectBillingAction = createAction(
  { name: 'rejectBillingAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number, reason?: string) => {
    const res = await client.patch(`/api/v1/billings/${id}/reject`, { reason: reason ?? null })
    return res.data.data as DistributorBillingDetail
  }
)

export const updatePaymentTypeAction = createAction(
  { name: 'updatePaymentTypeAction', requireAuth: true, requiredRole: 'Distributor' },
  async (id: number, paymentType: 'Cash' | 'Credit') => {
    const res = await client.patch(`/api/v1/billings/${id}/payment-type`, { paymentType })
    return res.data.data as DistributorBillingDetail
  }
)
