'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Eye } from 'lucide-react'
import type { DistributorBillingListItem } from '../../schema/distributor-billing.schema'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function StatusBadge({ status }: { status: DistributorBillingListItem['status'] }) {
  if (status === 'Approved')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Approved</Badge>
  if (status === 'Cancelled')
    return <Badge variant="destructive" className="text-xs">Cancelled</Badge>
  return <Badge variant="outline" className="text-xs">Submitted</Badge>
}

export function getDistributorBillingColumns(
  onView: (id: number) => void,
): ColumnDef<DistributorBillingListItem>[] {
  return [
    {
      accessorKey: 'billingNumber',
      header: 'Billing',
      cell: ({ row }) => (
        <div>
          <p className="font-mono text-xs font-semibold">{row.original.billingNumber}</p>
          <p className="text-xs text-muted-foreground">
            {new Date(row.original.billingDate).toLocaleDateString('en-US', {
              day: 'numeric',
              month: 'short',
              year: 'numeric',
            })}
          </p>
        </div>
      ),
    },
    {
      accessorKey: 'outletName',
      header: 'Outlet',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.outletName}</span>
      ),
    },
    {
      accessorKey: 'salesRepName',
      header: 'Sales Rep',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.salesRepName}</span>
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: () => <span className="block text-right">Amount</span>,
      cell: ({ row }) => (
        <span className="block text-right tabular-nums text-sm font-semibold">
          {formatCurrency(row.original.totalAmount)}
        </span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge status={row.original.status} />,
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1.5 text-xs"
          onClick={() => onView(row.original.id)}
        >
          <Eye className="h-3.5 w-3.5" />
          View Details
        </Button>
      ),
    },
  ]
}
