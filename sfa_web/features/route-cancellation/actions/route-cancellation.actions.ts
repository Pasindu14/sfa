'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  RejectCancellationInput,
  RouteCancellationListDto,
} from '../schema/route-cancellation.schema'

// ── Read ───────────────────────────────────────────────────────────────────

export const getPendingCancellationsAction = createAction(
  { name: 'getPendingCancellationsAction', requireAuth: true },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/daily-route-assignments/pending-deletions', {
      params: {
        page,
        pageSize,
        search: search || undefined,
      },
    })
    return res.data.data as RouteCancellationListDto
  }
)

// ── Workflow actions ───────────────────────────────────────────────────────

export const approveCancellationAction = createAction(
  { name: 'approveCancellationAction', requireAuth: true },
  async (id: number) => {
    await client.post(`/api/v1/daily-route-assignments/${id}/approve-deletion`)
    revalidatePath('/route-cancellations')
  }
)

export const rejectCancellationAction = createAction(
  { name: 'rejectCancellationAction', requireAuth: true },
  async (id: number, data: RejectCancellationInput) => {
    await client.post(`/api/v1/daily-route-assignments/${id}/reject-deletion`, data)
    revalidatePath('/route-cancellations')
  }
)
