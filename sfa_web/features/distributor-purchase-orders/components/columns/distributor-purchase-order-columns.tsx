'use client'

import type { ColumnDef } from '@tanstack/react-table'
import Link from 'next/link'
import { Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DistributorPurchaseOrderStatusBadge } from '../distributor-purchase-order-status-badge'
import type { MyPurchaseOrderSummaryDto } from '../../schema/distributor-purchase-order.schema'
import { formatColombo } from '@/lib/utils/datetime'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(dateStr: string | null) {
  return formatColombo(dateStr, 'd MMM yyyy')
}

export function getDistributorPurchaseOrderColumns(): ColumnDef<MyPurchaseOrderSummaryDto>[] {
  return [
    {
      accessorKey: 'orderNumber',
      header: 'Order #',
      cell: ({ row }) => (
        <Link
          href={`/distributor-purchase-orders/${row.original.id}`}
          className="font-mono text-sm font-semibold text-primary hover:underline"
        >
          {row.original.orderNumber}
        </Link>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => (
        <DistributorPurchaseOrderStatusBadge status={row.original.status} />
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: () => <div className="text-right">Total Amount</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold tabular-nums">
          {formatCurrency(row.original.totalAmount)}
        </div>
      ),
    },
    {
      accessorKey: 'itemCount',
      header: () => <div className="text-right">Items</div>,
      cell: ({ row }) => (
        <div className="text-right tabular-nums text-muted-foreground">
          {row.original.itemCount}
        </div>
      ),
    },
    {
      accessorKey: 'submittedAt',
      header: 'Submitted',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatDate(row.original.submittedAt)}
        </span>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatDate(row.original.createdAt)}
        </span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <Link href={`/distributor-purchase-orders/${row.original.id}`}>
          <Button size="sm" variant="outline" className="h-7 gap-1.5 text-xs">
            <Eye className="h-3.5 w-3.5" />
            View
          </Button>
        </Link>
      ),
    },
  ]
}
