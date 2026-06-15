'use client'

import { useQuery, useIsFetching } from '@tanstack/react-query'
import { getBinCardAction } from '../actions/bin-card.actions'
import { useBinCardFilterStore } from '../store'

// ── Query key factory ──────────────────────────────────────────────────────

export const binCardKeys = {
  all: ['bin-card'] as const,
  report: (distributorId: number, from: string, to: string, loadCount: number) =>
    [...binCardKeys.all, distributorId, from, to, loadCount] as const,
}

// ── Report query (fires only after Search sets appliedFilters) ─────────────

export function useBinCard() {
  const applied = useBinCardFilterStore((s) => s.appliedFilters)

  return useQuery({
    queryKey: applied
      ? binCardKeys.report(applied.distributorId, applied.from, applied.to, applied.loadCount)
      : [...binCardKeys.all, 'idle'],
    queryFn: async () => {
      const result = await getBinCardAction(applied!.distributorId, applied!.from, applied!.to)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: !!applied,
  })
}

export function useBinCardIsFetching() {
  return useIsFetching({ queryKey: binCardKeys.all }) > 0
}
