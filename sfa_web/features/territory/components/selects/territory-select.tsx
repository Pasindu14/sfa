'use client'

import { useQuery } from '@tanstack/react-query'
import { getActiveTerritoriesAction } from '../../actions/territory.actions'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import type { TerritoryDto } from '../types/territory.types'

function useActiveTerritories() {
  return useQuery({
    queryKey: ['territories', 'active'] as const,
    queryFn: async () => {
      const result = await getActiveTerritoriesAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

interface TerritorySelectProps {
  value?: string
  onValueChange: (value: string) => void
  disabled?: boolean
  placeholder?: string
}

export function TerritorySelect({
  value,
  onValueChange,
  disabled,
  placeholder = 'Select territory',
}: TerritorySelectProps) {
  const { data: territories, isLoading } = useActiveTerritories()

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
          {territories?.map((territory) => (
            <SelectItem key={territory.id} value={String(territory.id)}>
              {territory.name}
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
