'use client'

import { useCallback } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getRepRouteAction, getRepsForSelectAction } from '../actions/rep-route.actions'
import type { RepOptionDto } from '../schema/rep-route.schema'

export const repRouteKeys = {
  all: ['rep-route'] as const,
  route: (repId: number, date: string) => [...repRouteKeys.all, repId, date] as const,
}

/**
 * A rep's route for one day.
 *
 * Deliberately no `refetchInterval`: this is history, not live tracking. The live map polls
 * every 30s because "where is he now" changes; a past day does not. A long `staleTime` also
 * makes flicking between dates feel instant on the way back.
 */
export function useRepRoute(repId: number | null, date: string | null) {
  return useQuery({
    queryKey: repRouteKeys.route(repId ?? 0, date ?? ''),
    queryFn: async () => {
      const result = await getRepRouteAction(repId!, date!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: !!repId && !!date,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Returns the fetcher `AsyncSelect` expects, backed by the query cache.
 *
 * `AsyncSelect` wants an imperative `(query) => Promise<T[]>`, but components must not call
 * server actions directly. `fetchQuery` bridges the two: the action stays behind the hooks
 * layer, and repeated searches for the same term are served from cache instead of re-hitting
 * the API on every keystroke-driven re-open.
 */
export function useRepSearchFetcher() {
  const queryClient = useQueryClient()

  return useCallback(
    (search?: string): Promise<RepOptionDto[]> =>
      queryClient.fetchQuery({
        queryKey: [...repRouteKeys.all, 'reps', search ?? ''] as const,
        queryFn: async () => {
          const result = await getRepsForSelectAction(search)
          if (!result.success) throw new Error(result.error)
          return result.data
        },
        staleTime: 5 * 60 * 1000,
      }),
    [queryClient],
  )
}
