'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { RepLocationPingDto } from '../schema/field-rep.schema'

export const getFieldRepsLiveAction = createAction(
  { name: 'getFieldRepsLiveAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/location-pings/latest')
    return res.data.data as RepLocationPingDto[]
  }
)
