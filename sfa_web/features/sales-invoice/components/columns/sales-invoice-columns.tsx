'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { SalesInvoiceListItem } from '../types/sales-invoice.types'

export interface SalesInvoiceColumnActions {
  openDetail: (id: number) => void
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
  const { openDetail } = actions

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
          {new Date(row.original.invoiceDate).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })}
        </span>
      ),
    },
    {
      accessorKey: 'invoiceType',
      header: 'Type',
      cell: ({ row }) => {
        const isFree = row.original.invoiceType === 'FreeIssue'
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
      cell: ({ row }) => (
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={() => openDetail(row.original.id)}
        >
          <Eye className="h-4 w-4" />
          <span className="sr-only">View invoice</span>
        </Button>
      ),
    },
  ]
}
