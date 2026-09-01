'use client'

import { AsyncSelect } from '@/components/async-select'
import { useRepSearchFetcher } from '../../hooks/rep-route.hooks'
import type { RepOptionDto } from '../../schema/rep-route.schema'

interface RepSelectProps {
  value: number | null
  onChange: (repId: number | null) => void
}

/**
 * Sales-rep picker for the route page.
 *
 * `preload={false}` so each search round-trips to the API, which filters by role and active
 * status server-side. Loading one fixed page and filtering in the browser would quietly hide
 * reps once the org outgrows that page.
 */
export function RepSelect({ value, onChange }: RepSelectProps) {
  const fetcher = useRepSearchFetcher()

  return (
    <AsyncSelect<RepOptionDto>
      fetcher={fetcher}
      preload={false}
      label="sales rep"
      placeholder="Select a sales rep…"
      value={value ? String(value) : ''}
      onChange={(v) => onChange(v ? Number(v) : null)}
      getOptionValue={(r) => String(r.id)}
      getDisplayValue={(r) => <span className="truncate">{r.name}</span>}
      renderOption={(r) => (
        <div className="flex flex-col">
          <span className="text-sm font-medium">{r.name}</span>
          {r.username && (
            <span className="text-xs text-muted-foreground">{r.username}</span>
          )}
        </div>
      )}
      noResultsMessage="No sales reps found"
      notFound={<div className="p-2 text-sm text-muted-foreground">No sales reps found</div>}
      width="100%"
      triggerClassName="w-full sm:w-64"
      clearable
    />
  )
}
