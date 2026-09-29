'use client'

import { useIsFetching, useQuery, useQueryClient } from '@tanstack/react-query'
import type { z } from 'zod'
import { ActionError } from '@/lib/actions/action-error'
import {
  dashboardActivitySchema,
  dashboardBreakdownSchema,
  dashboardSalesSchema,
  dashboardTrendSchema,
  type DashboardSection,
} from '../schema/dashboard.schema'
import { useDashboardStore } from '../store/dashboard.store'

export const dashboardKeys = {
  all: ['dashboard'] as const,
  section: (section: DashboardSection, date: string | null) =>
    [...dashboardKeys.all, section, date ?? 'today'] as const,
}

/** Matches the API's 2-minute dashboard cache — polling faster would only re-read the cache. */
const REFRESH_MS = 2 * 60 * 1000

/**
 * Fetches one section through the `/api/dashboard/[section]` route handler — NOT a server action.
 * Next.js dispatches server actions one at a time per client, so actions would load the four
 * sections in series; plain GETs run in parallel.
 */
async function fetchSection<S extends z.ZodTypeAny>(
  section: DashboardSection,
  date: string | null,
  schema: S,
  signal: AbortSignal,
): Promise<z.infer<S>> {
  const qs = date ? `?date=${encodeURIComponent(date)}` : ''
  const res = await fetch(`/api/dashboard/${section}${qs}`, { signal, cache: 'no-store' })
  const body = await res.json().catch(() => null)
  if (!res.ok) {
    throw new ActionError({
      error: body?.message ?? 'Could not load the dashboard.',
      code: body?.code,
      status: res.status,
    })
  }
  return schema.parse(body)
}

function useSection<S extends z.ZodTypeAny>(section: DashboardSection, schema: S) {
  const date = useDashboardStore((s) => s.date)
  return useQuery({
    queryKey: dashboardKeys.section(section, date),
    queryFn: ({ signal }) => fetchSection(section, date, schema, signal),
    staleTime: REFRESH_MS,
    // Only today moves; a past day is a closed book.
    refetchInterval: date === null ? REFRESH_MS : false,
    // Keep the previous day's numbers on screen while a newly picked day loads, instead of
    // collapsing every section back to a skeleton.
    placeholderData: (prev) => prev,
  })
}

export const useDashboardSales = () => useSection('sales', dashboardSalesSchema)
export const useDashboardActivity = () => useSection('activity', dashboardActivitySchema)
export const useDashboardTrend = () => useSection('trend', dashboardTrendSchema)
export const useDashboardBreakdown = () => useSection('breakdown', dashboardBreakdownSchema)

/** True while any section is fetching — drives the header's refresh spinner. */
export function useDashboardIsFetching() {
  return useIsFetching({ queryKey: dashboardKeys.all }) > 0
}

/** Refetches every section at once (in parallel). */
export function useRefreshDashboard() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
}
