'use client'

import { useActiveAreas } from '../../hooks/area.hooks'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'

interface AreaSelectProps {
  value?: string
  onValueChange: (value: string) => void
  disabled?: boolean
  placeholder?: string
}

export function AreaSelect({
  value,
  onValueChange,
  disabled,
  placeholder = 'Select area',
}: AreaSelectProps) {
  const { data: areas, isLoading } = useActiveAreas()

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
          {areas?.map((area) => (
            <SelectItem key={area.id} value={String(area.id)}>
              {area.name}
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
