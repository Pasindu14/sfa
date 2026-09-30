'use client'

import { Badge } from '@/components/ui/badge'
import {
  RouteUnlockStatus,
  routeUnlockStatusLabels,
  type RouteUnlockStatusValue,
} from '../../schema/route-unlock-request.schema'
import { cn } from '@/lib/utils'

const statusClassNames: Record<RouteUnlockStatusValue, string> = {
  [RouteUnlockStatus.Pending]: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-50',
  [RouteUnlockStatus.Approved]: 'bg-green-50 text-green-700 border-green-200 hover:bg-green-50',
  [RouteUnlockStatus.Expired]: 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-100',
  [RouteUnlockStatus.Rejected]: 'bg-red-50 text-red-700 border-red-200 hover:bg-red-50',
  [RouteUnlockStatus.Cancelled]: 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-100',
  [RouteUnlockStatus.Revoked]: 'bg-orange-50 text-orange-700 border-orange-200 hover:bg-orange-50',
}

interface RouteUnlockStatusBadgeProps {
  status: RouteUnlockStatusValue
  className?: string
}

export function RouteUnlockStatusBadge({ status, className }: RouteUnlockStatusBadgeProps) {
  const label = routeUnlockStatusLabels[status] ?? String(status)
  return (
    <Badge
      variant="outline"
      className={cn('text-xs font-medium whitespace-nowrap', statusClassNames[status], className)}
    >
      {label}
    </Badge>
  )
}
