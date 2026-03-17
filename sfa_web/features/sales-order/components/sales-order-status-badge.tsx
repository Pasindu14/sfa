import { Badge } from '@/components/ui/badge'
import type { SalesOrderStatus } from '../types/sales-order.types'

const statusConfig: Record<SalesOrderStatus, { label: string; variant: 'default' | 'secondary' | 'outline' | 'destructive'; className?: string }> = {
  0: { label: 'Draft', variant: 'secondary' },
  1: { label: 'Pending Rep Approval', variant: 'outline', className: 'text-blue-600 border-blue-300' },
  2: { label: 'Pending Manager Approval', variant: 'outline', className: 'text-amber-600 border-amber-300' },
  3: { label: 'Pending Finalization', variant: 'outline', className: 'text-purple-600 border-purple-300' },
  4: { label: 'Finalized', variant: 'default' },
  5: { label: 'Cancelled', variant: 'destructive' },
  6: { label: 'Pending Acknowledgement', variant: 'outline', className: 'text-orange-600 border-orange-300' },
}

interface SalesOrderStatusBadgeProps {
  status: SalesOrderStatus
  className?: string
}

export function SalesOrderStatusBadge({ status, className }: SalesOrderStatusBadgeProps) {
  const config = statusConfig[status]
  return (
    <Badge variant={config.variant} className={`text-xs font-medium ${config.className ?? ''} ${className ?? ''}`}>
      {config.label}
    </Badge>
  )
}
