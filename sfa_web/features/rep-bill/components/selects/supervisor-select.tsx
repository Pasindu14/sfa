'use client'

import { AsyncSelect } from '@/components/async-select'
import { useSupervisorSearchFetcher } from '../../hooks/rep-bill.hooks'
import type { SupervisorOption } from '../../schema/rep-bill.schema'

interface SupervisorSelectProps {
  value: number | null
  onChange: (supervisorId: number | null) => void
}

/**
 * `preload={false}` so each search round-trips to the API, which filters by role and active
 * status server-side. Loading one fixed page and filtering in the browser would quietly hide
 * supervisors once the org outgrows that page.
 */
export function SupervisorSelect({ value, onChange }: SupervisorSelectProps) {
  const fetcher = useSupervisorSearchFetcher()

  return (
    <AsyncSelect<SupervisorOption>
      fetcher={fetcher}
      preload={false}
      label="supervisor"
      placeholder="Select a supervisor…"
      value={value ? String(value) : ''}
      onChange={(v) => onChange(v ? Number(v) : null)}
      getOptionValue={(s) => String(s.id)}
      getDisplayValue={(s) => <span className="truncate">{s.name}</span>}
      renderOption={(s) => (
        <div className="flex flex-col">
          <span className="text-sm font-medium">{s.name}</span>
          {s.username && <span className="text-xs text-muted-foreground">{s.username}</span>}
        </div>
      )}
      noResultsMessage="No supervisors found"
      notFound={<div className="p-2 text-sm text-muted-foreground">No supervisors found</div>}
      // AsyncSelect applies `width` as an inline style on the trigger, which outranks any
      // Tailwind width class — so sizing is controlled by the wrapper and 100% fills it.
      width="100%"
      triggerClassName="h-10"
      clearable
    />
  )
}
