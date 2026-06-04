'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  UpsertDraftInput,
  DistributorStockTakingPeriodDto,
  DistributorStockTakingSubmissionDto,
  ProductForSelect,
} from '../schema/distributor-stock-taking.schema'

export const getOpenPeriodsAction = createAction(
  { name: 'getOpenPeriodsDistributorAction', requireAuth: true, requiredRole: 'Distributor' },
  async () => {
    const res = await client.get('/api/v1/stock-taking/portal/periods')
    return res.data.data as DistributorStockTakingPeriodDto[]
  }
)

export const getMySubmissionAction = createAction(
  { name: 'getMySubmissionAction', requireAuth: true, requiredRole: 'Distributor' },
  async (periodId: number) => {
    const res = await client.get(`/api/v1/stock-taking/portal/submissions/${periodId}`)
    return res.data.data as DistributorStockTakingSubmissionDto | null
  }
)

export const upsertDraftAction = createAction(
  { name: 'upsertDraftAction', requireAuth: true, requiredRole: 'Distributor' },
  async (data: UpsertDraftInput) => {
    const res = await client.post('/api/v1/stock-taking/portal/submissions', data)
    revalidatePath('/distributor-stock-taking')
    return res.data.data as DistributorStockTakingSubmissionDto
  }
)

export const submitStockTakingAction = createAction(
  { name: 'submitStockTakingAction', requireAuth: true, requiredRole: 'Distributor' },
  async (periodId: number) => {
    const res = await client.post(`/api/v1/stock-taking/portal/submissions/${periodId}/submit`)
    revalidatePath('/distributor-stock-taking')
    return res.data.data as DistributorStockTakingSubmissionDto
  }
)

export const upsertAndSubmitAction = createAction(
  { name: 'upsertAndSubmitAction', requireAuth: true, requiredRole: 'Distributor' },
  async (data: UpsertDraftInput) => {
    await client.post('/api/v1/stock-taking/portal/submissions', data)
    const res = await client.post(`/api/v1/stock-taking/portal/submissions/${data.periodId}/submit`)
    revalidatePath('/distributor-stock-taking')
    return res.data.data as DistributorStockTakingSubmissionDto
  }
)

export const searchProductsForDistributorAction = createAction(
  { name: 'searchProductsForDistributorAction', requireAuth: true, requiredRole: 'Distributor' },
  async (search?: string) => {
    const res = await client.get('/api/v1/stock-taking/portal/products', {
      params: { search: search || undefined, pageSize: 50 },
    })
    return res.data.data as ProductForSelect[]
  }
)
