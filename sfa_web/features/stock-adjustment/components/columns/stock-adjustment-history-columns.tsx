'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Eye } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { formatColombo } from '@/lib/utils/datetime'
import { reasonLabel, type StockAdjustmentSummary } from '../../schema/stock-adjustment.schema'

export function getStockAdjustmentHistoryColumns(
  onView: (id: number) => void,
): ColumnDef<StockAdjustmentSummary>[] {
  return [
    {
      accessorKey: 'adjustmentNumber',
      header: 'Adjustment #',
      cell: ({ row }) => (
        <span className="font-mono text-xs font-medium">{row.original.adjustmentNumber}</span>
      ),
    },
    {
      accessorKey: 'adjustedAt',
      header: 'Date',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {formatColombo(row.original.adjustedAt, 'd MMM yyyy, HH:mm')}
        </span>
      ),
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => <span className="text-sm font-medium">{row.original.distributorName}</span>,
    },
    {
      accessorKey: 'reason',
      header: 'Reason',
      cell: ({ row }) => (
        <Badge variant="secondary" className="text-xs">{reasonLabel(row.original.reason)}</Badge>
      ),
    },
    {
      accessorKey: 'lineCount',
      header: 'Lines',
      cell: ({ row }) => <span className="tabular-nums text-sm">{row.original.lineCount}</span>,
    },
    {
      accessorKey: 'totalIncrease',
      header: 'Increase',
      cell: ({ row }) => (
        <span className="tabular-nums text-sm font-semibold text-green-600">
          +{row.original.totalIncrease} pcs
        </span>
      ),
    },
    {
      accessorKey: 'totalDecrease',
      header: 'Decrease',
      cell: ({ row }) => (
        <span className="tabular-nums text-sm font-semibold text-destructive">
          −{row.original.totalDecrease} pcs
        </span>
      ),
    },
    {
      accessorKey: 'adjustedByName',
      header: 'By',
      cell: ({ row }) => <span className="text-sm">{row.original.adjustedByName ?? '—'}</span>,
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
