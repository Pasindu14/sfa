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
import { formatColombo } from '@/lib/utils/datetime'
import { useStockTransferDetail } from '../../hooks/stock-transfer.hooks'
import { formatCasesPieces } from '../../lib/quantity'

export function StockTransferDetailDialog({
  transferId,
  onClose,
}: {
  transferId: number | null
  onClose: () => void
}) {
  const { data, isLoading, isError } = useStockTransferDetail(transferId)

  return (
    <Dialog open={transferId !== null} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{data ? `Stock transfer ${data.transferNumber}` : 'Stock transfer'}</DialogTitle>
          <DialogDescription>
            {data
              ? `${data.sourceDistributorName} → ${data.targetDistributorName} · ${formatColombo(data.transferredAt, 'd MMM yyyy, HH:mm')}`
              : 'Loading transfer details'}
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
          <p className="text-sm text-destructive">Could not load this transfer. Please try again.</p>
        )}

        {data && (
          <div className="flex flex-col gap-3">
            <div className="flex flex-wrap gap-x-6 gap-y-1 text-sm">
              <span>
                <span className="text-muted-foreground">By </span>
                {data.transferredByName ?? '—'}
              </span>
              <span>
                <span className="text-muted-foreground">Lines </span>
                {data.lineCount}
              </span>
              <span>
                <span className="text-muted-foreground">Total </span>
                <span className="tabular-nums">{data.totalQuantity} pcs</span>
              </span>
            </div>
            <ScrollArea className="max-h-80 rounded-lg border">
              <table className="w-full text-sm">
                <thead className="sticky top-0 bg-background text-xs text-muted-foreground">
                  <tr className="border-b">
                    <th className="px-3 py-2 text-left font-medium">Product</th>
                    <th className="px-3 py-2 text-left font-medium">Type</th>
                    <th className="px-3 py-2 text-right font-medium">Quantity</th>
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
                      <td className="px-3 py-1.5 text-right tabular-nums">
                        {formatCasesPieces(line.quantity, line.piecesPerPack)}
                        <span className="ml-2 text-xs text-muted-foreground">({line.quantity} pcs)</span>
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
