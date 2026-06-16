'use client'

import { useCallback } from 'react'
import { useActiveAreas } from '../../hooks/area.hooks'
import { AsyncSelect } from '@/components/async-select'
import type { AreaDto } from '../../schema/area.schema'

interface AreaSelectProps {
  value?: string
  onValueChange: (value: string) => void
  disabled?: boolean
  placeholder?: string
}

export function AreaSelect({
  value = '',
  onValueChange,
  disabled,
  placeholder = 'Select area',
}: AreaSelectProps) {
  const { data: areas = [], isLoading } = useActiveAreas()

  // In-memory fetcher backed by the TanStack Query cache — no API call per keystroke.
  // Returns every active area on open (empty query); filters by name as the user types.
  const fetcher = useCallback(
    async (query?: string): Promise<AreaDto[]> => {
      if (!query) return areas
      const q = query.toLowerCase()
      return areas.filter((a) => a.name.toLowerCase().includes(q))
    },
    [areas],
  )

  // Pre-paint the trigger label in edit mode, where `value` (an area id) is
  // known before the options list resolves.
  const initialOption = value
    ? areas.find((a) => String(a.id) === value) ?? null
    : null

  return (
    <AsyncSelect<AreaDto>
      fetcher={fetcher}
      preload={false}
      label="area"
      placeholder={placeholder}
      value={value}
      onChange={onValueChange}
      getOptionValue={(a) => String(a.id)}
      getDisplayValue={(a) => <span>{a.name}</span>}
      renderOption={(a) => <span>{a.name}</span>}
      noResultsMessage="No areas found"
      disabled={disabled || isLoading}
      width="100%"
      triggerClassName="w-full"
      initialOption={initialOption}
    />
  )
}
