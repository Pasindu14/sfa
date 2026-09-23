'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { ArrowRight, Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatColombo } from '@/lib/utils/datetime'
import type { StockTransferSummary } from '../../schema/stock-transfer.schema'

export function getStockTransferHistoryColumns(
  onView: (id: number) => void,
): ColumnDef<StockTransferSummary>[] {
  return [
    {
      accessorKey: 'transferNumber',
      header: 'Transfer #',
      cell: ({ row }) => (
        <span className="font-mono text-xs font-medium">{row.original.transferNumber}</span>
      ),
    },
    {
      accessorKey: 'transferredAt',
      header: 'Date',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {formatColombo(row.original.transferredAt, 'd MMM yyyy, HH:mm')}
        </span>
      ),
    },
    {
      id: 'route',
      header: 'From → To',
      cell: ({ row }) => (
        <div className="flex items-center gap-1.5 text-sm">
          <span>{row.original.sourceDistributorName}</span>
          <ArrowRight className="h-3 w-3 text-muted-foreground" />
          <span className="font-medium">{row.original.targetDistributorName}</span>
        </div>
      ),
    },
    {
      accessorKey: 'lineCount',
      header: 'Lines',
      cell: ({ row }) => <span className="tabular-nums text-sm">{row.original.lineCount}</span>,
    },
    {
      accessorKey: 'totalQuantity',
      header: 'Total Qty',
      cell: ({ row }) => (
        <span className="tabular-nums text-sm font-semibold">{row.original.totalQuantity} pcs</span>
      ),
    },
    {
      accessorKey: 'transferredByName',
      header: 'By',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.transferredByName ?? '—'}</span>
      ),
    },
    {
      accessorKey: 'notes',
      header: 'Notes',
      cell: ({ row }) => (
        <span className="line-clamp-1 max-w-[240px] text-xs text-muted-foreground">
          {row.original.notes || '—'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1.5"
          onClick={() => onView(row.original.id)}
        >
          <Eye className="h-3.5 w-3.5" />
          View
        </Button>
      ),
    },
  ]
}
