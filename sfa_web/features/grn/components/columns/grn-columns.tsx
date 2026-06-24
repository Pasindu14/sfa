'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, CheckCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { GrnListItem, GrnStatus } from '../../schema/grn.schema'
import { formatColombo } from '@/lib/utils/datetime'

export interface GrnColumnActions {
  openConfirm: (id: number) => void
  openDelete: (id: number) => void
}

function GrnStatusBadge({ status }: { status: GrnStatus }) {
  if (status === 'Confirmed')
    return <Badge className="bg-green-600 hover:bg-green-700 text-xs">Confirmed</Badge>
  if (status === 'Disputed')
    return <Badge variant="destructive" className="text-xs">Disputed</Badge>
  return <Badge variant="outline" className="text-xs">Pending</Badge>
}

export function getGrnColumns(actions: GrnColumnActions): ColumnDef<GrnListItem>[] {
  const { openConfirm, openDelete } = actions

  return [
    {
      id: 'grn',
      header: 'GRN Number',
      cell: ({ row }) => (
        <span className="font-mono text-sm font-medium">{row.original.grnNumber}</span>
      ),
    },
    {
      accessorKey: 'salesInvoiceVchBillNo',
      header: 'Invoice Bill No',
      cell: ({ row }) => (
        <span className="font-mono text-sm text-muted-foreground">
          {row.original.salesInvoiceVchBillNo}
        </span>
      ),
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.distributorName}</span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <GrnStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'receivedAt',
      header: 'Received At',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.receivedAt, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      accessorKey: 'confirmedByName',
      header: 'Confirmed By',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.confirmedByName ?? '—'}
        </span>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {formatColombo(row.original.createdAt, 'd MMM yyyy')}
        </span>
      ),
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
              {item.status === 'Pending' && (
                <>
                  <DropdownMenuItem
                    className="text-green-700 focus:text-green-700"
                    onClick={() => openConfirm(item.id)}
                  >
                    <CheckCircle className="mr-2 h-3.5 w-3.5" />
                    Confirm Receipt
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                </>
              )}
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
