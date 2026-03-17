'use client'

import { Badge } from '@/components/ui/badge'
import { SalesOrderStatus, type SalesOrderStatusValue } from '../schema/sales-order.schema'
import { cn } from '@/lib/utils'

const statusConfig: Record<SalesOrderStatusValue, { label: string; className: string }> = {
  [SalesOrderStatus.Draft]: {
    label: 'Draft',
    className: 'bg-slate-100 text-slate-700 border-slate-200 hover:bg-slate-100',
  },
  [SalesOrderStatus.PendingRepApproval]: {
    label: 'Pending Rep Approval',
    className: 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-50',
  },
  [SalesOrderStatus.PendingManagerApproval]: {
    label: 'Pending Manager Approval',
    className: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-50',
  },
  [SalesOrderStatus.PendingDistributorFinalization]: {
    label: 'Pending Finalization',
    className: 'bg-purple-50 text-purple-700 border-purple-200 hover:bg-purple-50',
  },
  [SalesOrderStatus.Finalized]: {
    label: 'Finalized',
    className: 'bg-green-50 text-green-700 border-green-200 hover:bg-green-50',
  },
  [SalesOrderStatus.Cancelled]: {
    label: 'Cancelled',
    className: 'bg-red-50 text-red-700 border-red-200 hover:bg-red-50',
  },
  [SalesOrderStatus.PendingDistributorAcknowledgement]: {
    label: 'Pending Dist. Acknowledgement',
    className: 'bg-orange-50 text-orange-700 border-orange-200 hover:bg-orange-50',
  },
}

interface SalesOrderStatusBadgeProps {
  status: SalesOrderStatusValue
  className?: string
}

export function SalesOrderStatusBadge({ status, className }: SalesOrderStatusBadgeProps) {
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
