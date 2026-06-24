'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, ClipboardList } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { SalesInvoiceListItem } from '../types/sales-invoice.types'
import { formatColombo } from '@/lib/utils/datetime'

export interface SalesInvoiceColumnActions {
  openDetail: (id: number) => void
  openDelete: (id: number) => void
  openCreateGrn: (id: number) => void
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

export function getSalesInvoiceColumns(
  actions: SalesInvoiceColumnActions,
): ColumnDef<SalesInvoiceListItem>[] {
  const { openDetail, openDelete, openCreateGrn } = actions

  return [
    {
      id: 'invoice',
      header: 'Invoice',
      cell: ({ row }) => {
        const { vchBillNo, batchNumber } = row.original
        return (
          <div>
            <div className="font-mono text-sm font-medium">{vchBillNo}</div>
            <div className="text-xs text-muted-foreground">{batchNumber}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.distributorName}</span>
      ),
    },
    {
      accessorKey: 'invoiceDate',
      header: 'Date',
      cell: ({ row }) => (
        <span className="text-sm">
          {formatColombo(row.original.invoiceDate, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      accessorKey: 'invoiceType',
      header: 'Type',
      cell: ({ row }) => {
        const isFree = row.original.hasFreeIssueItems || row.original.invoiceType === 'FreeIssue'
        return isFree ? (
          <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
        ) : (
          <Badge variant="secondary" className="text-xs">Regular</Badge>
        )
      },
    },
    {
      accessorKey: 'totalAmount',
      header: 'Amount',
      cell: ({ row }) => (
        <span className="text-sm font-medium tabular-nums">
          {formatCurrency(row.original.totalAmount)}
        </span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.original.status
        if (status === 'GrnReceived')
          return <Badge className="bg-green-600 hover:bg-green-700 text-xs">GRN Received</Badge>
        if (status === 'Disputed')
          return <Badge variant="destructive" className="text-xs">Disputed</Badge>
        return <Badge variant="outline" className="text-xs">Pending</Badge>
      },
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const item = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-44">
              <DropdownMenuItem onClick={() => openDetail(item.id)}>View</DropdownMenuItem>
              {item.status === 'Pending' && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => openCreateGrn(item.id)}>
                    <ClipboardList className="mr-2 h-3.5 w-3.5 text-muted-foreground" />
                    Create GRN
                  </DropdownMenuItem>
                </>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => openDelete(item.id)}
              >
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
