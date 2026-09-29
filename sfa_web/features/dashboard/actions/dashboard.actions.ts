'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import { dashboardSchema, type DashboardDto } from '../schema/dashboard.schema'

/**
 * The admin dashboard for one business day. `date` is a Colombo `YYYY-MM-DD` string built by
 * `toColomboDateStr` (never `toISOString()`, which shifts the day); omit it for today.
 */
export const getDashboardAction = createAction(
  { name: 'getDashboardAction', requireAuth: true, requiredRole: 'Admin' },
  async (date?: string): Promise<DashboardDto> => {
    const res = await client.get('/api/v1/dashboard', { params: date ? { date } : {} })
    return dashboardSchema.parse(res.data.data)
  },
)
