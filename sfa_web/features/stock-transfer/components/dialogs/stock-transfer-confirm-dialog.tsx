'use client'

import { ArrowRight, Loader2 } from 'lucide-react'
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
import type { DistributorStockItem } from '@/features/stock/schema/stock.schema'
import { formatCasesPieces, stockLineKey } from '../../lib/quantity'

export type ConfirmLine = { item: DistributorStockItem; quantity: number }

export function StockTransferConfirmDialog({
  open,
  sourceName,
  targetName,
  notes,
  lines,
  isPending,
  onOpenChange,
  onConfirm,
}: {
  open: boolean
  sourceName: string
  targetName: string
  notes: string
  lines: ConfirmLine[]
  isPending: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}) {
  const totalPieces = lines.reduce((sum, l) => sum + l.quantity, 0)
  const partial = lines.filter((l) => l.quantity < l.item.quantityOnHand).length

  return (
    <Dialog open={open} onOpenChange={(o) => !isPending && onOpenChange(o)}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Confirm stock transfer</DialogTitle>
          <DialogDescription>
            Stock will be deducted from the source distributor and added to the target.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-3 rounded-lg border bg-muted/40 px-4 py-3 text-sm">
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">From (closed)</span>
              <span className="font-semibold">{sourceName}</span>
            </div>
            <ArrowRight className="h-4 w-4 text-muted-foreground" />
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">To</span>
              <span className="font-semibold">{targetName}</span>
            </div>
            <div className="ml-auto flex flex-col items-end">
              <span className="text-xs text-muted-foreground">
                {lines.length} line{lines.length === 1 ? '' : 's'}
                {partial > 0 && ` · ${partial} partial`}
              </span>
              <span className="font-semibold tabular-nums">{totalPieces} pcs</span>
            </div>
          </div>

          <ScrollArea className="max-h-72 rounded-lg border">
            <table className="w-full text-sm">
              <thead className="sticky top-0 bg-background text-xs text-muted-foreground">
                <tr className="border-b">
                  <th className="px-3 py-2 text-left font-medium">Product</th>
                  <th className="px-3 py-2 text-left font-medium">Type</th>
                  <th className="px-3 py-2 text-right font-medium">Transfer</th>
                  <th className="px-3 py-2 text-right font-medium">Remaining</th>
                </tr>
              </thead>
              <tbody>
                {lines.map(({ item, quantity }) => (
                  <tr key={stockLineKey(item.productId, item.stockType)} className="border-b last:border-0">
                    <td className="px-3 py-1.5">
                      <span className="font-mono text-xs">{item.productCode}</span>{' '}
                      <span className="text-muted-foreground">{item.productDescription}</span>
                    </td>
                    <td className="px-3 py-1.5 text-xs">
                      {item.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal'}
                    </td>
                    <td className="px-3 py-1.5 text-right tabular-nums">
                      {formatCasesPieces(quantity, item.piecesPerPack)}
                    </td>
                    <td className="px-3 py-1.5 text-right tabular-nums text-muted-foreground">
                      {item.quantityOnHand - quantity} pcs
                    </td>
                  </tr>
                ))}
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
            {isPending ? 'Transferring...' : 'Confirm Transfer'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
