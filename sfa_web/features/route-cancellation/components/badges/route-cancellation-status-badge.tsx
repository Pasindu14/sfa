'use client'

import { Badge } from '@/components/ui/badge'
import { DeletionStatus, type DeletionStatusValue } from '../../schema/route-cancellation.schema'
import { cn } from '@/lib/utils'

const statusConfig: Record<DeletionStatusValue, { label: string; className: string }> = {
  [DeletionStatus.None]: {
    label: 'None',
    className: 'bg-slate-100 text-slate-700 border-slate-200 hover:bg-slate-100',
  },
  [DeletionStatus.PendingApproval]: {
    label: 'Pending Approval',
    className: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-50',
  },
  [DeletionStatus.Approved]: {
    label: 'Approved',
    className: 'bg-green-50 text-green-700 border-green-200 hover:bg-green-50',
  },
  [DeletionStatus.Rejected]: {
    label: 'Rejected',
    className: 'bg-red-50 text-red-700 border-red-200 hover:bg-red-50',
  },
}

interface RouteCancellationStatusBadgeProps {
  status: DeletionStatusValue
  className?: string
}

export function RouteCancellationStatusBadge({
  status,
  className,
}: RouteCancellationStatusBadgeProps) {
  const config = statusConfig[status] ?? { label: String(status), className: '' }
  return (
    <Badge
      variant="outline"
      className={cn('text-xs font-medium whitespace-nowrap', config.className, className)}
    >
      {config.label}
    </Badge>
  )
}
