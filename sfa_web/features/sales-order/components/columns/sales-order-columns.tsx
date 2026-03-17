'use client'

import type { ColumnDef } from '@tanstack/react-table'
import Link from 'next/link'
import { MoreHorizontal, Pencil, Eye } from 'lucide-react'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import { formatLKR } from '@/lib/utils'
import type { SalesOrderSummaryDto } from '../../types/sales-order.types'

export function getColumns(
  role: string
): ColumnDef<SalesOrderSummaryDto>[] {
  const columns: ColumnDef<SalesOrderSummaryDto>[] = [
    {
      accessorKey: 'orderNumber',
      header: 'Order #',
      cell: ({ row }) => (
        <Link
          href={`/sales-orders/${row.original.id}`}
          className="font-medium text-primary hover:underline"
        >
          {row.original.orderNumber}
        </Link>
      ),
    },
  ]

  if (role !== 'Distributor') {
    columns.push({
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.distributorName}</span>
      ),
    })
  }

  columns.push(
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <SalesOrderStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'totalAmount',
      header: () => <div className="text-right">Total</div>,
      cell: ({ row }) => (
        <div className="text-right font-medium">
          {formatLKR(row.original.totalAmount)}
        </div>
      ),
    },
    {
      accessorKey: 'submittedAt',
      header: 'Submitted At',
      cell: ({ row }) =>
        row.original.submittedAt
          ? format(new Date(row.original.submittedAt), 'dd MMM yyyy HH:mm')
          : '—',
    },
    {
      accessorKey: 'createdAt',
      header: 'Created At',
      cell: ({ row }) =>
        format(new Date(row.original.createdAt), 'dd MMM yyyy HH:mm'),
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const order = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`/sales-orders/${order.id}`}>
                  <Eye className="mr-2 h-4 w-4" />
                  View
                </Link>
              </DropdownMenuItem>
              {order.status === 0 && (
                <DropdownMenuItem asChild>
                  <Link href={`/sales-orders/${order.id}/edit`}>
                    <Pencil className="mr-2 h-4 w-4" />
                    Edit
                  </Link>
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  )

  return columns
}
