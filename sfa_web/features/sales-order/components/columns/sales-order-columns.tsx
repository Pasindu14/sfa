'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, Eye, Pencil } from 'lucide-react'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import type { SalesOrderSummaryDto } from '../../schema/sales-order.schema'
import { SalesOrderStatus } from '../../schema/sales-order.schema'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(dateStr: string | null | undefined) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-LK', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function getSalesOrderColumns(): ColumnDef<SalesOrderSummaryDto>[] {
  return [
    {
      accessorKey: 'orderNumber',
      header: 'Order #',
      cell: ({ row }) => (
        <Link
          href={`/sales-orders/${row.original.id}`}
          className="font-mono text-sm font-semibold text-primary hover:underline"
        >
          {row.original.orderNumber}
        </Link>
      ),
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="font-medium">{row.original.distributorName}</span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <SalesOrderStatusBadge status={row.original.status} />,
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
      accessorKey: 'submittedAt',
      header: 'Submitted',
      cell: ({ row }) => (
        <span className="text-muted-foreground text-sm">{formatDate(row.original.submittedAt)}</span>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => (
        <span className="text-muted-foreground text-sm">{formatDate(row.original.createdAt)}</span>
      ),
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const order = row.original
        const isDraft = order.status === SalesOrderStatus.Draft

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`/sales-orders/${order.id}`} className="flex items-center gap-2">
                  <Eye className="h-4 w-4" />
                  View
                </Link>
              </DropdownMenuItem>
              {isDraft && (
                <DropdownMenuItem asChild>
                  <Link href={`/sales-orders/${order.id}/edit`} className="flex items-center gap-2">
                    <Pencil className="h-4 w-4" />
                    Edit
                  </Link>
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
