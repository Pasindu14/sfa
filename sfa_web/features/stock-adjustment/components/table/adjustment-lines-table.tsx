'use client'

import { X } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { formatCasesPieces, splitCasesPieces, stockLineKey } from '@/features/stock/lib/quantity'

export type AdjustmentRow = {
  productId: number
  productCode: string
  productDescription: string
  stockType: 'Normal' | 'FreeIssue'
  piecesPerPack: number
  /** Balance shown on screen, in pieces (0 for added products). Sent as expectedQuantity. */
  currentQuantity: number
  /** Added via "Add product" rather than loaded from the distributor's stock. */
  added?: boolean
}

export function formatSignedDifference(diff: number, piecesPerPack: number): string {
  if (diff === 0) return '0'
  return `${diff > 0 ? '+' : '−'}${formatCasesPieces(Math.abs(diff), piecesPerPack)}`
}

function StockTypeBadge({ type }: { type: string }) {
  if (type === 'FreeIssue')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
  return <Badge variant="secondary" className="text-xs">Normal</Badge>
}

function toCount(raw: string): number {
  const n = Number.parseInt(raw, 10)
  return Number.isFinite(n) && n > 0 ? n : 0
}

// CS + PCS entry for the new balance. Emits total pieces (>= 0); the displayed CS / PCS are
// re-split from that total, so an over-typed PCS value rolls into cases.
function QuantityEntry({
  row,
  value,
  disabled,
  onChange,
}: {
  row: AdjustmentRow
  value: number
  disabled?: boolean
  onChange: (pieces: number) => void
}) {
  const ppp = row.piecesPerPack
  const { cases, pieces } = splitCasesPieces(Math.max(value, 0), ppp)
  const emit = (total: number) => onChange(Math.max(total, 0))

  return (
    <div className="flex items-center justify-end gap-1.5">
      {ppp > 0 && (
        <>
          <Input
            type="number"
            inputMode="numeric"
            min={0}
            aria-label={`${row.productCode} new balance cases`}
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
        aria-label={`${row.productCode} new balance pieces`}
        className="h-8 w-20 text-right tabular-nums"
        value={pieces}
        disabled={disabled}
        onChange={(e) => emit(cases * ppp + toCount(e.target.value))}
      />
      <span className="text-xs text-muted-foreground">PCS</span>
    </div>
  )
}

export function AdjustmentLinesTable({
  rows,
  newQuantities,
  disabled,
  onQuantityChange,
  onRemove,
}: {
  rows: AdjustmentRow[]
  newQuantities: Record<string, number>
  disabled?: boolean
  onQuantityChange: (key: string, pieces: number) => void
  onRemove: (key: string) => void
}) {
  return (
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Product Code</TableHead>
            <TableHead>Description</TableHead>
            <TableHead>Type</TableHead>
            <TableHead className="text-right">Current Balance</TableHead>
            <TableHead className="text-right">New Balance</TableHead>
            <TableHead className="text-right">Difference</TableHead>
            <TableHead className="w-10" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => {
            const key = stockLineKey(row.productId, row.stockType)
            const value = newQuantities[key] ?? row.currentQuantity
            const diff = value - row.currentQuantity
            return (
              <TableRow
                key={key}
                className={cn(diff !== 0 && 'bg-amber-50 hover:bg-amber-100/70 dark:bg-amber-950/30 dark:hover:bg-amber-950/50')}
              >
                <TableCell className="font-mono text-xs font-medium">{row.productCode}</TableCell>
                <TableCell className="text-sm">
                  {row.productDescription}
                  {row.added && (
                    <Badge variant="outline" className="ml-2 text-[10px]">Added</Badge>
                  )}
                </TableCell>
                <TableCell><StockTypeBadge type={row.stockType} /></TableCell>
                <TableCell className="text-right">
                  <div className="flex flex-col items-end leading-tight">
                    <span className={cn('tabular-nums text-sm font-semibold', row.currentQuantity < 0 && 'text-destructive')}>
                      {formatCasesPieces(row.currentQuantity, row.piecesPerPack)}
                    </span>
                    <span className="tabular-nums text-xs text-muted-foreground">
                      {row.currentQuantity} pcs
                    </span>
                  </div>
                </TableCell>
                <TableCell>
                  <QuantityEntry
                    row={row}
                    value={value}
                    disabled={disabled}
                    onChange={(pieces) => onQuantityChange(key, pieces)}
                  />
                </TableCell>
                <TableCell
                  className={cn(
                    'text-right tabular-nums text-sm font-semibold',
                    diff > 0 && 'text-green-600',
                    diff < 0 && 'text-destructive',
                    diff === 0 && 'text-muted-foreground font-normal',
                  )}
                >
                  {formatSignedDifference(diff, row.piecesPerPack)}
                </TableCell>
                <TableCell>
                  {row.added && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7"
                      aria-label={`Remove ${row.productCode}`}
                      disabled={disabled}
                      onClick={() => onRemove(key)}
                    >
                      <X className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
