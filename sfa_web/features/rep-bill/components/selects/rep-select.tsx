'use client'

import { AsyncSelect } from '@/components/async-select'
import { useSupervisorRepsFetcher } from '../../hooks/rep-bill.hooks'
import type { RepOption } from '../../schema/rep-bill.schema'

interface RepSelectProps {
  supervisorId: number | null
  value: number | null
  onChange: (repId: number | null) => void
}

/**
 * Sales reps under the chosen supervisor.
 *
 * `preload` is on here — unlike the supervisor picker — because the fetcher returns one
 * supervisor's direct reports rather than a paged slice of everyone, so the whole list is
 * already in hand and typing should filter it locally instead of re-hitting the API.
 *
 * The fetcher's identity changes with `supervisorId`, which is what makes AsyncSelect refetch
 * when the supervisor above it changes.
 */
export function RepSelect({ supervisorId, value, onChange }: RepSelectProps) {
  const fetcher = useSupervisorRepsFetcher(supervisorId)

  return (
    <AsyncSelect<RepOption>
      key={supervisorId ?? 'none'}
      fetcher={fetcher}
      preload
      filterFn={(rep, query) => rep.userName.toLowerCase().includes(query.toLowerCase())}
      label="sales rep"
      placeholder={supervisorId ? 'Select a sales rep…' : 'Select a supervisor first'}
      disabled={!supervisorId}
      value={value ? String(value) : ''}
      onChange={(v) => onChange(v ? Number(v) : null)}
      getOptionValue={(r) => String(r.userId)}
      getDisplayValue={(r) => <span className="truncate">{r.userName}</span>}
      renderOption={(r) => <span className="text-sm font-medium">{r.userName}</span>}
      noResultsMessage="No sales reps under this supervisor"
      notFound={
        <div className="p-2 text-sm text-muted-foreground">
          No sales reps report to this supervisor
        </div>
      }
      width="100%"
      triggerClassName="h-10"
      clearable
    />
  )
}
