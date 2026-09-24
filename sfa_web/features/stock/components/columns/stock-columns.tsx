'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import type { DistributorStockItem } from '../../schema/stock.schema'
import { formatColombo } from '@/lib/utils/datetime'

// Module-level: Intl.NumberFormat is expensive to construct.
const moneyFormatter = new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function formatMoney(amount: number): string {
  return moneyFormatter.format(amount)
}

function MoneyCell({ amount, strong }: { amount: number | null | undefined; strong?: boolean }) {
  if (amount === null || amount === undefined)
    return <span className="block text-right text-muted-foreground">—</span>
  return (
    <span className={`block text-right tabular-nums text-sm ${strong ? 'font-semibold' : ''}`}>
      {formatMoney(amount)}
    </span>
  )
}

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

// quantityOnHand is stored in pieces. piecesPerPack (0 = no pack size configured) splits it
// into a case balance + leftover piece balance, matching the "CS · PKT" convention used on mobile.
function StockBalanceCell({ qty, piecesPerPack }: { qty: number; piecesPerPack: number }) {
  if (piecesPerPack <= 0) {
    return (
      <div className="flex items-center gap-2">
        <span className="tabular-nums font-semibold">{qty} pcs</span>
        <StockLevelBadge qty={qty} />
      </div>
    )
  }

  const cases = Math.floor(qty / piecesPerPack)
  const pieces = qty - cases * piecesPerPack

  return (
    <div className="flex items-center gap-2">
      <div className="flex flex-col leading-tight">
        <span className="tabular-nums font-semibold text-sm">{cases} CS</span>
        <span className="tabular-nums text-xs text-muted-foreground">{pieces} PCS</span>
      </div>
      <StockLevelBadge qty={qty} />
    </div>
  )
}

/**
 * `showValue` adds the dealer pack/case price and stock value columns (admin Stock page — the
 * balances endpoint carries prices from the default pricing structure; the portal does not).
 */
export function getStockColumns({ showValue = false }: { showValue?: boolean } = {}): ColumnDef<DistributorStockItem>[] {
  const valueColumns: ColumnDef<DistributorStockItem>[] = showValue
    ? [
        {
          accessorKey: 'dealerPackPrice',
          header: () => <span className="block text-right">Pack Price</span>,
          cell: ({ row }) => <MoneyCell amount={row.original.dealerPackPrice} />,
        },
        {
          accessorKey: 'dealerCasePrice',
          header: () => <span className="block text-right">Case Price</span>,
          cell: ({ row }) => <MoneyCell amount={row.original.dealerCasePrice} />,
        },
        {
          accessorKey: 'stockValue',
          header: () => <span className="block text-right">Stock Value</span>,
          cell: ({ row }) => <MoneyCell amount={row.original.stockValue} strong />,
        },
      ]
    : []

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
      accessorKey: 'fleetName',
      header: 'Fleet',
      cell: ({ row }) => (
        <span className="text-sm">
          {row.original.fleetName ?? <span className="text-muted-foreground">—</span>}
        </span>
      ),
    },
    {
      accessorKey: 'quantityOnHand',
      header: 'Stock Balance',
      cell: ({ row }) => (
        <StockBalanceCell
          qty={row.original.quantityOnHand}
          piecesPerPack={row.original.piecesPerPack}
        />
      ),
    },
    ...valueColumns,
    {
      accessorKey: 'lastUpdatedAt',
      header: 'Last Updated',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {row.original.lastUpdatedAt
            ? formatColombo(row.original.lastUpdatedAt, 'd MMM yyyy, HH:mm')
            : '—'}
        </span>
      ),
    },
  ]
}
