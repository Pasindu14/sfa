'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreatePeriodInput,
  AdjustLineInput,
  StockTakingPeriodDto,
  StockTakingLineDto,
  StockTakingSubmissionDto,
  StockTakingPeriodsListResponse,
} from '../schema/stock-taking.schema'

export const getPeriodsAction = createAction(
  { name: 'getPeriodsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/stock-taking/periods', {
      params: { page, pageSize, search: search || undefined },
    })
    const body = res.data
    return {
      items: body.data as StockTakingPeriodDto[],
      totalCount: body.pagination?.total ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    } satisfies StockTakingPeriodsListResponse
  }
)

export const getPeriodByIdAction = createAction(
  { name: 'getPeriodByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/stock-taking/periods/${id}`)
    return res.data.data as StockTakingPeriodDto
  }
)

export const createPeriodAction = createAction(
  { name: 'createPeriodAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreatePeriodInput) => {
    const res = await client.post('/api/v1/stock-taking/periods', data)
    revalidatePath('/stock-taking')
    return res.data.data as StockTakingPeriodDto
  }
)

export const lockPeriodAction = createAction(
  { name: 'lockPeriodAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.post(`/api/v1/stock-taking/periods/${id}/lock`)
    revalidatePath('/stock-taking')
    return res.data.data as StockTakingPeriodDto
  }
)

export const unlockPeriodAction = createAction(
  { name: 'unlockPeriodAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.post(`/api/v1/stock-taking/periods/${id}/unlock`)
    revalidatePath('/stock-taking')
    return res.data.data as StockTakingPeriodDto
  }
)

export const getSubmissionForAdminAction = createAction(
  { name: 'getSubmissionForAdminAction', requireAuth: true, requiredRole: 'Admin' },
  async (periodId: number, distributorId: number) => {
    const res = await client.get(`/api/v1/stock-taking/periods/${periodId}/submissions`, {
      params: { distributorId },
    })
    return res.data.data as StockTakingSubmissionDto
  }
)

export const adjustLineAction = createAction(
  { name: 'adjustLineAction', requireAuth: true, requiredRole: 'Admin' },
  async (lineId: number, data: AdjustLineInput) => {
    const res = await client.post(`/api/v1/stock-taking/lines/${lineId}/adjust`, data)
    revalidatePath('/stock-taking')
    return res.data.data as StockTakingLineDto
  }
)
