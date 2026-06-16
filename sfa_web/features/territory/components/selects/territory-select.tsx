'use client'

import { useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getActiveTerritoriesAction } from '../../actions/territory.actions'
import { AsyncSelect } from '@/components/async-select'
import type { TerritoryDto } from '../types/territory.types'

function useActiveTerritories() {
  return useQuery({
    queryKey: ['territories', 'active'] as const,
    queryFn: async () => {
      const result = await getActiveTerritoriesAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

interface TerritorySelectProps {
  value?: string
  onValueChange: (value: string) => void
  disabled?: boolean
  placeholder?: string
}

export function TerritorySelect({
  value = '',
  onValueChange,
  disabled,
  placeholder = 'Select territory',
}: TerritorySelectProps) {
  const { data: territories = [], isLoading } = useActiveTerritories()

  // In-memory fetcher backed by the TanStack Query cache — no API call per keystroke.
  // Returns every active territory on open (empty query); filters by name as the user types.
  const fetcher = useCallback(
    async (query?: string): Promise<TerritoryDto[]> => {
      if (!query) return territories
      const q = query.toLowerCase()
      return territories.filter((t) => t.name.toLowerCase().includes(q))
    },
    [territories],
  )

  // Pre-paint the trigger label in edit mode, where `value` (a territory id) is
  // known before the options list resolves.
  const initialOption = value
    ? territories.find((t) => String(t.id) === value) ?? null
    : null

  return (
    <AsyncSelect<TerritoryDto>
      fetcher={fetcher}
      preload={false}
      label="territory"
      placeholder={placeholder}
      value={value}
      onChange={onValueChange}
      getOptionValue={(t) => String(t.id)}
      getDisplayValue={(t) => <span>{t.name}</span>}
      renderOption={(t) => <span>{t.name}</span>}
      noResultsMessage="No territories found"
      disabled={disabled || isLoading}
      width="100%"
      triggerClassName="w-full"
      initialOption={initialOption}
    />
  )
}
