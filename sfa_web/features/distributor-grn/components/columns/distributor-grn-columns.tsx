'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ClipboardCheck, Eye } from 'lucide-react'
import type { MyGrnListItem } from '../../schema/distributor-grn.schema'

function GrnStatusBadge({ status }: { status: MyGrnListItem['status'] }) {
  if (status === 'Confirmed')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Confirmed</Badge>
  if (status === 'Disputed')
    return <Badge variant="destructive" className="text-xs">Disputed</Badge>
  return <Badge variant="outline" className="text-xs">Pending</Badge>
}

export function getDistributorGrnColumns(
  onConfirm: (id: number) => void,
  onView: (id: number) => void,
): ColumnDef<MyGrnListItem>[] {
  return [
    {
      accessorKey: 'grnNumber',
      header: 'GRN',
      cell: ({ row }) => (
        <div>
          <p className="font-mono text-xs font-semibold">{row.original.grnNumber}</p>
          <p className="text-xs text-muted-foreground font-mono">
            {row.original.salesInvoiceVchBillNo}
          </p>
        </div>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      cell: ({ row }) => <GrnStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'receivedAt',
      header: 'Received At',
      cell: ({ row }) => {
        const date = row.original.receivedAt
        if (!date) return <span className="text-muted-foreground text-sm">—</span>
        return (
          <span className="text-sm">
            {new Date(date).toLocaleDateString('en-US', {
              day: 'numeric',
              month: 'short',
              year: 'numeric',
            })}
          </span>
        )
      },
    },
    {
      accessorKey: 'confirmedByName',
      header: 'Confirmed By',
      cell: ({ row }) => (
        <span className="text-sm">
          {row.original.confirmedByName ?? <span className="text-muted-foreground">—</span>}
        </span>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {new Date(row.original.createdAt).toLocaleDateString('en-US', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
          })}
        </span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="outline"
            className="h-7 gap-1.5 text-xs"
            onClick={() => onView(row.original.id)}
          >
            <Eye className="h-3.5 w-3.5" />
            View
          </Button>
          {row.original.status === 'Pending' && (
            <Button
              size="sm"
              className="h-7 gap-1.5 text-xs bg-green-600 hover:bg-green-700 text-white"
              onClick={() => onConfirm(row.original.id)}
            >
              <ClipboardCheck className="h-3.5 w-3.5" />
              Confirm Receipt
            </Button>
          )}
        </div>
      ),
    },
  ]
}
