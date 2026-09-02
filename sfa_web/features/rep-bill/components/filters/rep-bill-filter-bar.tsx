'use client'

import { RotateCcw, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import { toColomboDateStr } from '@/lib/utils/datetime'
import { useRepBillFilters } from '../../store'
import { SupervisorSelect } from '../selects/supervisor-select'
import { RepSelect } from '../selects/rep-select'

/**
 * The filter row. Lives *below* the page's hero card rather than inside it — the date picker's
 * popover trigger gets clipped by a padded card edge, the same reason the Rep Route History page
 * pulls its filters out.
 */
export function RepBillFilterBar() {
  const {
    dateFrom,
    dateTo,
    supervisorId,
    repId,
    appliedFilters,
    isFetching,
    setDateRange,
    setSupervisorId,
    setRepId,
    applyFilters,
    reset,
  } = useRepBillFilters()

  const hasLoaded = !!appliedFilters

  // The controls have moved on from what the table is showing. Say so, rather than letting the
  // rows silently disagree with the filters above them.
  const isDirty =
    hasLoaded &&
    (appliedFilters.dateFrom !== dateFrom ||
      appliedFilters.dateTo !== dateTo ||
      appliedFilters.salesRepId !== repId)

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-4 rounded-lg border bg-background p-4 sm:flex-row sm:flex-wrap sm:items-end">
        <div className="flex w-full flex-col gap-1.5 sm:w-auto">
          <label className="text-xs font-medium text-muted-foreground">Date range</label>
          <CalendarDatePicker
            id="rep-bill-date-range"
            date={{
              from: dateFrom ? new Date(`${dateFrom}T00:00:00`) : undefined,
              to: dateTo ? new Date(`${dateTo}T00:00:00`) : undefined,
            }}
            onDateSelect={({ from, to }) =>
              setDateRange(toColomboDateStr(from), toColomboDateStr(to))
            }
            numberOfMonths={2}
            variant="outline"
            className="h-10 w-full cursor-pointer sm:w-fit"
          />
        </div>

        {/* Width lives on the wrapper, not the select — AsyncSelect applies its `width` prop as
            an inline style on the trigger, which no Tailwind class can override. */}
        <div className="flex w-full flex-col gap-1.5 sm:w-72">
          <label className="text-xs font-medium text-muted-foreground">Supervisor</label>
          <SupervisorSelect value={supervisorId} onChange={setSupervisorId} />
        </div>

        <div className="flex w-full flex-col gap-1.5 sm:w-72">
          <label className="text-xs font-medium text-muted-foreground">Sales rep</label>
          <RepSelect supervisorId={supervisorId} value={repId} onChange={setRepId} />
        </div>

        <div className="flex items-center gap-2">
          <Button
            onClick={applyFilters}
            disabled={!repId || isFetching}
            className="h-10 gap-2 sm:w-36"
          >
            {isFetching ? <Spinner className="h-4 w-4" /> : <Search className="h-4 w-4" />}
            {isFetching ? 'Loading…' : hasLoaded ? 'Reload' : 'Load bills'}
          </Button>

          {hasLoaded && (
            <Button
              variant="ghost"
              onClick={reset}
              className="h-10 gap-1.5 text-muted-foreground"
            >
              <RotateCcw className="h-3.5 w-3.5" />
              Reset
            </Button>
          )}
        </div>
      </div>

      {isDirty && (
        <p className="text-xs text-muted-foreground">
          Filters changed — press <span className="font-medium">Reload</span> to apply them
        </p>
      )}
    </div>
  )
}
