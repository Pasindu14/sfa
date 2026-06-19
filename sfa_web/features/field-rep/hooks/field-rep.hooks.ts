'use client'

import { useQuery } from '@tanstack/react-query'
import { getFieldRepsLiveAction } from '../actions/field-rep.actions'

export const fieldRepKeys = {
  all: ['field-reps'] as const,
  live: () => [...fieldRepKeys.all, 'live'] as const,
}

export function useFieldRepsLive() {
  return useQuery({
    queryKey: fieldRepKeys.live(),
    queryFn: async () => {
      const result = await getFieldRepsLiveAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    // Poll every 30 seconds — pings arrive every 5 minutes, 30s is generous
    refetchInterval: 30_000,
    // Mark stale immediately so the next poll always fetches fresh data
    staleTime: 1_000,
    gcTime: 60_000,
  })
}
