'use client'

import { useQuery } from '@tanstack/react-query'
import { ActionError } from '@/lib/actions/action-error'
import { getDashboardAction } from '../actions/dashboard.actions'
import { useDashboardStore } from '../store/dashboard.store'

export const dashboardKeys = {
  all: ['dashboard'] as const,
  day: (date: string | null) => [...dashboardKeys.all, date ?? 'today'] as const,
}

/** Matches the API's 2-minute dashboard cache — polling faster would only re-read the cache. */
const REFRESH_MS = 2 * 60 * 1000

export function useDashboard() {
  const date = useDashboardStore((s) => s.date)

  return useQuery({
    queryKey: dashboardKeys.day(date),
    queryFn: async () => {
      const result = await getDashboardAction(date ?? undefined)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    staleTime: REFRESH_MS,
    // Only today moves; a past day is a closed book.
    refetchInterval: date === null ? REFRESH_MS : false,
    placeholderData: (prev) => prev,
  })
}
