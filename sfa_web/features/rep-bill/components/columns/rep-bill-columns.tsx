'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Eye } from 'lucide-react'
import { formatColombo } from '@/lib/utils/datetime'
import type { RepBillListItem } from '../../schema/rep-bill.schema'

export function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

export function RepStatusBadge({ status }: { status: RepBillListItem['repStatus'] }) {
  if (status === 'Cancelled')
    return <Badge variant="destructive" className="text-xs">Cancelled</Badge>
  return <Badge variant="outline" className="text-xs">Submitted</Badge>
}

export function DistributorStatusBadge({
  status,
}: {
  status: RepBillListItem['distributorStatus']
}) {
  if (status === 'Approved')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Approved</Badge>
  if (status === 'Rejected')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Rejected</Badge>
  return <Badge variant="secondary" className="text-xs">Pending</Badge>
}

export function PaymentTypeBadge({ type }: { type: RepBillListItem['paymentType'] }) {
  if (type === 'Credit')
    return (
      <Badge className="border-0 bg-blue-100 text-xs text-blue-700 hover:bg-blue-100">Credit</Badge>
    )
  return <Badge variant="outline" className="text-xs">Cash</Badge>
}

export function getRepBillColumns(onView: (id: number) => void): ColumnDef<RepBillListItem>[] {
  return [
    {
      accessorKey: 'billingNumber',
      header: 'Bill',
      cell: ({ row }) => (
        <div>
          <p className="font-mono text-xs font-semibold">{row.original.billingNumber}</p>
          <p className="text-xs text-muted-foreground">
            {formatColombo(row.original.billingDate, 'd MMM yyyy')}
          </p>
        </div>
      ),
    },
    {
      accessorKey: 'outletName',
      header: 'Outlet',
      cell: ({ row }) => <span className="text-sm">{row.original.outletName}</span>,
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.distributorName}</span>
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: () => <span className="block text-right">Amount</span>,
      cell: ({ row }) => (
        <span className="block text-right text-sm font-semibold tabular-nums">
          {formatCurrency(row.original.totalAmount)}
        </span>
      ),
    },
    {
      id: 'paymentType',
      header: 'Payment',
      cell: ({ row }) => <PaymentTypeBadge type={row.original.paymentType} />,
    },
    {
      id: 'cashCollected',
      header: 'Cash',
      cell: ({ row }) =>
        row.original.isCashCollected ? (
          <Badge className="border-0 bg-green-100 text-xs text-green-700 hover:bg-green-100">
            Collected
          </Badge>
        ) : (
          <Badge variant="secondary" className="text-xs text-muted-foreground">
            Pending
          </Badge>
        ),
    },
    {
      id: 'repStatus',
      header: 'Rep Status',
      cell: ({ row }) => <RepStatusBadge status={row.original.repStatus} />,
    },
    {
      id: 'distributorStatus',
      header: 'Distributor Status',
      cell: ({ row }) => <DistributorStatusBadge status={row.original.distributorStatus} />,
    },
    {
      id: 'actions',
      header: '',
      // The table is `table-layout: fixed`, so a column narrower than its content silently
      // clips the button. minSize stops a resize drag from shrinking it back into a clip.
      size: 110,
      minSize: 110,
      cell: ({ row }) => (
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1.5 text-xs"
          onClick={() => onView(row.original.id)}
        >
          <Eye className="h-3.5 w-3.5" />
          View
        </Button>
      ),
    },
  ]
}
