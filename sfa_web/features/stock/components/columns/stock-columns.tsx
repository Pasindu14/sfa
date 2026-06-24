'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import type { DistributorStockItem } from '../../schema/stock.schema'
import { formatColombo } from '@/lib/utils/datetime'

function StockLevelBadge({ qty }: { qty: number }) {
  if (qty <= 0)
    return <Badge variant="destructive" className="text-xs">Out of Stock</Badge>
  if (qty < 10)
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Low</Badge>
  return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">In Stock</Badge>
}

function StockTypeBadge({ type }: { type: string }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
  return <Badge variant="secondary" className="text-xs">Normal</Badge>
}

export function getStockColumns(): ColumnDef<DistributorStockItem>[] {
  return [
    {
      accessorKey: 'productCode',
      header: 'Product Code',
      cell: ({ row }) => (
        <span className="font-mono text-xs font-medium">{row.original.productCode}</span>
      ),
    },
    {
      accessorKey: 'productDescription',
      header: 'Description',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.productDescription}</span>
      ),
    },
    {
      accessorKey: 'stockType',
      header: 'Type',
      cell: ({ row }) => <StockTypeBadge type={row.original.stockType} />,
    },
    {
      accessorKey: 'quantityOnHand',
      header: 'Qty on Hand',
      cell: ({ row }) => {
        const qty = row.original.quantityOnHand
        return (
          <div className="flex items-center gap-2">
            <span className="tabular-nums font-semibold">{qty}</span>
            <StockLevelBadge qty={qty} />
          </div>
        )
      },
    },
    {
      accessorKey: 'lastUpdatedAt',
      header: 'Last Updated',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {formatColombo(row.original.lastUpdatedAt, 'd MMM yyyy, HH:mm')}
        </span>
      ),
    },
  ]
}
