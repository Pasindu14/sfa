'use client'

import { useQuery } from '@tanstack/react-query'
import { getActiveDivisionsAction } from '../../actions/division.actions'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
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
}

export function DivisionSelect({
  value,
  onValueChange,
  disabled,
  placeholder = 'Select division',
}: DivisionSelectProps) {
  const { data: divisions, isLoading } = useActiveDivisionsSelect()

  return (
    <div className="flex items-center gap-2">
      <Select
        value={value}
        onValueChange={onValueChange}
        disabled={disabled || isLoading}
      >
        <SelectTrigger className="flex-1">
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent className="max-h-10 overflow-y-scroll">
          {divisions?.map((division) => (
            <SelectItem key={division.id} value={String(division.id)}>
              {division.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {isLoading && (
        <Spinner className="size-4 shrink-0 text-muted-foreground" />
      )}
    </div>
  )
}
