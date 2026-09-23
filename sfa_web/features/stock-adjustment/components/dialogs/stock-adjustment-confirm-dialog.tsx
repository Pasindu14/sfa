'use client'

import { Loader2 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'
import { formatCasesPieces, stockLineKey } from '@/features/stock/lib/quantity'
import { formatSignedDifference, type AdjustmentRow } from '../table/adjustment-lines-table'

export type ChangedLine = { row: AdjustmentRow; newQuantity: number }

export function StockAdjustmentConfirmDialog({
  open,
  distributorName,
  reasonLabel,
  notes,
  lines,
  isPending,
  onOpenChange,
  onConfirm,
}: {
  open: boolean
  distributorName: string
  reasonLabel: string
  notes: string
  lines: ChangedLine[]
  isPending: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}) {
  let totalIncrease = 0
  let totalDecrease = 0
  for (const { row, newQuantity } of lines) {
    const diff = newQuantity - row.currentQuantity
    if (diff > 0) totalIncrease += diff
    else totalDecrease -= diff
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !isPending && onOpenChange(o)}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Confirm stock adjustment</DialogTitle>
          <DialogDescription>
            The balances below will be set for {distributorName}. Only changed lines are listed.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div className="rounded-lg border bg-muted/40 px-3 py-2">
              <span className="block text-xs text-muted-foreground">Reason</span>
              <span className="text-sm font-semibold">{reasonLabel}</span>
            </div>
            <div className="rounded-lg border bg-muted/40 px-3 py-2">
              <span className="block text-xs text-muted-foreground">Lines</span>
              <span className="text-sm font-semibold tabular-nums">{lines.length}</span>
            </div>
            <div className="rounded-lg border bg-muted/40 px-3 py-2">
              <span className="block text-xs text-muted-foreground">Total increase</span>
              <span className="text-sm font-semibold tabular-nums text-green-600">+{totalIncrease} pcs</span>
            </div>
            <div className="rounded-lg border bg-muted/40 px-3 py-2">
              <span className="block text-xs text-muted-foreground">Total decrease</span>
              <span className="text-sm font-semibold tabular-nums text-destructive">−{totalDecrease} pcs</span>
            </div>
          </div>

          <ScrollArea className="max-h-72 rounded-lg border">
            <table className="w-full text-sm">
              <thead className="sticky top-0 bg-background text-xs text-muted-foreground">
                <tr className="border-b">
                  <th className="px-3 py-2 text-left font-medium">Product</th>
                  <th className="px-3 py-2 text-left font-medium">Type</th>
                  <th className="px-3 py-2 text-right font-medium">Before → After</th>
                  <th className="px-3 py-2 text-right font-medium">Difference</th>
                </tr>
              </thead>
              <tbody>
                {lines.map(({ row, newQuantity }) => {
                  const diff = newQuantity - row.currentQuantity
                  return (
                    <tr key={stockLineKey(row.productId, row.stockType)} className="border-b last:border-0">
                      <td className="px-3 py-1.5">
                        <span className="font-mono text-xs">{row.productCode}</span>{' '}
                        <span className="text-muted-foreground">{row.productDescription}</span>
                      </td>
                      <td className="px-3 py-1.5 text-xs">
                        {row.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal'}
                      </td>
                      <td className="px-3 py-1.5 text-right tabular-nums whitespace-nowrap">
                        <span className="text-muted-foreground">
                          {formatCasesPieces(row.currentQuantity, row.piecesPerPack)}
                        </span>
                        {' → '}
                        <span className="font-medium">{formatCasesPieces(newQuantity, row.piecesPerPack)}</span>
                      </td>
                      <td
                        className={cn(
                          'px-3 py-1.5 text-right tabular-nums font-semibold whitespace-nowrap',
                          diff > 0 ? 'text-green-600' : 'text-destructive',
                        )}
                      >
                        {formatSignedDifference(diff, row.piecesPerPack)}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </ScrollArea>

          {notes.trim() && (
            <div className="text-sm">
              <span className="text-xs text-muted-foreground">Notes</span>
              <p className="whitespace-pre-wrap">{notes.trim()}</p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isPending} className="gap-2">
            {isPending && <Loader2 className="h-4 w-4 animate-spin" />}
            {isPending ? 'Saving...' : 'Confirm Adjustment'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
