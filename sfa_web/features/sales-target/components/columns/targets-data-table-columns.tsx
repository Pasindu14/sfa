'use client'

import { MoreHorizontal } from 'lucide-react'
import type { ColumnDef } from '@tanstack/react-table'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { SalesTargetDto } from '../../schema/sales-target.schema'

const MONTH_LABELS = [
  '', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
]

export function getTargetsColumns(
  onEdit?: (target: SalesTargetDto) => void,
): ColumnDef<SalesTargetDto>[] {
  return [
    {
      id: 'rep',
      header: 'Sales Rep',
      cell: ({ row }) => (
        <div>
          <div className="font-medium text-sm">{row.original.salesRepName}</div>
          <div className="text-xs text-muted-foreground font-mono">{row.original.salesRepId}</div>
        </div>
      ),
    },
    {
      id: 'product',
      header: 'Product',
      cell: ({ row }) => (
        <div>
          <div className="font-mono text-xs font-medium">{row.original.productCode}</div>
          <div className="text-xs text-muted-foreground truncate max-w-52">{row.original.productName}</div>
        </div>
      ),
    },
    {
      id: 'period',
      header: 'Period',
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">
          {MONTH_LABELS[row.original.month]} {row.original.year}
        </span>
      ),
    },
    {
      accessorKey: 'targetQuantity',
      header: 'Target Qty',
      cell: ({ row }) => (
        <span className="text-sm font-semibold tabular-nums">
          {row.original.targetQuantity.toLocaleString()}
        </span>
      ),
    },
    {
      id: 'supervisor',
      header: 'Supervisor',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.supervisorName ?? '—'}
        </span>
      ),
    },
    {
      id: 'updatedAt',
      header: 'Updated',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {new Date(row.original.updatedAt).toLocaleDateString('en-LK')}
        </span>
      ),
    },
    ...(onEdit ? [{
      id: 'actions',
      header: 'Actions',
      cell: ({ row }: { row: { original: SalesTargetDto } }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreHorizontal className="h-4 w-4" />
              <span className="sr-only">Open menu</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onEdit(row.original)}>
              Edit Quantity
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    } satisfies ColumnDef<SalesTargetDto>] : []),
  ]
}
