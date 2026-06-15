'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import { binCardResponseSchema, type BinCardResponse } from '../schema/bin-card.schema'

/**
 * Fetches the per-SKU bin card for a distributor over an inclusive date range.
 * `from`/`to` are ISO date strings (YYYY-MM-DD).
 */
export const getBinCardAction = createAction(
  { name: 'getBinCardAction', requireAuth: true, requiredRole: 'Admin' },
  async (distributorId: number, from: string, to: string): Promise<BinCardResponse> => {
    const res = await client.get(`/api/v1/stock/distributors/${distributorId}/bin-card`, {
      params: { from, to },
    })
    return binCardResponseSchema.parse(res.data.data)
  }
)
