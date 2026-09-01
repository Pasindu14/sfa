'use client'

import { useState } from 'react'
import { format } from 'date-fns'
import { Calendar as CalendarIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { toColomboDateStr } from '@/lib/utils/datetime'

interface DateOnlyPickerProps {
  /** Colombo business date as `YYYY-MM-DD`, or '' for none. */
  value: string
  onChange: (date: string) => void
  placeholder?: string
  disabled?: boolean
  id?: string
  /** Applied to the trigger button — set the width here. */
  className?: string
}

/**
 * Single business-date picker.
 *
 * Works in `YYYY-MM-DD` Colombo strings rather than `Date` objects, because that is what the
 * API's business-date params expect — keeping the string as the source of truth avoids a
 * Date→string conversion at every call site, which is where off-by-one-day bugs creep in.
 *
 * Use this instead of `CalendarDatePicker` when a single day is wanted; that component is a
 * range picker and always yields a `{ from, to }` pair.
 */
export function DateOnlyPicker({
  value,
  onChange,
  placeholder = 'Pick a date',
  disabled,
  id,
  className,
}: DateOnlyPickerProps) {
  const [open, setOpen] = useState(false)

  // Parsed as local midnight so the rendered label is the calendar day the user picked,
  // with no timezone shift applied on the way back out.
  const selected = value ? new Date(`${value}T00:00:00`) : undefined

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          id={id}
          variant="outline"
          disabled={disabled}
          className={cn(
            'justify-start text-left font-normal',
            !value && 'text-muted-foreground',
            className,
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4 shrink-0 text-muted-foreground" />
          {selected ? format(selected, 'd MMM yyyy') : placeholder}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={selected}
          onSelect={(day) => {
            if (day) onChange(toColomboDateStr(day))
            setOpen(false)
          }}
          autoFocus
        />
      </PopoverContent>
    </Popover>
  )
}
