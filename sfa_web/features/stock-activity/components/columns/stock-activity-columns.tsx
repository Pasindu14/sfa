'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { formatColombo } from '@/lib/utils/datetime'
import { formatCasesPieces } from '@/features/stock/lib/quantity'
import { transactionTypeLabel, type StockActivity } from '../../schema/stock-activity.schema'
import { formatReference } from '../../lib/reference'

function StockTypeBadge({ type }: { type: string }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
  return <Badge variant="secondary" className="text-xs">Normal</Badge>
}

function DirectionBadge({ direction }: { direction: string }) {
  if (direction === 'In')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">In</Badge>
  return <Badge variant="destructive" className="text-xs">Out</Badge>
}

export function getStockActivityColumns(): ColumnDef<StockActivity>[] {
  return [
    {
      accessorKey: 'transactedAt',
      header: 'Date / Time',
      cell: ({ row }) => (
        <span className="whitespace-nowrap text-xs text-muted-foreground">
          {formatColombo(row.original.transactedAt, 'd MMM yyyy, HH:mm')}
        </span>
      ),
    },
    {
      accessorKey: 'transactedByName',
      header: 'User',
      cell: ({ row }) => <span className="text-sm">{row.original.transactedByName ?? '—'}</span>,
    },
    {
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => <span className="text-sm">{row.original.distributorName}</span>,
    },
    {
      accessorKey: 'productCode',
      header: 'Product',
      cell: ({ row }) => (
        <div className="flex flex-col leading-tight">
          <span className="font-mono text-xs font-medium">{row.original.productCode}</span>
          <span className="text-xs text-muted-foreground">{row.original.productDescription}</span>
        </div>
      ),
    },
    {
      accessorKey: 'stockType',
      header: 'Stock Type',
      cell: ({ row }) => <StockTypeBadge type={row.original.stockType} />,
    },
    {
      accessorKey: 'transactionType',
      header: 'Type',
      cell: ({ row }) => (
        <span className="whitespace-nowrap text-sm">{transactionTypeLabel(row.original.transactionType)}</span>
      ),
    },
    {
      accessorKey: 'direction',
      header: 'In/Out',
      cell: ({ row }) => <DirectionBadge direction={row.original.direction} />,
    },
    {
      accessorKey: 'quantity',
      header: 'Qty',
      cell: ({ row }) => (
        <div className="flex flex-col leading-tight">
          <span className="whitespace-nowrap tabular-nums text-sm font-semibold">
            {formatCasesPieces(row.original.quantity, row.original.piecesPerPack)}
          </span>
          <span className="tabular-nums text-xs text-muted-foreground">{row.original.quantity} pcs</span>
        </div>
      ),
    },
    {
      accessorKey: 'quantityBefore',
      header: 'Before',
      cell: ({ row }) => (
        <span className="whitespace-nowrap tabular-nums text-sm text-muted-foreground">
          {formatCasesPieces(row.original.quantityBefore, row.original.piecesPerPack)}
        </span>
      ),
    },
    {
      accessorKey: 'quantityAfter',
      header: 'After',
      cell: ({ row }) => (
        <span className="whitespace-nowrap tabular-nums text-sm">
          {formatCasesPieces(row.original.quantityAfter, row.original.piecesPerPack)}
        </span>
      ),
    },
    {
      id: 'reference',
      header: 'Reference',
      cell: ({ row }) => (
        <span className="whitespace-nowrap font-mono text-xs">{formatReference(row.original) || '—'}</span>
      ),
    },
    {
      accessorKey: 'notes',
      header: 'Notes',
      cell: ({ row }) => (
        <span className="line-clamp-2 max-w-[260px] text-xs text-muted-foreground" title={row.original.notes ?? undefined}>
          {row.original.notes || '—'}
        </span>
      ),
    },
  ]
}
