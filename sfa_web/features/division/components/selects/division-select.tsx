'use client'

import { useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getActiveDivisionsAction } from '../../actions/division.actions'
import { AsyncSelect } from '@/components/async-select'
import type { DivisionDto } from '../types/division.types'

function useActiveDivisionsSelect() {
  return useQuery({
    queryKey: ['divisions', 'active'] as const,
    queryFn: async () => {
      const result = await getActiveDivisionsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

interface DivisionSelectProps {
  value?: string
  onValueChange: (value: string) => void
  disabled?: boolean
  placeholder?: string
  /** When set, only divisions belonging to this territory are shown. */
  territoryId?: number
}

export function DivisionSelect({
  value = '',
  onValueChange,
  disabled,
  placeholder = 'Select division',
  territoryId,
}: DivisionSelectProps) {
  const { data: divisions = [], isLoading } = useActiveDivisionsSelect()

  // Narrow to the selected territory when one is provided.
  const pool = territoryId
    ? divisions.filter((d) => d.territoryId === territoryId)
    : divisions

  const fetcher = useCallback(
    async (query?: string): Promise<DivisionDto[]> => {
      if (!query) return pool
      const q = query.toLowerCase()
      return pool.filter((d) => d.name.toLowerCase().includes(q))
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [pool],
  )

  const initialOption = value
    ? pool.find((d) => String(d.id) === value) ?? null
    : null

  return (
    <AsyncSelect<DivisionDto>
      // key forces a remount (clearing internal state) whenever the territory changes.
      key={territoryId ?? 'all'}
      fetcher={fetcher}
      preload={false}
      label="division"
      placeholder={placeholder}
      value={value}
      onChange={onValueChange}
      getOptionValue={(d) => String(d.id)}
      getDisplayValue={(d) => <span>{d.name}</span>}
      renderOption={(d) => (
        <div className="flex flex-col">
          <span className="text-sm leading-none">{d.name}</span>
          <span className="text-xs text-muted-foreground">
            {d.territoryName} → {d.areaName}
          </span>
        </div>
      )}
      noResultsMessage="No divisions found"
      disabled={disabled || isLoading}
      width="100%"
      triggerClassName="w-full"
      initialOption={initialOption}
    />
  )
}
