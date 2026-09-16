'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  GrantExemptionInput,
  ProximityExemptionDto,
} from '../schema/proximity-exemption.schema'

// Every action is Admin-gated here as well as on the API endpoint. Hiding the
// menu item in the UI is cosmetic; these two are the real checks.

export const getExemptionHistoryAction = createAction(
  { name: 'getExemptionHistoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (userId: number) => {
    const res = await client.get(`/api/v1/proximity-exemptions/user/${userId}`)
    return res.data.data as ProximityExemptionDto[]
  }
)

export const getCurrentExemptionAction = createAction(
  { name: 'getCurrentExemptionAction', requireAuth: true, requiredRole: 'Admin' },
  async (userId: number) => {
    const res = await client.get(`/api/v1/proximity-exemptions/user/${userId}/current`)
    // The API answers 200 with null data when the rep holds no live grant.
    return (res.data.data ?? null) as ProximityExemptionDto | null
  }
)

export const grantExemptionAction = createAction(
  { name: 'grantExemptionAction', requireAuth: true, requiredRole: 'Admin' },
  async (userId: number, data: GrantExemptionInput) => {
    const res = await client.post(`/api/v1/proximity-exemptions/user/${userId}`, data)
    revalidatePath('/users')
    return res.data.data as ProximityExemptionDto
  }
)

export const revokeExemptionAction = createAction(
  { name: 'revokeExemptionAction', requireAuth: true, requiredRole: 'Admin' },
  async (exemptionId: number, rowVersion: number) => {
    const res = await client.post(
      `/api/v1/proximity-exemptions/${exemptionId}/revoke`,
      { rowVersion }
    )
    revalidatePath('/users')
    return res.data.data as ProximityExemptionDto
  }
)
