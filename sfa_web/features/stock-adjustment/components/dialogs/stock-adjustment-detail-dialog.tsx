'use client'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { formatColombo } from '@/lib/utils/datetime'
import { formatCasesPieces } from '@/features/stock/lib/quantity'
import { useStockAdjustmentDetail } from '../../hooks/stock-adjustment.hooks'
import { reasonLabel } from '../../schema/stock-adjustment.schema'
import { formatSignedDifference } from '../table/adjustment-lines-table'

export function StockAdjustmentDetailDialog({
  adjustmentId,
  onClose,
}: {
  adjustmentId: number | null
  onClose: () => void
}) {
  const { data, isLoading, isError } = useStockAdjustmentDetail(adjustmentId)

  return (
    <Dialog open={adjustmentId !== null} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{data ? `Stock adjustment ${data.adjustmentNumber}` : 'Stock adjustment'}</DialogTitle>
          <DialogDescription>
            {data
              ? `${data.distributorName} · ${formatColombo(data.adjustedAt, 'd MMM yyyy, HH:mm')}`
              : 'Loading adjustment details'}
          </DialogDescription>
        </DialogHeader>

        {isLoading && (
          <div className="flex flex-col gap-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full" />
            ))}
          </div>
        )}

        {isError && (
          <p className="text-sm text-destructive">Could not load this adjustment. Please try again.</p>
        )}

        {data && (
          <div className="flex flex-col gap-3">
            <div className="flex flex-wrap gap-x-6 gap-y-1 text-sm">
              <span>
                <span className="text-muted-foreground">Reason </span>
                {reasonLabel(data.reason)}
              </span>
              <span>
                <span className="text-muted-foreground">By </span>
                {data.adjustedByName ?? '—'}
              </span>
              <span>
                <span className="text-muted-foreground">Increase </span>
                <span className="tabular-nums text-green-600">+{data.totalIncrease} pcs</span>
              </span>
              <span>
                <span className="text-muted-foreground">Decrease </span>
                <span className="tabular-nums text-destructive">−{data.totalDecrease} pcs</span>
              </span>
            </div>
            <ScrollArea className="max-h-80 rounded-lg border">
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
                  {data.lines.map((line) => (
                    <tr key={line.id} className="border-b last:border-0">
                      <td className="px-3 py-1.5">
                        <span className="font-mono text-xs">{line.productCode}</span>{' '}
                        <span className="text-muted-foreground">{line.productDescription}</span>
                      </td>
                      <td className="px-3 py-1.5 text-xs">
                        {line.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal'}
                      </td>
                      <td className="px-3 py-1.5 text-right tabular-nums whitespace-nowrap">
                        <span className="text-muted-foreground">
                          {formatCasesPieces(line.quantityBefore, line.piecesPerPack)}
                        </span>
                        {' → '}
                        <span className="font-medium">{formatCasesPieces(line.newQuantity, line.piecesPerPack)}</span>
                      </td>
                      <td
                        className={cn(
                          'px-3 py-1.5 text-right tabular-nums font-semibold whitespace-nowrap',
                          line.difference > 0 ? 'text-green-600' : 'text-destructive',
                        )}
                      >
                        {formatSignedDifference(line.difference, line.piecesPerPack)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </ScrollArea>
            {data.notes && (
              <div className="text-sm">
                <span className="text-xs text-muted-foreground">Notes</span>
                <p className="whitespace-pre-wrap">{data.notes}</p>
              </div>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
