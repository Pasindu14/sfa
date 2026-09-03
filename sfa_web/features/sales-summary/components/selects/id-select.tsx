'use client'

import { useCallback } from 'react'
import { AsyncSelect } from '@/components/async-select'

/**
 * Thin wrapper over AsyncSelect for the report's narrowing filters.
 *
 * They are all the same shape — pick an entity, store its id, allow clearing — so this keeps the
 * criteria panel readable instead of repeating near-identical AsyncSelect blocks.
 *
 * Every callback handed to AsyncSelect is memoized. `filterFn` and `fetcher` are dependencies of
 * AsyncSelect's search effect (`components/async-select.tsx:173`); passing inline arrows gives them
 * a new identity on every render, and in preload mode with a search term that effect calls
 * `setOptions(originalOptions.filter(...))` — a new array each time — which re-renders, which makes
 * new callbacks, which re-runs the effect. That is a render loop while typing. Keep these stable.
 */
export function IdSelect<T extends { id: number }>({
  label,
  placeholder,
  value,
  onChange,
  fetcher,
  getName,
  getSubtitle,
  preload = false,
  disabled = false,
}: {
  label: string
  placeholder?: string
  value: number | null
  onChange: (id: number | null) => void
  /** Must be a stable reference — memoize it in the parent (the hooks in this feature already do). */
  fetcher: (search?: string) => Promise<T[]>
  getName: (item: T) => string
  getSubtitle?: (item: T) => string | undefined
  /** True when the fetcher ignores the search term and returns the whole (small) list. */
  preload?: boolean
  disabled?: boolean
}) {
  const handleChange = useCallback(
    (val: string) => onChange(val ? Number(val) : null),
    [onChange],
  )

  const getOptionValue = useCallback((item: T) => item.id.toString(), [])

  const getDisplayValue = useCallback(
    (item: T) => <span className="text-sm">{getName(item)}</span>,
    [getName],
  )

  const renderOption = useCallback(
    (item: T) => {
      const subtitle = getSubtitle?.(item)
      return (
        <div className="flex flex-col gap-0.5 py-0.5">
          <span className="text-sm font-medium">{getName(item)}</span>
          {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
        </div>
      )
    },
    [getName, getSubtitle],
  )

  const filterFn = useCallback(
    (item: T, query: string) => getName(item).toLowerCase().includes(query.toLowerCase()),
    [getName],
  )

  return (
    <AsyncSelect<T>
      label={label}
      placeholder={placeholder ?? `Any ${label.toLowerCase()}`}
      fetcher={fetcher}
      preload={preload}
      // Only meaningful in preload mode, where nothing is filtered server-side.
      filterFn={preload ? filterFn : undefined}
      value={value?.toString() ?? ''}
      onChange={handleChange}
      getOptionValue={getOptionValue}
      getDisplayValue={getDisplayValue}
      renderOption={renderOption}
      noResultsMessage={`No ${label.toLowerCase()} found`}
      disabled={disabled}
      // Width lives on the wrapper in the criteria panel, not here — AsyncSelect applies `width`
      // as an inline style on the trigger, which no Tailwind class can override.
      triggerClassName="h-9 w-full"
      clearable
    />
  )
}
