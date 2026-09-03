'use client'

import { AsyncSelect } from '@/components/async-select'

/**
 * Thin wrapper over AsyncSelect for the report's nine optional "narrow to one X" filters.
 *
 * They are all the same shape — pick an entity, store its id, allow clearing — so this keeps the
 * criteria bar readable instead of repeating nine near-identical AsyncSelect blocks.
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
  fetcher: (search?: string) => Promise<T[]>
  getName: (item: T) => string
  getSubtitle?: (item: T) => string | undefined
  /** True when the fetcher ignores the search term and returns the whole (small) active list. */
  preload?: boolean
  disabled?: boolean
}) {
  return (
    <AsyncSelect<T>
      label={label}
      placeholder={placeholder ?? `Any ${label.toLowerCase()}`}
      fetcher={fetcher}
      preload={preload}
      // Only meaningful in preload mode, where nothing is filtered server-side.
      filterFn={
        preload
          ? (item, query) => getName(item).toLowerCase().includes(query.toLowerCase())
          : undefined
      }
      value={value?.toString() ?? ''}
      onChange={(val) => onChange(val ? Number(val) : null)}
      getOptionValue={(item) => item.id.toString()}
      getDisplayValue={(item) => <span className="text-sm">{getName(item)}</span>}
      renderOption={(item) => {
        const subtitle = getSubtitle?.(item)
        return (
          <div className="flex flex-col gap-0.5 py-0.5">
            <span className="text-sm font-medium">{getName(item)}</span>
            {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
          </div>
        )
      }}
      noResultsMessage={`No ${label.toLowerCase()} found`}
      disabled={disabled}
      // Width lives on the wrapper in the criteria bar, not here — AsyncSelect applies `width`
      // as an inline style on the trigger, which no Tailwind class can override.
      triggerClassName="h-9 w-full"
      clearable
    />
  )
}
