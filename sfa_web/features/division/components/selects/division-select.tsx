'use client'

import { useQuery } from '@tanstack/react-query'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import type { DivisionDto } from '../types/division.types'

const getActiveDivisionsSelectAction = createAction(
  { name: 'getActiveDivisionsSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/divisions/active')
    return res.data.data as DivisionDto[]
  }
)

function useActiveDivisionsSelect() {
  return useQuery({
    queryKey: ['divisions', 'active'] as const,
    queryFn: async () => {
      const result = await getActiveDivisionsSelectAction()
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
