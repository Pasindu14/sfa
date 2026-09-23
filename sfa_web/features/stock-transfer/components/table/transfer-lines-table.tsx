'use client'

import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { DistributorStockItem } from '@/features/stock/schema/stock.schema'
import { formatCasesPieces, splitCasesPieces, stockLineKey } from '../../lib/quantity'

function StockTypeBadge({ type }: { type: string }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
  return <Badge variant="secondary" className="text-xs">Normal</Badge>
}

function toCount(raw: string): number {
  const n = Number.parseInt(raw, 10)
  return Number.isFinite(n) && n > 0 ? n : 0
}

// CS + PCS entry for one line. Emits total pieces, clamped to [0, balance]; the displayed
// CS / PCS are re-split from that total, so an over-typed PCS value rolls into cases.
function QuantityEntry({
  item,
  value,
  disabled,
  onChange,
}: {
  item: DistributorStockItem
  value: number
  disabled?: boolean
  onChange: (pieces: number) => void
}) {
  const ppp = item.piecesPerPack
  const balance = item.quantityOnHand
  const { cases, pieces } = splitCasesPieces(value, ppp)
  const emit = (total: number) => onChange(Math.min(Math.max(total, 0), balance))

  return (
    <div className="flex items-center justify-end gap-1.5">
      {ppp > 0 && (
        <>
          <Input
            type="number"
            inputMode="numeric"
            min={0}
            aria-label={`${item.productCode} cases`}
            className="h-8 w-20 text-right tabular-nums"
            value={cases}
            disabled={disabled}
            onChange={(e) => emit(toCount(e.target.value) * ppp + pieces)}
          />
          <span className="text-xs text-muted-foreground">CS</span>
        </>
      )}
      <Input
        type="number"
        inputMode="numeric"
        min={0}
        aria-label={`${item.productCode} pieces`}
        className="h-8 w-20 text-right tabular-nums"
        value={pieces}
        disabled={disabled}
        onChange={(e) => emit(cases * ppp + toCount(e.target.value))}
      />
      <span className="text-xs text-muted-foreground">PCS</span>
    </div>
  )
}

export function TransferLinesTable({
  items,
  quantities,
  disabled,
  onQuantityChange,
}: {
  items: DistributorStockItem[]
  quantities: Record<string, number>
  disabled?: boolean
  onQuantityChange: (key: string, pieces: number) => void
}) {
  const totalBalance = items.reduce((sum, i) => sum + i.quantityOnHand, 0)
  const totalTransfer = items.reduce(
    (sum, i) => sum + (quantities[stockLineKey(i.productId, i.stockType)] ?? 0),
    0,
  )

  return (
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Product Code</TableHead>
            <TableHead>Description</TableHead>
            <TableHead>Type</TableHead>
            <TableHead className="text-right">Balance</TableHead>
            <TableHead className="text-right">Transfer Qty</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const key = stockLineKey(item.productId, item.stockType)
            const value = quantities[key] ?? 0
            return (
              <TableRow key={key} className={value === 0 ? 'opacity-60' : undefined}>
                <TableCell className="font-mono text-xs font-medium">{item.productCode}</TableCell>
                <TableCell className="text-sm">{item.productDescription}</TableCell>
                <TableCell><StockTypeBadge type={item.stockType} /></TableCell>
                <TableCell className="text-right">
                  <div className="flex flex-col items-end leading-tight">
                    <span className="tabular-nums text-sm font-semibold">
                      {formatCasesPieces(item.quantityOnHand, item.piecesPerPack)}
                    </span>
                    <span className="tabular-nums text-xs text-muted-foreground">
                      {item.quantityOnHand} pcs
                    </span>
                  </div>
                </TableCell>
                <TableCell>
                  <QuantityEntry
                    item={item}
                    value={value}
                    disabled={disabled}
                    onChange={(pieces) => onQuantityChange(key, pieces)}
                  />
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
        <TableFooter>
          <TableRow>
            <TableCell colSpan={3} className="font-semibold">Total</TableCell>
            <TableCell className="text-right tabular-nums font-semibold">{totalBalance} pcs</TableCell>
            <TableCell className="text-right tabular-nums font-semibold">{totalTransfer} pcs</TableCell>
          </TableRow>
        </TableFooter>
      </Table>
    </div>
  )
}
