'use client'

import { Badge } from '@/components/ui/badge'
import { PurchaseOrderStatus, type PurchaseOrderStatusValue } from '../schema/distributor-purchase-order.schema'
import { cn } from '@/lib/utils'

const statusConfig: Record<PurchaseOrderStatusValue, { label: string; className: string }> = {
  [PurchaseOrderStatus.Draft]: {
    label: 'Draft',
    className: 'bg-slate-100 text-slate-700 border-slate-200 hover:bg-slate-100',
  },
  [PurchaseOrderStatus.PendingRepApproval]: {
    label: 'Pending Rep Approval',
    className: 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-50',
  },
  [PurchaseOrderStatus.PendingManagerApproval]: {
    label: 'Pending Manager Approval',
    className: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-50',
  },
  [PurchaseOrderStatus.PendingDistributorFinalization]: {
    label: 'Pending Finalization',
    className: 'bg-purple-50 text-purple-700 border-purple-200 hover:bg-purple-50',
  },
  [PurchaseOrderStatus.Finalized]: {
    label: 'Finalized',
    className: 'bg-green-50 text-green-700 border-green-200 hover:bg-green-50',
  },
  [PurchaseOrderStatus.Cancelled]: {
    label: 'Cancelled',
    className: 'bg-red-50 text-red-700 border-red-200 hover:bg-red-50',
  },
  [PurchaseOrderStatus.PendingDistributorAcknowledgement]: {
    label: 'Pending Acknowledgement',
    className: 'bg-orange-50 text-orange-700 border-orange-200 hover:bg-orange-50',
  },
}

interface Props {
  status: PurchaseOrderStatusValue
  className?: string
}

export function DistributorPurchaseOrderStatusBadge({ status, className }: Props) {
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
